using System;
using System.Threading.Tasks;

namespace LiveStream.Sinks
{
    public interface IM2TcpConnection : IDisposable
    {
        Task<IWorkChunk> GetNextWorkChunkAsync(bool considerRetryQueue);

        void FinishWorkChunks(Func<IWorkChunk, bool> filter);

        int SourceCount { get; }
        
        int WorkCount { get; }
    }
}