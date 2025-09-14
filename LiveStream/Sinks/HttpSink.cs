using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sinks;

public class HttpSink(int port, int minBufferSize) : ISink
{
    private const int MillisecondsTimeout = 15000;
    private readonly Logger<HttpSink> logger = new();
    private readonly TcpListener listener = TcpListener.Create(port);
    private IConnectionManager connectionManager;

    public async Task SinkLoopAsync(IConnectionManager connectionManager, CancellationToken cancellationToken)
    {
        this.connectionManager = connectionManager;
        listener.Start();

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client); // Fire-and-forget
        }
    }


    private async Task HandleClientAsync(TcpClient tcpClient)
    {
        tcpClient.SendBufferSize = 64 * 1024;
        tcpClient.ReceiveBufferSize = 64 * 1024;
        tcpClient.SendTimeout = MillisecondsTimeout;
        tcpClient.ReceiveTimeout = MillisecondsTimeout;

        var endPoint = tcpClient.Client.RemoteEndPoint;

        try
        {
            using var connection = connectionManager.CreateConnection();
            using var stream = tcpClient.GetStream();
            logger.Info($"Connection established {endPoint}");

            var header = Encoding.UTF8.GetBytes(
                "HTTP/1.1 200 OK" + Environment.NewLine
                                  + "Cache-control: no-cache" + Environment.NewLine
                                  + "Connection: close" + Environment.NewLine
                                  + "Content-Type: application/octet-stream" + Environment.NewLine
                                  + Environment.NewLine);
            stream.Write(header, 0, header.Length);

            var timeoutTime = DateTime.UtcNow.AddMilliseconds(MillisecondsTimeout);
            while (connection.Size < minBufferSize)
            {
                Thread.Sleep(100);
                if (DateTime.UtcNow > timeoutTime)
                {
                    await tcpClient.Client.DisconnectAsync(false);
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
                var chunk = await connection.ReadBlockingOrNullAsync(MillisecondsTimeout);

                if (chunk == null || (!firstChunk && chunk.IsStreamReset))
                {
                    logger.Info($"{endPoint}; Stream reset, close connection");
                    await tcpClient.Client.DisconnectAsync(false);
                    stream.Close();
                    tcpClient.Close();
                    return;
                }

                await stream.WriteChunkAsync(chunk);
                firstChunk = false;

                if (counter == 50)
                {
                    logger.Info($"{endPoint}; Sent {chunk.Length} Bytes; Queue {connection.Size}");
                    counter = 0;
                }
            }
        }
        catch (Exception e)
        {
            logger.Info($"Connection lost {endPoint}: {e.Message}");
            try
            {
                tcpClient.Client?.Disconnect(false);
                tcpClient.Close();    
            }
            catch
            {
                // ignored
            }
        }
    }
}