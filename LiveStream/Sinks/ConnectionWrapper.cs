using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveStream
{
    public class ConnectionWrapper
    {
        private int fileId = 0;
        private readonly Guid sequence;
        private readonly IConnection connection;
        private readonly List<WorkChunk> workChunks = new List<WorkChunk>();

        public ConnectionWrapper(IConnection connection)
        {
            this.connection = connection;
            
            sequence = Guid.NewGuid();
        }
        
        public WorkChunk GetNextWorkChunk(bool considerRetryQueue)
        {
            var workChunk = considerRetryQueue ? GetWorkChunkToRetryOrNull() : null;
            
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
                    sequence: sequence,
                    processed: false,
                    retryAt: DateTime.Now.AddSeconds(1));
                
                lock (workChunks)
                {
                    workChunks.Add(workChunk);
                }
            }

            return workChunk;
        }
        
        private WorkChunk GetWorkChunkToRetryOrNull()
        {
            WorkChunk workChunk;
            lock (workChunks)
            {
                workChunks.RemoveAll(wi => wi.Processed);
                workChunk = workChunks.FirstOrDefault(wi => wi.RetryAt < DateTime.Now);
            }
            
            if (workChunk == null)
            {
                return null;
            }
                
            workChunk.RetryAt = DateTime.Now.AddMilliseconds(500);
            return workChunk;
        }

        public void FinishWorkChunks(Func<WorkChunk, bool> filter)
        {
            lock (workChunks)
            {
                foreach (var workChunk in workChunks.Where(filter).ToList())
                {
                    workChunk.Processed = true;
                }
            }
        }
        
        public int SourceCount => connection.MediaQueue.Count;
        
        public int WorkCount => workChunks.Count;
    }
}