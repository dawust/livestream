using System;

namespace LiveStream
{
    public interface IM2TCPConnection : IDisposable
    {
        WorkChunk GetNextWorkChunk();

        void FinishWorkChunk(WorkChunk workChunk);
        
        int SourceCount { get; }
        
        int WorkCount { get; }
    }
}