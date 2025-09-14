using System.Threading.Tasks;

namespace LiveStream
{
    public interface IConnection : IReadOnlyConnection
    {
        bool HasWrites { get; }
        
        void Write(IChunk chunk);
    }
}