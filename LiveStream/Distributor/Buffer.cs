using System.Collections.Generic;
using System.Linq;

namespace LiveStream
{
    public class Buffer
    {
        private readonly Queue<IChunk> queue;
        private readonly int maxSize;

        public Buffer(int size)
        {
            queue = new Queue<IChunk>();
            maxSize = size;
        }

        public void Write(IChunk chunk)
        {
            if (maxSize == 0)
            {
                return;
            }
            
            if (queue.Count == maxSize)
            {
                queue.Dequeue();
            }
            
            queue.Enqueue(chunk);
        }

        public IReadOnlyList<IChunk> GetChunks()
        {
            return queue.ToList();
        }

        public int Size => maxSize;
    }
}