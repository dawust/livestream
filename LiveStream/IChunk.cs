namespace LiveStream
{
    public interface IChunk
    {
        byte[] Buffer { get; }
        
        int Length { get; }
        
        bool IsStreamReset { get; }
    }
}