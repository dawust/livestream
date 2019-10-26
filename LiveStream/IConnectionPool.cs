namespace LiveStream
{
    public interface IConnectionPool
    {
        IConnection CreateConnection(MediaQueue queue);
    }
}