using System;

namespace LiveStream
{
    public interface IReadOnlyConnection : IDisposable
    {
        IChunk ReadBlocking(Action lockedAction = null);

        int Size { get; }
    }
}