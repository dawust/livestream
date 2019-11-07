using System;

namespace LiveStream
{
    public interface IReadOnlyConnection : IDisposable
    {
        IChunk ReadBlocking(Action lockedAction = null);

        IChunk ReadBlockingOrNull(int millisecondsTimeout);

        int Size { get; }
    }
}