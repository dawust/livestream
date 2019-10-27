using System.Collections.Generic;
using System.Linq;

namespace LiveStream
{
    public class ConnectionPool : IConnectionPool
    {
        private readonly List<Connection> connections = new List<Connection>();
        private List<Connection> connectionsClone = new List<Connection>();

        public IConnection CreateConnection()
        {            
            var mediaQueue = new MediaQueue();
            var connection = new Connection(mediaQueue, CloseDeadConnections);
            
            lock (connections)
            {
                connections.Add(connection);
                connectionsClone = connections.ToList();
            }
            
            var connectionCount = GetConnections().Count;
            Logger.Info<ConnectionPool>($"New connection established, {connectionCount} connections");

            return connection;
        }

        private void CloseDeadConnections()
        {
            lock (connections)
            {
                connections.RemoveAll(c => !c.IsAlive);
                connectionsClone = connections.ToList();
            }
            
            var connectionCount = GetConnections().Count;
            Logger.Info<ConnectionPool>($"Connection removed, {connectionCount} connections");
        }

        public IList<Connection> GetConnections()
        {
            return connectionsClone;
        }
    }
}