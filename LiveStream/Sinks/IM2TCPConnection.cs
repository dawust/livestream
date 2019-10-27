using System;

namespace LiveStream
{
    public interface IM2TCPConnection : IDisposable
    {
        WorkChunk GetNextWorkChunk();

        void FinishWorkChunks(int lastId, int seed);

        int SourceCount { get; }
        
        int WorkCount { get; }
    }
}