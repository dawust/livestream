using System;

namespace LiveStream.Sinks
{
    public interface IWorkChunk : IChunk
    {
        int FileId { get; }

        Guid Sequence { get; }
    }
}