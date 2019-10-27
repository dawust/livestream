using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveStream
{
    public class M2TCPConnection : IM2TCPConnection
    {
        private int fileId = 0;

        private readonly int seed;
        private readonly IConnection connection;
        private readonly Action destructorAction;
        private readonly List<WorkChunk> workItems = new List<WorkChunk>();

        public M2TCPConnection(IConnection connection, Action destructorAction)
        {
            this.connection = connection;
            this.destructorAction = destructorAction;
            
            seed = DateTime.Now.GetHashCode() / 100;
        }
        
        public WorkChunk GetNextWorkChunk()
        {
            var workChunk = GetWorkChunkToRetryOrNull();
            
            if (workChunk == null)
            {
                var receiverChunk = connection.MediaQueue.ReadBlocking();
                lock (workItems)
                {
                    workChunk = new WorkChunk(
                        buffer: receiverChunk.Buffer, 
                        length: receiverChunk.Length, 
                        fileId: fileId,
                        seed: seed,
                        processed: false,
                        retryAt: DateTime.Now.AddSeconds(1));

                    fileId++;
                    
                    workItems.Add(workChunk);
                }
            }

            return workChunk;
        }
        
        private WorkChunk GetWorkChunkToRetryOrNull()
        {
            WorkChunk workChunk;
            lock (workItems)
            {
                workItems.RemoveAll(wi => wi.Processed);
                workChunk = workItems.FirstOrDefault(wi => wi.RetryAt < DateTime.Now);
            }
            
            if (workChunk == null)
            {
                return null;
            }
                
            workChunk.RetryAt = DateTime.Now.AddMilliseconds(500);
            return workChunk;
        }

        public void FinishWorkChunk(WorkChunk workChunk)
        {
            lock (workItems)
            {
                workChunk.Processed = true;
            }
        }

        public int SourceCount => connection.MediaQueue.Count;
        
        public int WorkCount => workItems.Count;

        public IConnection Connection => connection;
        
        public void Dispose() => destructorAction();
    }
}