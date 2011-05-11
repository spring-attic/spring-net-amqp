using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Threading;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /**
 * @author Dave Syer
 * 
 */

    /// <summary>
    /// An active object counter.
    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public class ActiveObjectCounter<T>
    {
        /// <summary>
        /// A lock dictionary.
        /// </summary>
        private readonly ConcurrentDictionary<T, CountDownLatch> locks = new ConcurrentDictionary<T, CountDownLatch>();

        /// <summary>
        /// Add the object.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        public void Add(T obj)
        {
            var latchLock = new CountDownLatch(1);
            this.locks.AddOrUpdate(obj, latchLock, (key, oldValue) => latchLock);
        }

        /// <summary>
        /// Release the object.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        public void Release(T obj)
        {
            CountDownLatch remove = null;
            this.locks.TryRemove(obj, out remove);
            if (remove != null)
            {
                remove.CountDown();
            }
        }

        /// <summary>
        /// Await action.
        /// </summary>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        /// <returns>
        /// True if timed out, else false.
        /// </returns>
        public bool Await(TimeSpan timeout)
        {
            var t0 = DateTime.Now;
            var t1 = t0.Add(timeout);
            while (DateTime.Now <= t1)
            {
                if (this.locks == null || this.locks.Count < 1)
                {
                    return true;
                }

                var objects = new HashSet<T>(this.locks.Keys);
                foreach (var obj in objects)
                {
                    CountDownLatch latchLock = this.locks[obj];
                    if (latchLock == null)
                    {
                        continue;
                    }

                    t0 = DateTime.Now;
                    if (latchLock.Await(t1.Subtract(t0)))
                    {
                        CountDownLatch removeResult;
                        this.locks.TryRemove(obj, out removeResult);

                        // TODO: Do something if removeResult is null?
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get the count.
        /// </summary>
        /// <returns>
        /// The count.
        /// </returns>
        public int GetCount()
        {
            return this.locks.Count;
        }

        /// <summary>
        /// Dispose the locks.
        /// </summary>
        public void Dispose()
        {
            this.locks.Clear();
        }
    }
}
