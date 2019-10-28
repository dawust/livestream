using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveStream
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly List<Connection> connections = new List<Connection>();
        private List<IConnection> connectionsClone = new List<IConnection>();

        public IConnection CreateConnection()
        {            
            var mediaQueue = new MediaQueue();
            var connection = new Connection(mediaQueue, CloseDeadConnections);
            
            lock (connections)
            {
                connections.Add(connection);
                connectionsClone = connections.Select(c => (IConnection)c).ToList();
            }
            
            var connectionCount = GetConnections().Count;
            Logger.Info<ConnectionManager>($"New connection established, {connectionCount} connections");

            return connection;
        }

        private void CloseDeadConnections()
        {
            lock (connections)
            {
                connections.RemoveAll(c => !c.IsAlive);
                connectionsClone = connections.Select(c => (IConnection)c).ToList();
            }
            
            var connectionCount = GetConnections().Count;
            Logger.Info<ConnectionManager>($"Connection removed, {connectionCount} connections");
        }

        public IList<IConnection> GetConnections()
        {
            return connectionsClone;
        }
        
        private class Connection : IConnection
        {
            private Action DestructorAction { get; }
            public bool IsAlive { get; private set; }
            public MediaQueue MediaQueue { get; private set; }
        
            public Connection(MediaQueue mediaQueue, Action destructorAction)
            {
                MediaQueue = mediaQueue;
                IsAlive = true;
                DestructorAction = destructorAction;
            }

            public void Dispose()
            {
                IsAlive = false;
                DestructorAction();
                MediaQueue.Clear();
                MediaQueue = null;
            }
        }
    }
}