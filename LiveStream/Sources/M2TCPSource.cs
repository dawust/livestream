using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace LiveStream
{
    public class M2TCPSource : ISource
    {
        private readonly Logger<M2TCPSource> logger = new Logger<M2TCPSource>();
        private readonly IDictionary<Tuple<int, Guid>, IChunk> chunks = new Dictionary<Tuple<int, Guid>, IChunk>();

        private readonly string hostname;
        private readonly int port;
        private readonly int connections;
        private readonly Guid connectionId;

        private MediaQueue queue;
        private int currentFileId;
        private Guid currentSequence;

        public M2TCPSource(string hostname, int port, int connections)
        {
            this.hostname = hostname;
            this.port = port;
            this.connections = connections;
            this.connectionId = Guid.NewGuid();
        }

        public void SourceLoop(MediaQueue mediaQueue)
        {
            queue = mediaQueue;
            logger.Info($"Start {connections} connections");
            
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
        }

        private void ControlThread()
        {
            while (true)
            {
                logger.Info("Start control thread");
                try
                {
                    var tcpClient = new TcpClient(hostname, port);
                    tcpClient.SendBufferSize = 256 * 1024;
                    tcpClient.ReceiveBufferSize = 256 * 1024;
                    tcpClient.NoDelay = true;
                    var networkStream = tcpClient.GetStream();

                    networkStream.WriteInt32(M2TCPSink.ControlThreadMagicNumber);
                    networkStream.WriteGuid(connectionId);

                    while (true)
                    {
                        IReadOnlyList<Tuple<int, Guid>> chunkIds;
                        int lastCheckedFileId;
                        Guid lastCheckedSequence;
                        
                        lock (chunks)
                        {
                            chunkIds = chunks.Select(c => c.Key).ToList();
                            lastCheckedFileId = currentFileId;
                            lastCheckedSequence = currentSequence;
                        }
                        
                        foreach (var chunkId in chunkIds)
                        {
                            networkStream.WriteInt32(M2TCPSink.SingleIdMagicNumber);
                            networkStream.WriteInt32(chunkId.Item1);
                            networkStream.WriteGuid(chunkId.Item2);
                        }
                        
                        networkStream.WriteInt32(M2TCPSink.LastIdMagicNumber);
                        networkStream.WriteInt32(lastCheckedFileId);
                        networkStream.WriteGuid(lastCheckedSequence);
                        Thread.Sleep(100);
                    }
                }
                catch (Exception e)
                {
                    logger.Warning($"Control thread connection closed: {e.Message}");
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

                    networkStream.WriteInt32(shouldRequeue ? M2TCPSink.ReceiveRequeueThreadMagicNumber : M2TCPSink.ReceiveOnlyThreadMagicNumber);
                    networkStream.WriteGuid(connectionId);

                    while (true)
                    {
                        var fileId = networkStream.ReadInt32();
                        var length = networkStream.ReadInt32();
                        var sequence = networkStream.ReadGuid();
                        var buffer = networkStream.ReadExactly(length);

                        var chunk = new Chunk(buffer, length);

                        lock (chunks)
                        {
                            chunks[Tuple.Create(fileId, sequence)] = chunk;
                            if (fileId == 0 && currentSequence != sequence)
                            {
                                logger.Info($"Got new sequence {sequence}");
                                currentSequence = sequence;
                                currentFileId = 0;
                                foreach (var chunkId in chunks.Select(c => c.Key)
                                    .Where(kvp => kvp.Item2 != currentSequence).ToList())
                                {
                                    chunks.Remove(chunkId);
                                }
                            }

                            while (chunks.ContainsKey(Tuple.Create(currentFileId, currentSequence)))
                            {
                                var nextChunk = chunks[Tuple.Create(currentFileId, currentSequence)];
                                foreach (var chunkId in chunks.Select(c => c.Key)
                                    .Where(kvp => kvp.Item1 < currentFileId && kvp.Item2 == currentSequence).ToList())
                                {
                                    chunks.Remove(chunkId);
                                }

                                queue.Write(nextChunk);
                                currentFileId++;
                                lastCheckedId = currentFileId;
                                lastCheck = DateTime.Now;
                            }

                            if (lastCheckedId == currentFileId && lastCheck.AddSeconds(1) < DateTime.Now)
                            {
                                logger.Warning($"Missing id {lastCheckedId}");
                                lastCheck = DateTime.Now;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Warning($"Connection closed: {e.Message}");
                }

                Thread.Sleep(1000);
            }
        }
    }
}