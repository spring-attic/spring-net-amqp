// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActiveObjectCounter.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Common.Logging;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>An active object counter.</summary>
    /// <typeparam name="T">Type T.</typeparam>
    /// <author>Dave Syer</author><author>Joe Fitzgerald (.NET)</author>
    public class ActiveObjectCounter<T>
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// A lock dictionary.
        /// </summary>
        private readonly ConcurrentDictionary<T, CountdownEvent> locks = new ConcurrentDictionary<T, CountdownEvent>();

        /// <summary>Add the object.</summary>
        /// <param name="obj">The obj.</param>
        public void Add(T obj)
        {
            var latchLock = new CountdownEvent(1);
            this.locks.AddOrUpdate(obj, latchLock, (key, oldValue) => latchLock);
        }

        /// <summary>Release the object.</summary>
        /// <param name="obj">The obj.</param>
        public void Release(T obj)
        {
            CountdownEvent remove = null;
            try
            {
                this.locks.TryRemove(obj, out remove);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not remove from locks.", ex);

                // throw;
            }

            if (remove != null)
            {
                if (remove.CurrentCount > 0)
                {
                    remove.Signal();
                }
            }
        }

        /// <summary>Await action.</summary>
        /// <param name="timeout">The timeout.</param>
        /// <returns>True if timed out, else false.</returns>
        public bool Await(TimeSpan timeout)
        {
            var t0 = DateTime.Now;
            var t1 = t0.Add(timeout);
            while (DateTime.Now <= t1)
            {
                if (this.locks == null || this.locks.Count == 0)
                {
                    return true;
                }

                var objects = new HashSet<T>(this.locks.Keys);
                foreach (var obj in objects)
                {
                    CountdownEvent latchLock = null;

                    try
                    {
                        this.locks.TryGetValue(obj, out latchLock);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error occurred getting object.", ex);
                    }

                    if (latchLock == null)
                    {
                        continue;
                    }

                    t0 = DateTime.Now;
                    var t3 = t1.Subtract(t0);
                    if (latchLock.Wait(t3.Milliseconds > 0 ? t3.Milliseconds : 0))
                    {
                        CountdownEvent removeResult;
                        try
                        {
                            this.locks.TryRemove(obj, out removeResult);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Error occurred removing lock.", ex);
                        }
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
        public int GetCount() { return this.locks.Count; }

        /// <summary>
        /// Dispose the locks.
        /// </summary>
        public void Dispose() { this.locks.Clear(); }
    }
}
