using System;
using System.Threading.Tasks;

namespace LiveStream
{
    public interface IReadOnlyConnection : IDisposable
    {
        Task<IChunk> ReadBlockingAsync();

        Task<IChunk> ReadBlockingOrNullAsync(int millisecondsTimeout);

        int Size { get; }
    }
}