using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sinks
{
    public interface ISink
    {
        Task SinkLoopAsync(IConnectionManager connectionManager, CancellationToken cancellationToken);
    }
}