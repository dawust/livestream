using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace LiveStream
{
    public class M2TCPSource : ISource
    {
        private readonly IDictionary<Tuple<int, int>, IChunk> chunks = new Dictionary<Tuple<int, int>, IChunk>();
        private readonly MediaQueue queue = new MediaQueue();

        private readonly string hostname;
        private readonly int port;
        private readonly int connections;
        private readonly Guid connectionId;

        private int lastId = 0;
        private int currentSeed;

        public M2TCPSource(string hostname, int port, int connections)
        {
            this.hostname = hostname;
            this.port = port;
            this.connections = connections;
            this.connectionId = Guid.NewGuid();
        }

        public MediaQueue StartSource()
        {
            Logger.Info<M2TCPSource>($"Start {connections} connections");
            
            for (var tid = 0; tid < (connections + 1) / 2; tid++)
            {
                var thread = new Thread(() => ReceiveThread(shouldRequeue: true));
                thread.Priority = ThreadPriority.Lowest;
                thread.Start();
            }
            
            for (var tid = 0; tid < (connections + 1) / 2; tid++)
            {
                var thread = new Thread(() => ReceiveThread(shouldRequeue: false));
                thread.Priority = ThreadPriority.Lowest;
                thread.Start();
            }
            
            var controlThread = new Thread(ControlThread);
            controlThread.Start();

            return queue;
        }

        private void ControlThread()
        {
            while (true)
            {
                Logger.Info<M2TCPSource>("Start control thread");
                try
                {
                    var tcpClient = new TcpClient(hostname, port);
                    tcpClient.SendBufferSize = 256 * 1024;
                    tcpClient.ReceiveBufferSize = 256 * 1024;
                    tcpClient.NoDelay = true;
                    var networkStream = tcpClient.GetStream();

                    networkStream.SendInt32(M2TCPSink.ControlThreadMagicNumber);
                    networkStream.SendGuid(connectionId);

                    while (true)
                    {
                        var lastCheckedId = 0;
                        var lastSeed = 0;
                        IReadOnlyList<Tuple<int, int>> chunkIds = null;
                        
                        lock (chunks)
                        {
                            chunkIds = chunks.Select(c => c.Key).ToList();
                            lastCheckedId = lastId;
                            lastSeed = currentSeed;
                        }
                        
                        foreach (var chunkId in chunkIds)
                        {
                            networkStream.SendInt32(M2TCPSink.SingleIdMagicNumber);
                            networkStream.SendInt32(chunkId.Item1);
                            networkStream.SendInt32(chunkId.Item2);
                        }
                        
                        networkStream.SendInt32(M2TCPSink.LastIdMagicNumber);
                        networkStream.SendInt32(lastCheckedId);
                        networkStream.SendInt32(lastSeed);
                        Thread.Sleep(100);
                    }
                }
                catch (Exception e)
                {
                    Logger.Warning<M2TCPSource>($"Control thread connection closed: {e.Message}");
                }

                Thread.Sleep(1000);
            }
        }

        private void ReceiveThread(bool shouldRequeue)
        {
            while (true)
            {
                try
                {
                    var tcpClient = new TcpClient(hostname, port);
                    tcpClient.SendBufferSize = 256 * 1024;
                    tcpClient.ReceiveBufferSize = 256 * 1024;
                    var networkStream = tcpClient.GetStream();

                    var lastCheckedId = 0;
                    var lastCheck = DateTime.Now;

                    networkStream.SendInt32(shouldRequeue ? M2TCPSink.ReceiveRequeueThreadMagicNumber : M2TCPSink.ReceiveOnlyThreadMagicNumber);
                    networkStream.SendGuid(connectionId);

                    while (true)
                    {
                        var fileId = networkStream.ReadInt32();
                        var length = networkStream.ReadInt32();
                        var seed = networkStream.ReadInt32();

                        var buffer = new byte[length];
                        networkStream.ReadExactly(buffer, length);

                        var chunk = new Chunk(buffer, length);

                        lock (chunks)
                        {
                            chunks[Tuple.Create(fileId, seed)] = chunk;
                            if (fileId == 0)
                            {
                                Logger.Info<M2TCPSource>($"Got new seed {seed}");
                                currentSeed = seed;
                                lastId = 0;
                                foreach (var chunkId in chunks.Select(c => c.Key)
                                    .Where(kvp => kvp.Item2 != currentSeed).ToList())
                                {
                                    chunks.Remove(chunkId);
                                }
                            }

                            while (chunks.ContainsKey(Tuple.Create(lastId, currentSeed)))
                            {
                                var nextChunk = chunks[Tuple.Create(lastId, currentSeed)];
                                foreach (var chunkId in chunks.Select(c => c.Key)
                                    .Where(kvp => kvp.Item1 < lastId && kvp.Item2 == currentSeed).ToList())
                                {
                                    chunks.Remove(chunkId);
                                }

                                queue.Write(nextChunk);
                                lastId++;
                                lastCheckedId = lastId;
                                lastCheck = DateTime.Now;
                            }

                            if (lastCheckedId == lastId && lastCheck.AddSeconds(1) < DateTime.Now)
                            {
                                Logger.Warning<M2TCPSource>($"Missing id {lastCheckedId}");
                                lastCheck = DateTime.Now;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Warning<M2TCPSource>($"Connection closed: {e.Message}");
                }

                Thread.Sleep(1000);
            }
        }
    }
}