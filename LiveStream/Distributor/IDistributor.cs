namespace LiveStream
{
    public interface IDistributor
    {
        void DistributionLoop(MediaQueue source, ConnectionManager connectionManager, Buffer buffer);
    }
}