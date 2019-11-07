using System;

namespace LiveStream
{
    public class MediaQueue
    {
        private readonly BlockingQueue<IChunk> queue = new BlockingQueue<IChunk>();

        public void Write(IChunk chunk) => queue.Enqueue(chunk);

        public IChunk ReadBlocking(Action lockedAction = null) => queue.Dequeue(lockedAction);
        
        public IChunk ReadBlockingOrNull(int millisecondsTimeout) => queue.DequeueOrNull(millisecondsTimeout);

        public void Clear() => queue.Clear();

        public int Count => queue.Count;
    }
}