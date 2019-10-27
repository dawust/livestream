using System;

namespace LiveStream
{
    public class Distributor
    {
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
                            Logger.Warning<Distributor>("Buffer overflow in connection");
                        }
                        
                        queue.Write(chunk);
                    }
                    catch (Exception e)
                    {
                        Logger.Error<Distributor>(e.Message);
                    }
                }
            }
        }
    }
}