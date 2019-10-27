namespace LiveStream
{
    public interface IMediaQueue
    {
        void Write(IChunk chunk);

        IChunk ReadBlocking();

        int Count { get; }
    }
}