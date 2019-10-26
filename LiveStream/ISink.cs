namespace LiveStream
{
    public interface ISink
    {
        void StartSink(IConnectionPool connectionPool);
    }
}