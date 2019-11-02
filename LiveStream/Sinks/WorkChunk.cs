using System;

namespace LiveStream.Sinks
{
    public class WorkChunk : IWorkChunk
    {
        public WorkChunk(byte[] buffer, int length, int fileId, Guid sequence, bool isStreamReset)
        {
            Buffer = buffer;
            Length = length;
            FileId = fileId;
            Sequence = sequence;
            IsStreamReset = isStreamReset;
        }

        public byte[] Buffer { get; }
        
        public int Length { get; }

        public int FileId { get; }

        public Guid Sequence { get; }
        
        public bool IsStreamReset { get; }
        
        public bool Processed { get; set; }
        
        public DateTime RetryAt { get; set; }

        
    }
}