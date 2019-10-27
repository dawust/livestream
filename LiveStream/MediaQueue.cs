using System;

namespace LiveStream
{
    public class MediaQueue
    {
        private readonly BlockingQueue<IChunk> queue = new BlockingQueue<IChunk>();

        public void Write(IChunk chunk)
        {
            queue.Enqueue(chunk);
        }

        public IChunk ReadBlocking()
        {
            return queue.Dequeue();
        }
        
        public IChunk ReadBlocking(Action lockedAction)
        {
            return queue.Dequeue(lockedAction);
        }

        public void Clear()
        {
            queue.Clear();
        }

        public int Count => queue.Count;
    }
}