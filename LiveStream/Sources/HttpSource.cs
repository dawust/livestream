using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sources;

public class HttpSource(string httpUri) : ISource
{
    private const int ReceiveSize = 16384;
    
    private readonly Logger<HttpSource> logger = new();

    public async Task SourceLoopAsync(AsyncBlockingQueue<IChunk> mediaQueue, CancellationToken cancellationToken)
    {
        var httpClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        
        while (true)
        {
            try
            {
                logger.Info($"Read from {httpUri}");
                    
                using var response = await httpClient.GetAsync(httpUri, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var responseStream = await response.Content.ReadAsStreamAsync();

                while (true)
                {
                    var buffer = new byte[ReceiveSize * 4];

                    var receivedLength = 0;
                    while (receivedLength < ReceiveSize * 2)
                    {
                        var length = await responseStream.ReadAsync(buffer, receivedLength, ReceiveSize);
                        if (length == 0)
                        {
                            throw new Exception("Socket was closed, returned 0 bytes");
                        }
                
                        receivedLength += length;
                    }

                    var chunk = new Chunk(buffer, receivedLength);
                    mediaQueue.Enqueue(chunk);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Lost connection to {httpUri}: {e.Message}");
                Thread.Sleep(2000);
            }
        }
    }
}