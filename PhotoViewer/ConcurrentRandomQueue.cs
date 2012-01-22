using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoViewer
{
    /// <summary>
    /// Queue-like class that yields elements at random
    /// </summary>
    class ConcurrentRandomQueue<T>
    {
        private readonly object lockRoot = new object();
        private readonly List<T> items = new List<T>();

        private readonly Random random;

        public ConcurrentRandomQueue(Random random)
        {
            this.random = random;
        }

        public void Enqueue(T t)
        {
            lock (lockRoot)
            {
                items.Add(t);
            }
        }

        public void EnqueueRange(IEnumerable<T> ts)
        {
            lock (lockRoot)
            {
                items.AddRange(ts);
            }
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool TryDequeue(out T result)
        {
            lock (lockRoot)
            {
                if (items.Count == 0)
                {
                    result = default(T);
                    return false;
                }

                var i = random.Next(items.Count);
                result = items[i];
                items.RemoveAt(i);
                return true;
            }
        }
    }

    class QueueEmptyException : Exception { }
}
