namespace LiveStream.Sources
{
    public interface ISource
    {
        void SourceLoop(MediaQueue mediaQueue);
    }
}