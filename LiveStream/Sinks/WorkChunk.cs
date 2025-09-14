using System;

namespace LiveStream.Sinks;

public class WorkChunk(byte[] buffer, int length, int fileId, Guid sequence, bool isStreamReset)
    : IWorkChunk
{
    public byte[] Buffer { get; } = buffer;
    public int Length { get; } = length;
    public int FileId { get; } = fileId;
    public Guid Sequence { get; } = sequence;
    public bool IsStreamReset { get; } = isStreamReset;
    public bool Processed { get; set; }
        
    public DateTime RetryAt { get; set; }
}