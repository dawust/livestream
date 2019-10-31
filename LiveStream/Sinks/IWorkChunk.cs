using System;

namespace LiveStream
{
    public interface IWorkChunk : IChunk
    {
        int FileId { get; }

        Guid Sequence { get; }
    }
}