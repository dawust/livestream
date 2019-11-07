using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LiveStream.Sinks
{
    public class HttpSink : ISink
    {
        private const int MillisecondsTimeout = 15000;
        private readonly Logger<HttpSink> logger = new Logger<HttpSink>();
        private readonly TcpListener listener;
        private readonly int minBufferSize;

        private IConnectionManager connectionManager;

        public HttpSink(int port, int minBufferSize)
        {
            this.minBufferSize = minBufferSize;
            listener = TcpListener.Create(port);
        }

        public void SinkLoop(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
            listener.Start();

            while (true)
            {
                var client = listener.AcceptTcpClient();
                var thread = new Thread(() => Listen(client));
                thread.Start();
            }
        }

        private void Listen(TcpClient tcpClient)
        {
            tcpClient.SendBufferSize = 64 * 1024;
            tcpClient.ReceiveBufferSize = 64 * 1024;
            tcpClient.SendTimeout = MillisecondsTimeout;
            tcpClient.ReceiveTimeout = MillisecondsTimeout;

            var endPoint = tcpClient.Client.RemoteEndPoint;

            try
            {
                using (var connection = connectionManager.CreateConnection())
                {
                    logger.Info($"Connection established {endPoint}");
                    var stream = tcpClient.GetStream();

                    var header = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 200 OK" + Environment.NewLine
                                          + "Cache-control: no-cache" + Environment.NewLine
                                          + "Connection: close" + Environment.NewLine
                                          + "Content-Type: application/octet-stream" + Environment.NewLine
                                          + Environment.NewLine);
                    stream.Write(header, 0, header.Length);

                    var timeoutTime = DateTime.Now.AddMilliseconds(MillisecondsTimeout);
                    while (connection.Size < minBufferSize)
                    {
                        Thread.Sleep(100);
                        if (DateTime.Now > timeoutTime)
                        {
                            tcpClient.Client.Disconnect(false);
                            stream.Close();
                            tcpClient.Close();
                            return;
                        }
                    }

                    var counter = 0;
                    var firstChunk = true;

                    while (true)
                    {
                        counter++;
                        var chunk = connection.ReadBlockingOrNull(MillisecondsTimeout);

                        if (chunk == null || (!firstChunk && chunk.IsStreamReset))
                        {
                            logger.Info($"{endPoint}; Stream reset, close connection");
                            tcpClient.Client.Disconnect(false);
                            stream.Close();
                            tcpClient.Close();
                            return;
                        }

                        stream.WriteChunk(chunk);
                        firstChunk = false;

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
                tcpClient.Client?.Disconnect(false);
                tcpClient.Close();
            }
        }
    }
}