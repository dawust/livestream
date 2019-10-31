using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LiveStream
{
    public class HttpSink : ISink
    {
        private readonly Logger<HttpSink> logger = new Logger<HttpSink>();
        private readonly TcpListener listener;
        private IConnectionManager connectionManager;

        public HttpSink(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void SinkLoop(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
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
            tcpClient.SendBufferSize = 64 * 1024;
            tcpClient.ReceiveBufferSize = 64 * 1024;

            var endPoint = tcpClient.Client.RemoteEndPoint;
            
            try
            {
                using (var connection = connectionManager.CreateConnection())
                {
                    logger.Info($"Connection established {endPoint}");
                    var stream = tcpClient.GetStream();

                    var header = Encoding.UTF8.GetBytes(
                        "HTTP/1.0 200 OK" + Environment.NewLine
                                          + "Transfer-Encoding: chunked" + Environment.NewLine
                                          + "Content-Type: application/octet-stream" + Environment.NewLine
                                          + Environment.NewLine);
                    stream.Write(header, 0, header.Length);

                    var counter = 0;
                    while (true)
                    {
                        counter++;
                        var chunk = connection.ReadBlocking();

                        stream.WriteChunk(chunk);
                        if (counter == 50)
                        {
                            logger.Info($"{endPoint}; Sent {chunk.Length} Bytes; Queue {connection.Size}");
                            counter = 0;
                        }
                    }
                }
            }
            
            catch (Exception e)
            {
                logger.Info($"Connection lost {endPoint}: {e.Message}");
            }
        }
    }
}