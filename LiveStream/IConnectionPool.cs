namespace LiveStream
{
    public interface IConnectionPool
    {
        IConnection CreateConnection();
    }
}