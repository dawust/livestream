using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sinks;

public class ConnectionWrapper(IReadOnlyConnection connection)
{
    private int fileId = 0;
    private readonly Guid sequence = Guid.NewGuid();
    private readonly List<WorkChunk> workChunks = new();
    
    private readonly SemaphoreSlim dequeueLock = new(1, 1);
    
    public async Task<IWorkChunk> GetNextWorkChunkAsync(bool considerRetryQueue)
    {
        var workChunkToRetry = considerRetryQueue ? GetWorkChunkToRetryOrNull() : null;
        if (workChunkToRetry != null)
        {
            return workChunkToRetry;
        }

        IChunk receiverChunk;
        int chunkFileId;
            
        await dequeueLock.WaitAsync();
        try
        {
            receiverChunk = await connection.ReadBlockingAsync();
            chunkFileId = fileId++;
        }
        finally
        {
            dequeueLock.Release();
        }

        var workChunk = new WorkChunk(
            buffer: receiverChunk.Buffer,
            length: receiverChunk.Length,
            fileId: chunkFileId,
            sequence: sequence,
            isStreamReset: receiverChunk.IsStreamReset)
        {
            Processed = false,
            RetryAt = DateTime.UtcNow.AddSeconds(1),
        };

        lock (workChunks)
        {
            workChunks.Add(workChunk);
        }

        return workChunk;
    }
        
    private WorkChunk GetWorkChunkToRetryOrNull()
    {
        WorkChunk workChunk;
        lock (workChunks)
        {
            workChunks.RemoveAll(wi => wi.Processed);
            workChunk = workChunks.FirstOrDefault(wi => wi.RetryAt < DateTime.UtcNow);
        }
            
        if (workChunk == null)
        {
            return null;
        }
                
        workChunk.RetryAt = DateTime.UtcNow.AddMilliseconds(500);
        return workChunk;
    }

    public void FinishWorkChunks(Func<IWorkChunk, bool> filter)
    {
        lock (workChunks)
        {
            foreach (var workChunk in workChunks.Where<WorkChunk>(filter).ToList())
            {
                workChunk.Processed = true;
            }
        }
    }
        
    public int SourceCount => connection.Size;
        
    public int WorkCount => workChunks.Count;
}