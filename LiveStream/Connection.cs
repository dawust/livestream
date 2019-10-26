namespace LiveStream
{
    public class Connection
    {
        private readonly ConnectionPool connectionPool;
        
        public bool IsAlive { get; private set; }
        public MediaQueue MediaQueue { get; }
        
        public Connection(MediaQueue mediaQueue, ConnectionPool connectionPool)
        {
            MediaQueue = mediaQueue;
            IsAlive = true;
            this.connectionPool = connectionPool;
        }

        public void Close()
        {
            IsAlive = false;
            connectionPool.CloseDeadConnections();
        }
    }
}