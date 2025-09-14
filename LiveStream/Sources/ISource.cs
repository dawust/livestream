using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sources
{
    public interface ISource
    {
        Task SourceLoopAsync(AsyncBlockingQueue<IChunk> mediaQueue, CancellationToken cancellationToken);
    }
}