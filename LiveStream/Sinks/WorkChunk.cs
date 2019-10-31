using System;

namespace LiveStream
{
    public class WorkChunk : IWorkChunk
    {
        public WorkChunk(byte[] buffer, int length, int fileId, Guid sequence)
        {
            Buffer = buffer;
            Length = length;
            FileId = fileId;
            Sequence = sequence;
        }

        public byte[] Buffer { get; }
        
        public int Length { get; }

        public int FileId { get; }

        public Guid Sequence { get; }
        
        public bool Processed { get; set; }
        
        public DateTime RetryAt { get; set; }
    }
}