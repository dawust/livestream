using System;

namespace LiveStream
{
    public class Distributor
    {
        private const int MaxQueueSize = 1000;
        private readonly Logger<Distributor> logger = new Logger<Distributor>();

        public void DistributionLoop(MediaQueue source, ConnectionManager connectionManager, Buffer buffer)
        {
            var maxConnectionSize = MaxQueueSize + buffer.Size;
            while (true)
            {
                var chunk = source.ReadBlocking();
                
                foreach (var connection in connectionManager.GetConnections())
                {
                    try
                    {
                        if (connection.Size > maxConnectionSize)
                        {
                            connection.Clear();
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
}