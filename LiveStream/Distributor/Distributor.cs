using System;
using System.Threading.Tasks;

namespace LiveStream.Distributor;

public class Distributor
{
    private const int MaxQueueSize = 1000;
    private readonly Logger<Distributor> logger = new();

    public async Task DistributionLoopAsync(AsyncBlockingQueue<IChunk> mediaQueue, ConnectionManager connectionManager, Buffer buffer)
    {
        var maxConnectionSize = MaxQueueSize + buffer.Size;
        while (true)
        {
            var chunk = await mediaQueue.DequeueAsync();
                
            foreach (var connection in connectionManager.GetConnections())
            {
                try
                {
                    if (connection.Size > maxConnectionSize)
                    {
                        connection.Dispose();
                        logger.Warning("Buffer overflow in connection");
                    }

                    if (!connection.HasWrites && buffer.Size > 0)
                    {
                        var bufferChunks = buffer.GetChunks();
                        foreach (var bufferChunk in bufferChunks)
                        {
                            connection.Write(bufferChunk);
                        }
                        logger.Info($"Fill connection with {bufferChunks.Count} blocks");
                    }

                    connection.Write(chunk);
                }
                catch (Exception e)
                {
                    logger.Error(e.Message);
                }
            }
                                
            buffer.Write(chunk);
        }
    }
}