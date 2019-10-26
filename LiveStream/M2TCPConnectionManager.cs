using System.Collections.Generic;

namespace LiveStream
{
    public class M2TCPConnectionManager
    {
        private readonly IDictionary<int, M2TCPConnection> m2TcpConnections = new Dictionary<int, M2TCPConnection>();
        private readonly IDictionary<int, int> m2TcpConnectionCounter = new Dictionary<int, int>();

        private readonly IConnectionPool connectionPool;

        public M2TCPConnectionManager(IConnectionPool connectionPool)
        {
            this.connectionPool = connectionPool;
        }
        
        public IM2TCPConnection GetConnection(int connectionId)
        {
            lock (m2TcpConnections)
            {
                if (!m2TcpConnections.ContainsKey(connectionId))
                {
                    m2TcpConnectionCounter[connectionId] = 0;
                    m2TcpConnections[connectionId] = new M2TCPConnection(connectionPool);
                }

                m2TcpConnectionCounter[connectionId] = m2TcpConnectionCounter[connectionId] + 1; 
                return m2TcpConnections[connectionId];  
            }
        }

        public void CloseConnection(int connectionId)
        {
            lock (m2TcpConnections)
            {
                m2TcpConnectionCounter[connectionId] = m2TcpConnectionCounter[connectionId] - 1;

                if (m2TcpConnectionCounter[connectionId] < 0)
                {
                    Logger.Error<M2TCPConnectionManager>("Less than 0 connections");
                }
                
                if (m2TcpConnectionCounter[connectionId] <= 0)
                {
                    m2TcpConnections[connectionId].Close();
                }
            }
        }
    }
}