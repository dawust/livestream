using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream
{
    public class M2TCPSink : ISink
    {
        public const int MagicByte = 1337;
        
        private readonly TcpListener listener;
        private M2TCPConnectionManager m2TcpConnectionManager;

        public M2TCPSink(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void StartSink(IConnectionManager connectionManager)
        {
            m2TcpConnectionManager = new M2TCPConnectionManager(connectionManager);
            new Thread(ReceiveLoop).Start();
        }

        private void ReceiveLoop()
        {
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
            tcpClient.SendBufferSize = 1024 * 1024;
            tcpClient.ReceiveBufferSize = 1024 * 1024;

            try
            {
                var stream = tcpClient.GetStream();

                var magicByte = stream.ReadInt32();
                var connectionId = stream.ReadInt32();

                if (magicByte != MagicByte)
                {
                    throw new Exception("Magic byte did not match");
                }
                
                Logger.Info<M2TCPSink>($"Accept connection {tcpClient.Client.RemoteEndPoint} with connection id {connectionId}");
                
                using (var m2TcpConnection = m2TcpConnectionManager.GetOrCreateConnection(connectionId))
                {
                    while (true)
                    {
                        var workChunk = m2TcpConnection.GetNextWorkChunk();
                    
                        var startTime = DateTime.Now;
                        stream.SendWorkChunk(workChunk);

                        var lastId = stream.ReadInt32();
                        m2TcpConnection.FinishWorkChunks(lastId);
                    
                        if (workChunk.FileId % 50 == 0)
                        {
                            var processingTime = (DateTime.Now - startTime).Milliseconds;
                            Logger.Info<M2TCPSink>($"Sent {workChunk.Length} Bytes; Block {workChunk.FileId}; Receiver Queue {m2TcpConnection.SourceCount}; Work Queue {m2TcpConnection.WorkCount}; Time {processingTime}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Info<M2TCPSink>($"Lost connection {tcpClient.Client.RemoteEndPoint}: {e.Message}");
            }
        }
    }
}