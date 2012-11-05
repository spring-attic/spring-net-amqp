// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionExtensions.cs" company="The original author or authors.">
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// Concurrent Queue Extensions
    /// </summary>
    public static class CollectionExtensions
    {
        internal static readonly TimeSpan MaxValue = TimeSpan.FromMilliseconds(int.MaxValue);

        /// <summary>The take.</summary>
        /// <param name="queue">The queue.</param>
        /// <typeparam name="T">Type T</typeparam>
        /// <returns>The T.</returns>
        public static T Take<T>(this ConcurrentQueue<T> queue)
        {
            T result;
            var success = queue.TryDequeue(out result);
            if (success)
            {
                return result;
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>The poll.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="element">The element.</param>
        /// <typeparam name="T">Type T</typeparam>
        /// <returns>The System.Boolean.</returns>
        /// <exception cref="ThreadInterruptedException"></exception>
        public static bool Poll<T>(this ConcurrentQueue<T> queue, TimeSpan duration, out T element)
        {
            var deadline = DateTime.UtcNow.Add(duration);

            T result;
            while (true)
            {
                if (queue.Count > 0)
                {
                    var success = queue.TryDequeue(out result);
                    if (success)
                    {
                        break;
                    }
                }

                if (duration.Ticks <= 0)
                {
                    element = default(T);
                    return false;
                }

                try
                {
                    Thread.Sleep(Cap(duration > MaxValue ? MaxValue : duration));
                    duration = deadline.Subtract(DateTime.UtcNow);
                }
                catch (ThreadInterruptedException e)
                {
                    throw e.PreserveStackTrace();
                }
            }

            element = result;
            return true;
        }

        /// <summary>The poll.</summary>
        /// <param name="collection">The collection.</param>
        /// <param name="duration">The duration.</param>
        /// <typeparam name="T">Type T</typeparam>
        /// <returns>The T.</returns>
        public static T Poll<T>(this BlockingCollection<T> collection, TimeSpan duration)
        {
            T result = default(T);
            var tokenSource = new CancellationTokenSource();
            var task = new Task(
                () =>
                {
                    try
                    {
                        result = collection.Take(tokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        result = default(T);
                    }
                }, 
                tokenSource.Token);
            task.Start();
            task.Wait(duration);
            return result;
        }

        /// <summary>The poll.</summary>
        /// <param name="collection">The collection.</param>
        /// <param name="timeoutMilliseconds">The timeout milliseconds.</param>
        /// <typeparam name="T">Type T</typeparam>
        /// <returns>The T.</returns>
        public static T Poll<T>(this BlockingCollection<T> collection, int timeoutMilliseconds) { return collection.Poll(new TimeSpan(0, 0, 0, 0, timeoutMilliseconds)); }

        /// <summary>The get.</summary>
        /// <param name="collection">The collection.</param>
        /// <param name="key">The key.</param>
        /// <typeparam name="TKey">Type TKey</typeparam>
        /// <typeparam name="TValue">Type TValue</typeparam>
        /// <returns>The TValue.</returns>
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> collection, TKey key)
        {
            TValue result = default(TValue);
            try
            {
                var success = collection.TryGetValue(key, out result);
                if (success)
                {
                    return result;
                }

                return default(TValue);
            }
            catch (Exception ex)
            {
                return default(TValue);
            }
        }

        /// <summary>The add.</summary>
        /// <param name="collection">The collection.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns>The System.Boolean.</returns>
        public static bool Add<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> collection, TKey key, TValue value)
        {
            var result = collection.TryAdd(key, value);
            return result;
        }

        /// <summary>The get and remove.</summary>
        /// <param name="collection">The collection.</param>
        /// <param name="key">The key.</param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns>The TValue.</returns>
        public static TValue GetAndRemove<TKey, TValue>(this IDictionary<TKey, TValue> collection, TKey key)
        {
            TValue result;
            var resultFound = collection.TryGetValue(key, out result);

            if (resultFound)
            {
                collection.Remove(key);
                return result;
            }

            return default(TValue);
        }

        /// <summary>The add or update.</summary>
        /// <param name="list">The list.</param>
        /// <param name="value">The value.</param>
        /// <typeparam name="TValue"></typeparam>
        /// <returns>The System.Boolean.</returns>
        public static bool AddOrUpdate<TValue>(this LinkedList<TValue> list, TValue value)
        {
            if (list.Contains(value))
            {
                return false;
            }
            else
            {
                list.AddLast(value);
                return true;
            }
        }

        /// <summary>The add list value.</summary>
        /// <param name="collection">The collection.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        public static void AddListValue<TKey, TValue>(this IDictionary<TKey, LinkedList<TValue>> collection, TKey key, TValue value)
        {
            if (!collection.ContainsKey(key))
            {
                var newValueList = new LinkedList<TValue>();
                newValueList.AddOrUpdate(value);
                collection.Add(key, newValueList);
                return;
            }

            var valueList = collection.Get(key);
            if (valueList == default(LinkedList<TValue>))
            {
                valueList = new LinkedList<TValue>();
            }

            valueList.AddOrUpdate(value);
            collection[key] = valueList;
        }

        /// <summary>The add or update.</summary>
        /// <param name="collection">The collection.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> collection, TKey key, TValue value)
        {
            if (collection.ContainsKey(key))
            {
                collection[key] = value;
                return;
            }

            collection.Add(key, value);
        }

        internal static TimeSpan Cap(TimeSpan waitTime) { return waitTime > MaxValue ? MaxValue : waitTime; }

        /// <summary>Lock the stack trace information of the given <paramref name="exception"/>
        /// so that it can be rethrow without losing the stack information.</summary>
        /// <remarks><example><code>try
        ///     {
        ///         //...
        ///     }
        ///     catch( Exception e )
        ///     {
        ///         //...
        ///         throw e.PreserveStackTrace(); //rethrow the exception - preserving the full call stack trace!
        ///     }</code>
        /// </example>
        /// </remarks>
        /// <param name="exception">The exception to lock the statck trace.</param>
        /// <returns>The same <paramref name="exception"/> with stack traced locked.</returns>
        private static T PreserveStackTrace<T>(this T exception) where T : Exception
        {
            _preserveStackTrace(exception);
            return exception;
        }

        private static readonly Action<Exception> _preserveStackTrace = (Action<Exception>)Delegate.CreateDelegate(
            typeof(Action<Exception>), 
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic));
    }
}
