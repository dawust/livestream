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
        private readonly int connectionId;

        private int lastId = 0;
        private int currentSeed;

        public M2TCPSource(string hostname, int port, int connections)
        {
            this.hostname = hostname;
            this.port = port;
            this.connections = connections;
            this.connectionId = DateTime.Now.GetHashCode() / 100;
        }

        public MediaQueue StartSource()
        {
            Logger.Info<M2TCPSource>("Start");
            for (var tid = 0; tid < connections; tid++)
            {
                var thread = new Thread(ReceiveThread);
                thread.Priority = ThreadPriority.Lowest;
                thread.Start(tid);
            }

            return queue;
        }

        private void ReceiveThread(object o)
        {
            while (true)
            {
                try
                {
                    var tcpClient = new TcpClient(hostname, port);
                    tcpClient.SendBufferSize = 64 * 1024;
                    tcpClient.ReceiveBufferSize = 64 * 1024;
                    var networkStream = tcpClient.GetStream();
                    networkStream.ReadTimeout = 10000;

                    var lastCheckedId = 0;
                    var lastCheck = DateTime.Now;

                    networkStream.SendInt32(M2TCPSink.MagicByte);
                    networkStream.SendInt32(connectionId);

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

                        networkStream.SendInt32(lastCheckedId - 1);
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