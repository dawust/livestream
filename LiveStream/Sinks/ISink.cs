namespace LiveStream
{
    public interface ISink
    {
        void StartSink(IConnectionManager connectionManager);
    }
}