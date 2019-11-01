namespace LiveStream
{
    public class Chunk : IChunk
    {
        public Chunk(byte[] buffer, int length)
        {
            Buffer = buffer;
            Length = length;
        }

        public byte[] Buffer { get; }
        public int Length { get; }
        
        public bool IsStreamReset => false;
    }
}