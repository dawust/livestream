using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using LiveStream.Sinks;

namespace LiveStream.Sources
{
    public class M2TcpSource : ISource
    {
        private readonly Logger<M2TcpSource> logger = new Logger<M2TcpSource>();
        private readonly IDictionary<(int FileId, Guid Guid), IChunk> chunks = new Dictionary<(int FileId, Guid Guid), IChunk>();

        private readonly string hostname;
        private readonly int port;
        private readonly int connections;
        private readonly Guid connectionId;
        private readonly bool sendResetPackets;

        private MediaQueue queue;
        private int currentFileId;
        private Guid currentSequence;

        public M2TcpSource(string hostname, int port, int connections, bool sendResetPackets)
        {
            this.hostname = hostname;
            this.port = port;
            this.connections = connections;
            this.sendResetPackets = sendResetPackets;
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

                    networkStream.WriteInt32(M2TcpSink.ControlThreadMagicNumber);
                    networkStream.WriteGuid(connectionId);

                    while (true)
                    {
                        IReadOnlyList<(int FileId, Guid Guid)> chunkIds;
                        int lastCheckedFileId;
                        Guid lastCheckedSequence;

                        lock (chunks)
                        {
                            chunkIds = chunks.Keys.ToList();
                            lastCheckedFileId = currentFileId;
                            lastCheckedSequence = currentSequence;
                        }

                        foreach (var chunkId in chunkIds)
                        {
                            networkStream.WriteInt32(M2TcpSink.SingleIdMagicNumber);
                            networkStream.WriteInt32(chunkId.Item1);
                            networkStream.WriteGuid(chunkId.Item2);
                        }

                        networkStream.WriteInt32(M2TcpSink.LastIdMagicNumber);
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
                    using (var tcpClient = new TcpClient(hostname, port))
                    {
                        tcpClient.SendBufferSize = 256 * 1024;
                        tcpClient.ReceiveBufferSize = 256 * 1024;
                        var networkStream = tcpClient.GetStream();

                        var lastCheckedId = 0;
                        var lastCheck = DateTime.Now;

                        networkStream.WriteInt32(shouldRequeue
                            ? M2TcpSink.ReceiveRequeueThreadMagicNumber
                            : M2TcpSink.ReceiveOnlyThreadMagicNumber);
                        networkStream.WriteGuid(connectionId);

                        while (true)
                        {
                            var fileId = networkStream.ReadInt32();
                            var length = networkStream.ReadInt32();
                            var sequence = networkStream.ReadGuid();
                            var buffer = networkStream.ReadExactly(length);
            
                            var isStreamReset = fileId == 0 && currentSequence != sequence;
                            var chunk = new WorkChunk(
                                buffer: buffer, 
                                length: length, 
                                fileId: fileId, 
                                sequence: sequence, 
                                isStreamReset: isStreamReset && sendResetPackets);
                            
                            lock (chunks)
                            {
                                chunks[(chunk.FileId, chunk.Sequence)] = chunk;
                                if (isStreamReset)
                                {
                                    currentSequence = chunk.Sequence;
                                    currentFileId = 0;

                                    logger.Info($"Got new sequence {currentSequence}");

                                    foreach (var chunkId in chunks.Select(c => c.Key)
                                        .Where(kvp => kvp.Guid != currentSequence).ToList())
                                    {
                                        chunks.Remove(chunkId);
                                    }
                                }

                                while (chunks.ContainsKey((currentFileId, currentSequence)))
                                {
                                    var nextChunk = chunks[(currentFileId, currentSequence)];

                                    queue.Write(nextChunk);
                                    currentFileId++;
                                    lastCheckedId = currentFileId;
                                    lastCheck = DateTime.Now;
                                }
                                
                                foreach (var chunkId in chunks.Keys
                                    .Where(k => k.FileId < currentFileId && k.Guid == currentSequence).ToList())
                                {
                                    chunks.Remove(chunkId);
                                }

                                if (lastCheckedId == currentFileId && lastCheck.AddSeconds(1) < DateTime.Now)
                                {
                                    logger.Warning($"Missing id {lastCheckedId}");
                                    lastCheck = DateTime.Now;
                                }
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