using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sinks
{
    public class M2TcpSink(int port) : ISink
    {
        public const int ReceiveRequeueThreadMagicNumber = 1_1337_1;
        public const int ReceiveOnlyThreadMagicNumber = 2_1337_1;
        public const int ControlThreadMagicNumber = 3_1337_1;
        
        public const int LastIdMagicNumber = 1337;
        public const int SingleIdMagicNumber = 31337;
     
        private readonly Logger<M2TcpSink> logger = new();
        private readonly TcpListener listener = new(IPAddress.Any, port);
        private M2TcpConnectionManager m2TcpConnectionManager;

        public Task SinkLoopAsync(IConnectionManager connectionManager, CancellationToken cancellationToken)
        {
            m2TcpConnectionManager = new M2TcpConnectionManager(connectionManager);
            listener.Start();

            while (!cancellationToken.IsCancellationRequested)
            {
                var client = listener.AcceptTcpClient();
                _ = ListenAsync(client);
            }

            return Task.CompletedTask;
        }

        private async Task ListenAsync(TcpClient tcpClient)
        {
            tcpClient.SendBufferSize = 256 * 1024;
            tcpClient.ReceiveBufferSize = 256 * 1024;

            var remoteEndpoint = tcpClient.Client.RemoteEndPoint;
            try
            {
                await using var stream = tcpClient.GetStream();
                var magicNumber = await stream.ReadInt32Async();
                var connectionId = await stream.ReadGuidAsync();

                if (magicNumber is ReceiveRequeueThreadMagicNumber or ReceiveOnlyThreadMagicNumber)
                {
                    var shouldRequeue = magicNumber == ReceiveRequeueThreadMagicNumber;
                    logger.Info($"Accept connection {tcpClient.Client.RemoteEndPoint}; Id {connectionId}");

                    using var m2TcpConnection = m2TcpConnectionManager.GetOrCreateConnection(connectionId);
                    while (true)
                    {
                        var workChunk = await m2TcpConnection.GetNextWorkChunkAsync(considerRetryQueue: shouldRequeue);

                        var startTime = DateTime.UtcNow;
                        await stream.WriteWorkChunkAsync(workChunk);

                        if (workChunk.FileId % 50 == 0)
                        {
                            var processingTime = (DateTime.UtcNow - startTime).Milliseconds;
                            logger.Info(
                                $"Sent {workChunk.Length} Bytes; Block {workChunk.FileId}; Connection Queue {m2TcpConnection.SourceCount}; Work Queue {m2TcpConnection.WorkCount}; Time {processingTime}");
                        }
                    }
                }

                if (magicNumber == ControlThreadMagicNumber)
                {
                    logger.Info($"Accept control connection {tcpClient.Client.RemoteEndPoint}; Id {connectionId}");

                    using var m2TcpConnection = m2TcpConnectionManager.GetOrCreateConnection(connectionId);
                    while (true)
                    {
                        var type = await stream.ReadInt32Async();
                        var lastId = await stream.ReadInt32Async();
                        var sequence = await stream.ReadGuidAsync();

                        if (type == LastIdMagicNumber)
                        {
                            m2TcpConnection.FinishWorkChunks(
                                wc => wc.FileId < lastId && wc.Sequence == sequence);
                        }
                        else if (type == SingleIdMagicNumber)
                        {
                            m2TcpConnection.FinishWorkChunks(wc =>
                                wc.FileId == lastId && wc.Sequence == sequence);
                        }
                        else
                        {
                            throw new Exception(
                                "Control thread magic number did not match! Probably wrong protocol version!");
                        }
                    }
                }

                throw new Exception("Magic number did not match! Probably wrong protocol version!");
            }
            catch (Exception e)
            {
                logger.Warning($"Lost connection {remoteEndpoint}: {e.Message}");
                try
                {
                    tcpClient.Close();
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}