using System;
using System.Collections.Generic;

namespace LiveStream
{
    public class M2TCPConnectionManager
    {
        private readonly IDictionary<int, M2TCPConnection> m2TcpConnections = new Dictionary<int, M2TCPConnection>();

        private readonly IConnectionManager connectionManager;

        public M2TCPConnectionManager(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
        }
        
        public IM2TCPConnection GetOrCreateConnection(int connectionId)
        {
            lock (m2TcpConnections)
            {
                if (!m2TcpConnections.ContainsKey(connectionId))
                {
                    var connection = connectionManager.CreateConnection();
                    m2TcpConnections[connectionId] = new M2TCPConnection(connection, () => CloseConnection(connectionId));
                }

                var m2TcpConnection = m2TcpConnections[connectionId];
                m2TcpConnection.AddReference();
                
                return m2TcpConnection;  
            }
        }

        private void CloseConnection(int connectionId)
        {
            lock (m2TcpConnections)
            {
                var m2TcpConnection = m2TcpConnections[connectionId];
                m2TcpConnection.RemoveReference();
                
                if (!m2TcpConnection.HasReferences())
                {
                    m2TcpConnections[connectionId].Connection.Dispose();
                    m2TcpConnections.Remove(connectionId);
                }
            }
        }
        
        private class M2TCPConnection : IM2TCPConnection
        {
            private readonly IConnection connection;
            private readonly Action destructorAction;
            private readonly ConnectionWrapper connectionWrapper;
            
            private int references = 0;

            public M2TCPConnection(IConnection connection, Action destructorAction)
            {
                this.connection = connection;
                this.destructorAction = destructorAction;

                connectionWrapper = new ConnectionWrapper(connection);
            }
        
            public WorkChunk GetNextWorkChunk() => connectionWrapper.GetNextWorkChunk();

            public void FinishWorkChunk(WorkChunk workChunk) => connectionWrapper.FinishWorkChunk(workChunk);

            public int SourceCount => connectionWrapper.SourceCount;
        
            public int WorkCount => connectionWrapper.WorkCount;

            public IConnection Connection => connection;

            public void Dispose() => destructorAction();

            public void AddReference() => references++;

            public void RemoveReference() => references--;

            public bool HasReferences()
            {
                if (references < 0)
                {
                    Logger.Error<M2TCPConnection>("Less than 0 references");
                }
                
                return references <= 0;
            }
        }
    }
}