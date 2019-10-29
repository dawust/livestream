using System;

namespace LiveStream
{
    public class Distributor
    {
        private readonly Logger<Distributor> logger = new Logger<Distributor>();
        
        public void DistributionLoop(MediaQueue source, ConnectionManager connectionManager)
        {
            while (true)
            {
                var chunk = source.ReadBlocking();
                
                foreach (var connection in connectionManager.GetConnections())
                {
                    try
                    {
                        var queue = connection.MediaQueue;

                        if (queue.Count > 1000)
                        {
                            queue.Clear();
                            logger.Warning("Buffer overflow in connection");
                        }
                        
                        queue.Write(chunk);
                    }
                    catch (Exception e)
                    {
                        logger.Error(e.Message);
                    }
                }
            }
        }
    }
}