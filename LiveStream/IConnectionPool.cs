namespace LiveStream
{
    public interface IConnectionPool
    {
        Connection CreateConnection(MediaQueue queue);
    }
}