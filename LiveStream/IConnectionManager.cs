namespace LiveStream
{
    public interface IConnectionManager
    {
        IReadOnlyConnection CreateConnection();
    }
}