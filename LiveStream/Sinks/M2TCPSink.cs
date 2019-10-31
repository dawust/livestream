using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LiveStream
{
    public class M2TCPSink : ISink
    {
        public const int ReceiveRequeueThreadMagicNumber = 113371;
        public const int ReceiveOnlyThreadMagicNumber = 213371;
        public const int ControlThreadMagicNumber = 313371;
        
        public const int LastIdMagicNumber = 1337;
        public const int SingleIdMagicNumber = 31337;
     
        private readonly Logger<M2TCPSink> logger = new Logger<M2TCPSink>();
        private readonly TcpListener listener;
        private M2TCPConnectionManager m2TcpConnectionManager;

        public M2TCPSink(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void SinkLoop(IConnectionManager connectionManager)
        {
            m2TcpConnectionManager = new M2TCPConnectionManager(connectionManager);
            listener.Start();

            while (true)
            {
                var client = listener.AcceptTcpClient();
                var thread = new Thread(Listen);
                thread.Start(client);
            }
        }

        private void Listen(object o)
        {
            var tcpClient = (TcpClient) o;
            tcpClient.SendBufferSize = 256 * 1024;
            tcpClient.ReceiveBufferSize = 256 * 1024;

            try
            {
                var stream = tcpClient.GetStream();

                var magicNumber = stream.ReadInt32();
                var connectionId = stream.ReadGuid();

                if (magicNumber == ReceiveRequeueThreadMagicNumber || magicNumber == ReceiveOnlyThreadMagicNumber)
                {
                    var shouldRequeue = magicNumber == ReceiveRequeueThreadMagicNumber;
                    logger.Info($"Accept connection {tcpClient.Client.RemoteEndPoint}; Id {connectionId}");
                
                    using (var m2TcpConnection = m2TcpConnectionManager.GetOrCreateConnection(connectionId))
                    {
                        while (true)
                        {
                            var workChunk = m2TcpConnection.GetNextWorkChunk(considerRetryQueue: shouldRequeue);
                    
                            var startTime = DateTime.Now;
                            stream.WriteWorkChunk(workChunk);
                    
                            if (workChunk.FileId % 50 == 0)
                            {
                                var processingTime = (DateTime.Now - startTime).Milliseconds;
                                logger.Info($"Sent {workChunk.Length} Bytes; Block {workChunk.FileId}; Connection Queue {m2TcpConnection.SourceCount}; Work Queue {m2TcpConnection.WorkCount}; Time {processingTime}");
                            }
                        }
                    }
                }
                
                if (magicNumber == ControlThreadMagicNumber)
                {
                    logger.Info($"Accept control connection {tcpClient.Client.RemoteEndPoint}; Id {connectionId}");
                
                    using (var m2TcpConnection = m2TcpConnectionManager.GetOrCreateConnection(connectionId))
                    {
                        while (true)
                        {
                            var type = stream.ReadInt32();
                            var lastId = stream.ReadInt32();
                            var sequence = stream.ReadGuid();

                            if (type == LastIdMagicNumber)
                            {
                                m2TcpConnection.FinishWorkChunks(wi => wi.FileId < lastId && wi.Sequence == sequence); 
                            } 
                            else if (type == SingleIdMagicNumber)
                            {
                                m2TcpConnection.FinishWorkChunks(wi => wi.FileId == lastId && wi.Sequence == sequence);    
                            }
                            else
                            {
                                throw new Exception("Control thread magic number did not match! Probably wrong protocol version!");
                            }
                        }
                    }
                }

                throw new Exception("Magic number did not match! Probably wrong protocol version!");
                
            }
            catch (Exception e)
            {
                logger.Warning($"Lost connection {tcpClient.Client.RemoteEndPoint}: {e.Message}");
                try
                {
                    tcpClient.Close();
                }
                catch
                {
                }
            }
        }
    }
}