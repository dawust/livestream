namespace LiveStream
{
    public interface ISource
    {
        void SourceLoop(MediaQueue mediaQueue);
    }
}