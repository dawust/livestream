using System;
using System.Collections.Generic;

namespace LiveStream.Sinks
{
    public class M2TcpConnectionManager
    {
        private readonly IDictionary<Guid, M2TcpConnection> m2TcpConnections = new Dictionary<Guid, M2TcpConnection>();

        private readonly IConnectionManager connectionManager;

        public M2TcpConnectionManager(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
        }
        
        public IM2TcpConnection GetOrCreateConnection(Guid connectionId)
        {
            lock (m2TcpConnections)
            {
                if (!m2TcpConnections.ContainsKey(connectionId))
                {
                    var connection = connectionManager.CreateConnection();
                    m2TcpConnections[connectionId] = new M2TcpConnection(connection, () => CloseConnection(connectionId));
                }

                var m2TcpConnection = m2TcpConnections[connectionId];
                m2TcpConnection.AddReference();
                
                return m2TcpConnection;  
            }
        }

        private void CloseConnection(Guid connectionId)
        {
            lock (m2TcpConnections)
            {
                var m2TcpConnection = m2TcpConnections[connectionId];
                m2TcpConnection.RemoveReference();
                
                if (m2TcpConnection.HasNoReferences())
                {
                    m2TcpConnections[connectionId].Connection.Dispose();
                    m2TcpConnections.Remove(connectionId);
                }
            }
        }
        
        private class M2TcpConnection : IM2TcpConnection
        {
            private readonly IReadOnlyConnection connection;
            private readonly Action destructorAction;
            private readonly ConnectionWrapper connectionWrapper;
            
            private int references = 0;

            public M2TcpConnection(IReadOnlyConnection connection, Action destructorAction)
            {
                this.connection = connection;
                this.destructorAction = destructorAction;

                connectionWrapper = new ConnectionWrapper(connection);
            }
        
            public IWorkChunk GetNextWorkChunk(bool considerRetryQueue) => connectionWrapper.GetNextWorkChunk(considerRetryQueue);

            public void FinishWorkChunks(Func<WorkChunk, bool> filter) => connectionWrapper.FinishWorkChunks(filter);

            public int SourceCount => connectionWrapper.SourceCount;
        
            public int WorkCount => connectionWrapper.WorkCount;

            public IReadOnlyConnection Connection => connection;

            public void Dispose() => destructorAction();

            public void AddReference() => references++;

            public void RemoveReference() => references--;

            public bool HasNoReferences() => references == 0;
        }
    }
}