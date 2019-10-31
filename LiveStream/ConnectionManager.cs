using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveStream
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly Logger<ConnectionManager> logger = new Logger<ConnectionManager>();
        private readonly List<Connection> connections = new List<Connection>();
        private volatile IReadOnlyList<Connection> connectionsClone = new List<Connection>();

        public IReadOnlyConnection CreateConnection()
        {            
            var connection = new Connection(destructorAction: CloseDeadConnections);
            
            lock (connections)
            {
                connections.Add(connection);
                connectionsClone = connections.ToList();
            }
            
            var connectionCount = GetConnections().Count;
            logger.Info($"Connection established, {connectionCount} connections");

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
            logger.Info($"Connection removed, {connectionCount} connections");
        }

        public IReadOnlyList<IConnection> GetConnections()
        {
            return connectionsClone;
        }
        
        private class Connection : IConnection
        {
            public bool IsAlive { get; private set; }
            
            public bool HasWrites { get; private set; }
            
            private readonly Action destructorAction;

            private readonly MediaQueue queue;
        
            public Connection(Action destructorAction)
            {
                queue = new MediaQueue();
                this.destructorAction = destructorAction;
                IsAlive = true;
            }

            public void Write(IChunk chunk)
            {
                HasWrites = true;
                queue.Write(chunk);
            } 

            public IChunk ReadBlocking(Action lockedAction = null) => queue.ReadBlocking(lockedAction);

            public void Clear() => queue.Clear();

            public int Size => queue.Count;

            public void Dispose()
            {
                IsAlive = false;
                destructorAction();
                queue.Clear();
            }
        }
    }
}