using System;

namespace LiveStream.Sinks
{
    public interface IM2TcpConnection : IDisposable
    {
        WorkChunk GetNextWorkChunk(bool considerRetryQueue);

        void FinishWorkChunks(Func<WorkChunk, bool> filter);

        int SourceCount { get; }
        
        int WorkCount { get; }
    }
}