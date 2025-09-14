using System.Collections.Generic;
using System.Linq;

namespace LiveStream.Distributor;

public class Buffer(int size)
{
    private readonly Queue<IChunk> queue = new();

    public void Write(IChunk chunk)
    {
        if (size == 0)
        {
            return;
        }
            
        if (chunk.IsStreamReset)
        {
            queue.Clear();
        }
            
        if (queue.Count == size)
        {
            queue.Dequeue();
        }
            
        queue.Enqueue(chunk);
    }

    public IReadOnlyList<IChunk> GetChunks()
    {
        return queue.ToList();
    }

    public int Size => size;
}