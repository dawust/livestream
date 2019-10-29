namespace LiveStream
{
    public interface ISink
    {
        void SinkLoop(IConnectionManager connectionManager);
    }
}