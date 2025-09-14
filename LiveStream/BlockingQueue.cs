using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream;

public class AsyncBlockingQueue<T> where T : class
{
    private readonly Queue<T> queue = new();
    private readonly SemaphoreSlim itemsAvailable = new(0);
    private readonly Lock lockObject = new();

    public void Clear()
    {
        lock (lockObject)
        {
            queue.Clear();

            // Adjust semaphore count
            while (itemsAvailable.CurrentCount > 0)
            {
                itemsAvailable.Wait(0);
            }
        }
    }

    public int Count
    {
        get
        {
            lock (lockObject)
            {
                return queue.Count;
            }
        }
    }

    public void Enqueue(T item)
    {
        lock (lockObject)
        {
            queue.Enqueue(item);
        }

        itemsAvailable.Release();
    }

    public async Task<T> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await itemsAvailable.WaitAsync(cancellationToken);

        lock (lockObject)
        {
            return queue.Dequeue();
        }
    }

    public async Task<T> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var acquired = await itemsAvailable.WaitAsync(timeout, cancellationToken);

        if (!acquired)
        {
            return null;
        }

        lock (lockObject)
        {
            return queue.Dequeue();
        }
    }
}