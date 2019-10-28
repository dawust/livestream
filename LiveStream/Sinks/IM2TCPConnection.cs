using System;

namespace LiveStream
{
    public interface IM2TCPConnection : IDisposable
    {
        WorkChunk GetNextWorkChunk(bool considerRetryQueue);

        void FinishWorkChunks(Func<WorkChunk, bool> filter);

        int SourceCount { get; }
        
        int WorkCount { get; }
    }
}