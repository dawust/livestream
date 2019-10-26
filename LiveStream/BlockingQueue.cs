﻿using System.Collections.Generic;
using System.Threading;

namespace LiveStream
{
    public class BlockingQueue<T> where T : class
    {
        private readonly LinkedList<T> queue = new LinkedList<T>();

        public void AddFirst(T item)
        {
            lock (queue)
            {
                queue.AddFirst(item);
                if (queue.Count == 1)
                {
                    // wake up any blocked dequeue
                    Monitor.PulseAll(queue);
                }
            }
        }

        public void Clear()
        {
            lock (queue)
            {
                queue.Clear();
            }
        }

        public int Count
        {
            get
            {
                int ret;
                lock (queue)
                {
                    ret = queue.Count;
                }

                return ret;
            }
        }


        public void Enqueue(T item)
        {
            lock (queue)
            {
                queue.AddLast(item);
                if (queue.Count == 1)
                {
                    // wake up any blocked dequeue
                    Monitor.PulseAll(queue);
                }
            }
        }

        public T PeekOrNull()
        {
            lock (queue)
            {
                if (queue.Count == 0)
                {
                    return null;
                }

                return queue.First.Value;
            }
        }

        public T Dequeue()
        {
            lock (queue)
            {
                while (queue.Count == 0)
                {
                    Monitor.Wait(queue);
                }

                var item = queue.First.Value;
                queue.RemoveFirst();
                Monitor.PulseAll(queue);

                return item;
            }
        }
    }
}