namespace LiveStream.Sinks
{
    public interface ISink
    {
        void SinkLoop(IConnectionManager connectionManager);
    }
}