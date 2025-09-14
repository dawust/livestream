using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiveStream.Sinks
{
    public class M2TcpConnectionManager(IConnectionManager connectionManager)
    {
        private readonly IDictionary<Guid, M2TcpConnection> m2TcpConnections = new Dictionary<Guid, M2TcpConnection>();

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
        
        private class M2TcpConnection(IReadOnlyConnection connection, Action destructorAction) : IM2TcpConnection
        {
            private readonly ConnectionWrapper connectionWrapper = new(connection);
            
            private int references = 0;
            public Task<IWorkChunk> GetNextWorkChunkAsync(bool considerRetryQueue) => connectionWrapper.GetNextWorkChunkAsync(considerRetryQueue);

            public void FinishWorkChunks(Func<IWorkChunk, bool> filter) => connectionWrapper.FinishWorkChunks(filter);

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