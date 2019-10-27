using System;

namespace LiveStream
{
    public class WorkChunk : IChunk
    {
        public WorkChunk(byte[] buffer, int length, int fileId, int seed, bool processed, DateTime retryAt)
        {
            Buffer = buffer;
            Length = length;
            FileId = fileId;
            Seed = seed;
            Processed = processed;
            RetryAt = retryAt;
        }

        public byte[] Buffer { get; }
        public int Length { get; }

        public int FileId { get; }

        public int Seed { get; }
        
        public bool Processed { get; set; }
        
        public DateTime RetryAt { get; set; }
    }
}