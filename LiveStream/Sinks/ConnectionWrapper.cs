using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveStream
{
    public class ConnectionWrapper
    {
        private int fileId = 0;
        private readonly int seed = 0;
        private readonly IConnection connection;
        private readonly List<WorkChunk> workItems = new List<WorkChunk>();

        public ConnectionWrapper(IConnection connection)
        {
            this.connection = connection;
            
            seed = DateTime.Now.GetHashCode() / 100;
        }
        
        public WorkChunk GetNextWorkChunk()
        {
            var workChunk = GetWorkChunkToRetryOrNull();
            
            if (workChunk == null)
            {
                var chunkFileId = 0;
                var receiverChunk = connection.MediaQueue.ReadBlocking(() =>
                {
                    chunkFileId = fileId;
                    fileId++;
                });

                workChunk = new WorkChunk(
                    buffer: receiverChunk.Buffer, 
                    length: receiverChunk.Length, 
                    fileId: chunkFileId,
                    seed: seed,
                    processed: false,
                    retryAt: DateTime.Now.AddSeconds(1));
                
                lock (workItems)
                {
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

        public void FinishWorkChunks(int lastId)
        {
            lock (workItems)
            {
                foreach (var workChunk in workItems.Where(wi => wi.FileId < lastId).ToList())
                {
                    workChunk.Processed = true;
                }
            }
        }
        
        public int SourceCount => connection.MediaQueue.Count;
        
        public int WorkCount => workItems.Count;
    }
}