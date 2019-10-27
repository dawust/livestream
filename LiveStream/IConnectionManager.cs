namespace LiveStream
{
    public interface IConnectionManager
    {
        IConnection CreateConnection();
    }
}