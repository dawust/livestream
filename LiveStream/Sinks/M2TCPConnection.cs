using System;

namespace LiveStream
{
    public class M2TCPConnection : IM2TCPConnection
    {
        private readonly IConnection connection;
        private readonly Action destructorAction;
        private readonly ConnectionWrapper connectionWrapper;

        public M2TCPConnection(IConnection connection, Action destructorAction)
        {
            this.connection = connection;
            this.destructorAction = destructorAction;

            connectionWrapper = new ConnectionWrapper(connection);
        }
        
        public WorkChunk GetNextWorkChunk()
        {
            return connectionWrapper.GetNextWorkChunk();
        }

        public void FinishWorkChunk(WorkChunk workChunk)
        {
            connectionWrapper.FinishWorkChunk(workChunk);
        }

        public int SourceCount => connectionWrapper.SourceCount;
        
        public int WorkCount => connectionWrapper.WorkCount;

        public IConnection Connection => connection;
        
        public void Dispose() => destructorAction();
    }
}