using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sources;

public class UdpSource(int udpPort) : ISource
{
    private readonly Logger<UdpSource> logger = new();

    public async Task SourceLoopAsync(AsyncBlockingQueue<IChunk> mediaQueue, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                var endPoint = new IPEndPoint(IPAddress.Any, udpPort);
                var udpClient = new UdpClient(endPoint) { Client = { ReceiveBufferSize = 1024 * 1024 } };

                while (true)
                {
                    var result = await udpClient.ReceiveAsync();
                    var buffer = result.Buffer;

                    var chunk = new Chunk(buffer, buffer.Length);
                    mediaQueue.Enqueue(chunk);
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
            }
        }
    }
}