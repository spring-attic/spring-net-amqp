// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LinkedBlockingQueue.cs" company="The original author or authors.">
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
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using Spring.Collections.Generic;
using Spring.Utility;
#endregion

namespace Spring.Threading.Collections.Generic
{
    /// <summary>
    /// An optionally-bounded <see cref="IBlockingQueue{T}"/> based on
    /// linked nodes.</summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks><para>This queue orders elements FIFO (first-in-first-out).
    /// The <b>head</b> of the queue is that element that has been on the
    /// queue the longest time.
    /// The <b>tail</b> of the queue is that element that has been on the
    /// queue the shortest time. New elements
    /// are inserted at the tail of the queue, and the queue retrieval
    /// operations obtain elements at the head of the queue.
    /// Linked queues typically have higher throughput than array-based queues but
    /// less predictable performance in most concurrent applications.</para>
    /// <para>The optional capacity bound constructor argument serves as a
    /// way to prevent excessive queue expansion. The capacity, if unspecified,
    /// is equal to <see cref="System.Int32.MaxValue"/>.  Linked nodes are
    /// dynamically created upon each insertion unless this would bring the
    /// queue above capacity.</para>
    /// </remarks>
    /// <author>Doug Lea</author><author>Griffin Caprio (.NET)</author>
    [Serializable]
    public class LinkedBlockingQueue<T> : AbstractBlockingQueue<T>, ISerializable
    {
        // BACKPORT_3_1
        #region inner classes
        internal class Node
        {
            internal T Item;

            internal Node Next;

            internal Node(T x) { this.Item = x; }
        }
        #endregion

        #region private fields

        /// <summary>The capacity bound, or <see cref="int.MaxValue"/> if none </summary>
        private readonly int _capacity;

        /// <summary>Current number of elements </summary>
        [NonSerialized] private volatile int _activeCount;

        [NonSerialized] private volatile bool _isBroken;

        /// <summary>Head of linked list </summary>
        [NonSerialized] private Node _head;

        /// <summary>Tail of linked list </summary>
        [NonSerialized] private Node _last;

        /// <summary>Lock held by take, poll, etc </summary>
        [NonSerialized] private readonly object _takeLock = new object();

        /// <summary>Lock held by put, offer, etc </summary>
        [NonSerialized] private readonly object _putLock = new object();
        #endregion

        #region ctors

        /// <summary>Initializes a new instance of the <see cref="LinkedBlockingQueue{T}"/> class.  Creates a <see cref="LinkedBlockingQueue{T}"/> with a capacity of<see cref="System.Int32.MaxValue"/>.</summary>
        public LinkedBlockingQueue()
            : this(int.MaxValue) { }

        /// <summary>Initializes a new instance of the <see cref="LinkedBlockingQueue{T}"/> class. Creates a <see cref="LinkedBlockingQueue{T}"/> with the given (fixed) capacity.</summary>
        /// <param name="capacity">the capacity of this queue</param>
        /// <exception cref="System.ArgumentException">if the <paramref name="capacity"/> is not greater than zero.</exception>
        public LinkedBlockingQueue(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "capacity", capacity, "Capacity must be positive integer.");
            }

            this._capacity = capacity;
            this._last = this._head = new Node(default(T));
        }

        /// <summary>Initializes a new instance of the <see cref="LinkedBlockingQueue{T}"/> class. Creates a <see cref="LinkedBlockingQueue{T}"/> with a capacity of<see cref="System.Int32.MaxValue"/>, initially containing the elements o)f the
        /// given collection, added in traversal order of the collection's iterator.</summary>
        /// <param name="collection">the collection of elements to initially contain</param>
        /// <exception cref="System.ArgumentNullException">if the collection or any of its elements are null.</exception>
        /// <exception cref="System.ArgumentException">if the collection size exceeds the capacity of this queue.</exception>
        public LinkedBlockingQueue(ICollection<T> collection)
            : this(int.MaxValue)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection", "must not be null.");
            }

            int count = 0;
            foreach (var item in collection)
            {
                this.Insert(item);
                count++; // we must count ourselves, as collection can change.
            }

            this._activeCount = count;
        }

        /// <summary>Initializes a new instance of the <see cref="LinkedBlockingQueue{T}"/> class. Reconstitute this queue instance from a stream (that is,
        /// deserialize it).</summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param>
        /// <param name="context">The destination (see <see cref="System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param>
        protected LinkedBlockingQueue(SerializationInfo info, StreamingContext context)
        {
            SerializationUtilities.DefaultReadObject(info, context, this);
            this._last = this._head = new Node(default(T));

            var items = (T[])info.GetValue("Data", typeof(T[]));
            foreach (var item in items)
            {
                this.Insert(item);
            }

            this._activeCount = items.Length;
        }

        #endregion

        #region ISerializable Members

        /// <summary>Populates a <see cref="System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.</summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param>
        /// <param name="context">The destination (see <see cref="System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            lock (this._putLock)
            {
                lock (this._takeLock)
                {
                    SerializationUtilities.DefaultWriteObject(info, context, this);
                    info.AddValue("Data", this.ToArray());
                }
            }
        }

        #endregion

        #region base class overrides

        /// <summary>
        /// Inserts the specified element into this queue, waiting if necessary
        /// for space to become available.</summary>
        /// <param name="element">the element to add</param>
        /// <exception cref="ThreadInterruptedException">if interrupted while waiting.</exception>
        /// <exception cref="QueueBrokenException">If the queue is already <see cref="IsBroken">closed</see>.</exception>
        public override void Put(T element)
        {
            if (!this.TryPut(element))
            {
                throw new QueueBrokenException();
            }
        }

        /// <summary>Inserts the specified element into this queue, waiting if necessary
        /// for space to become available.</summary>
        /// <remarks><see cref="Break"/> the queue will cause a waiting <see cref="TryPut"/>
        /// to returned <c>false</c>. This is very useful to indicate that the
        /// consumer is stopped or going to stop so that the producer should not 
        /// put more items into the queue.</remarks>
        /// <param name="element">the element to add</param>
        /// <returns><c>true</c> if succesfully and <c>false</c> if queue <see cref="IsBroken"/>.</returns>
        /// <exception cref="ThreadInterruptedException">if interrupted while waiting.</exception>
        public virtual bool TryPut(T element)
        {
            int tempCount;
            lock (this._putLock)
            {
                /*
                 * Note that count is used in wait guard even though it is
                 * not protected by lock. This works because count can
                 * only decrease at this point (all other puts are shut
                 * out by lock), and we (or some other waiting put) are
                 * signaled if it ever changes from capacity. Similarly 
                 * for all other uses of count in other wait guards.
                 */
                if (this._isBroken)
                {
                    return false;
                }

                try
                {
                    while (this._activeCount == this._capacity)
                    {
                        Monitor.Wait(this._putLock);
                        if (this._isBroken)
                        {
                            return false;
                        }
                    }
                }
                catch (ThreadInterruptedException e)
                {
                    Monitor.Pulse(this._putLock);
                    throw e.PreserveStackTrace();
                }

                this.Insert(element);
                lock (this)
                {
                    tempCount = this._activeCount++;
                }

                if (tempCount + 1 < this._capacity)
                {
                    Monitor.Pulse(this._putLock);
                }
            }

            if (tempCount == 0)
            {
                this.SignalNotEmpty();
            }

            return true;
        }

        /// <summary>
        /// Inserts the specified element into this queue, waiting up to the
        /// specified wait time if necessary for space to become available.</summary>
        /// <param name="element">the element to add</param>
        /// <param name="duration">how long to wait before giving up</param>
        /// <returns><c>true</c> if successful, or <c>false</c> if
        /// the specified waiting time elapses before space is available</returns>
        /// <exception cref="System.InvalidOperationException">If the element cannot be added at this time due to capacity restrictions.</exception>
        /// <exception cref="ThreadInterruptedException">if interrupted while waiting.</exception>
        public override bool Offer(T element, TimeSpan duration)
        {
            DateTime deadline = WaitTime.Deadline(duration);
            int tempCount;
            lock (this._putLock)
            {
                for (;;)
                {
                    if (this._isBroken)
                    {
                        return false;
                    }

                    if (this._activeCount < this._capacity)
                    {
                        this.Insert(element);
                        lock (this)
                        {
                            tempCount = this._activeCount++;
                        }

                        if (tempCount + 1 < this._capacity)
                        {
                            Monitor.Pulse(this._putLock);
                        }

                        break;
                    }

                    if (duration.Ticks <= 0)
                    {
                        return false;
                    }

                    try
                    {
                        Monitor.Wait(this._putLock, WaitTime.Cap(duration));
                        duration = deadline.Subtract(DateTime.UtcNow);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Monitor.Pulse(this._putLock);
                        throw e.PreserveStackTrace();
                    }
                }
            }

            if (tempCount == 0)
            {
                this.SignalNotEmpty();
            }

            return true;
        }

        /// <summary>
        /// Inserts the specified element into this queue if it is possible to do
        /// so immediately without violating capacity restrictions.</summary>
        /// <remarks>When using a capacity-restricted queue, this method is generally
        /// preferable to <see cref="AbstractQueue{T}.Add(T)"/>,
        /// which can fail to insert an element only by throwing an exception.</remarks>
        /// <param name="element">The element to add.</param>
        /// <returns><c>true</c> if the element was added to this queue.</returns>
        public override bool Offer(T element)
        {
            if (this._activeCount == this._capacity || this._isBroken)
            {
                return false;
            }

            int tempCount = -1;
            lock (this._putLock)
            {
                if (this._activeCount < this._capacity && !this._isBroken)
                {
                    this.Insert(element);
                    lock (this)
                    {
                        tempCount = this._activeCount++;
                    }

                    if (tempCount + 1 < this._capacity)
                    {
                        Monitor.Pulse(this._putLock);
                    }
                }
            }

            if (tempCount == 0)
            {
                this.SignalNotEmpty();
            }

            return tempCount >= 0;
        }

        /// <summary> 
        /// Retrieves and removes the head of this queue, waiting if necessary
        /// until an element becomes available.
        /// </summary>
        /// <returns> the head of this queue</returns>
        /// <exception cref="QueueBrokenException">
        /// If the queue is empty and already <see cref="IsBroken">closed</see>.
        /// </exception>
        public override T Take()
        {
            T x;
            if (!this.TryTake(out x))
            {
                throw new QueueBrokenException();
            }

            return x;
        }

        /// <summary>
        /// Retrieves and removes the head of this queue, waiting if necessary
        /// until an element becomes available.</summary>
        /// <remarks><see cref="Break"/> the queue will cause a waiting <see cref="TryTake"/>
        /// to returned <c>false</c>. This is very useful to indicate that the
        /// producer is stopped so that the producer should stop waiting for
        /// element from queu.</remarks>
        /// <param name="element">The head of this queue if successful.</param>
        /// <returns><c>true</c> if succesfully and <c>false</c> if queue is empty and <see cref="IsBroken">closed</see>.</returns>
        public virtual bool TryTake(out T element)
        {
            T x;
            int tempCount;
            lock (this._takeLock)
            {
                try
                {
                    while (this._activeCount == 0)
                    {
                        if (this._isBroken)
                        {
                            element = default(T);
                            return false;
                        }

                        Monitor.Wait(this._takeLock);
                    }
                }
                catch (ThreadInterruptedException e)
                {
                    Monitor.Pulse(this._takeLock);
                    throw e.PreserveStackTrace();
                }

                x = this.Extract();
                lock (this)
                {
                    tempCount = this._activeCount--;
                }

                if (tempCount > 1)
                {
                    Monitor.Pulse(this._takeLock);
                }
            }

            if (tempCount == this._capacity)
            {
                this.SignalNotFull();
            }

            element = x;
            return true;
        }

        /// <summary>
        /// Retrieves and removes the head of this queue, waiting up to the
        /// specified wait time if necessary for an element to become available.</summary>
        /// <param name="duration">How long to wait before giving up.</param>
        /// <param name="element">Set to the head of this queue. <c>default(T)</c> if queue is empty.</param>
        /// <returns><c>false</c> if the queue is still empty after waited for the time 
        /// specified by the <paramref name="duration"/>. Otherwise <c>true</c>.</returns>
        public override bool Poll(TimeSpan duration, out T element)
        {
            DateTime deadline = WaitTime.Deadline(duration);
            T x;
            int c;
            lock (this._takeLock)
            {
                for (;;)
                {
                    if (this._activeCount > 0)
                    {
                        x = this.Extract();
                        lock (this)
                        {
                            c = this._activeCount--;
                        }

                        if (c > 1)
                        {
                            Monitor.Pulse(this._takeLock);
                        }

                        break;
                    }

                    if (duration.Ticks <= 0 || this._isBroken)
                    {
                        element = default(T);
                        return false;
                    }

                    try
                    {
                        Monitor.Wait(this._takeLock, WaitTime.Cap(duration));
                        duration = deadline.Subtract(DateTime.UtcNow);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Monitor.Pulse(this._takeLock);
                        throw e.PreserveStackTrace();
                    }
                }
            }

            if (c == this._capacity)
            {
                this.SignalNotFull();
            }

            element = x;
            return true;
        }

        /// <summary>Retrieves and removes the head of this queue into out parameter<paramref name="element"/>. </summary>
        /// <param name="element">Set to the head of this queue. <c>default(T)</c> if queue is empty.</param>
        /// <returns><c>false</c> if the queue is empty. Otherwise <c>true</c>.</returns>
        public override bool Poll(out T element)
        {
            if (this._activeCount == 0)
            {
                element = default(T);
                return false;
            }

            T x = default(T);
            int c = -1;
            lock (this._takeLock)
            {
                if (this._activeCount > 0)
                {
                    x = this.Extract();
                    lock (this)
                    {
                        c = this._activeCount--;
                    }

                    if (c > 1)
                    {
                        Monitor.Pulse(this._takeLock);
                    }
                }
            }

            if (c == this._capacity)
            {
                this.SignalNotFull();
            }

            element = x;
            return c >= 0;
        }

        /// <summary>Retrieves, but does not remove, the head of this queue into out
        /// parameter <paramref name="element"/>.</summary>
        /// <param name="element">The head of this queue. <c>default(T)</c> if queue is empty.</param>
        /// <returns><c>false</c> is the queue is empty. Otherwise <c>true</c>.</returns>
        public override bool Peek(out T element)
        {
            if (this._activeCount == 0)
            {
                element = default(T);
                return false;
            }

            lock (this._takeLock)
            {
                Node first = this._head.Next;
                bool exists = first != null;
                element = exists ? first.Item : default(T);
                return exists;
            }
        }

        /// <summary>
        /// Removes a single instance of the specified element from this queue,
        /// if it is present.  </summary>
        /// <remarks>
        /// 	If this queue contains one or more such elements.
        /// Returns <c>true</c> if this queue contained the specified element
        /// (or equivalently, if this queue changed as a result of the call).</remarks>
        /// <param name="objectToRemove">element to be removed from this queue, if present</param>
        /// <returns><c>true</c> if this queue changed as a result of the call</returns>
        public override bool Remove(T objectToRemove)
        {
            bool removed = false;
            lock (this._putLock)
            {
                lock (this._takeLock)
                {
                    Node trail = this._head;
                    Node p = this._head.Next;
                    while (p != null)
                    {
                        if (Equals(objectToRemove, p.Item))
                        {
                            removed = true;
                            break;
                        }

                        trail = p;
                        p = p.Next;
                    }

                    if (removed)
                    {
                        p.Item = default(T);
                        trail.Next = p.Next;
                        if (this._last == p)
                        {
                            this._last = trail;
                        }

                        lock (this)
                        {
                            if (this._activeCount-- == this._capacity)
                            {
                                Monitor.PulseAll(this._putLock);
                            }
                        }
                    }
                }
            }

            return removed;
        }

        /// <summary> 
        /// Returns the number of additional elements that this queue can ideally
        /// (in the absence of memory or resource constraints) accept without
        /// blocking. This is always equal to the initial capacity of this queue
        /// minus the current <see cref="LinkedBlockingQueue{T}.Count"/> of this queue.
        /// </summary>
        /// <remarks> 
        /// Note that you <b>cannot</b> always tell if an attempt to insert
        /// an element will succeed by inspecting <see cref="LinkedBlockingQueue{T}.RemainingCapacity"/>
        /// because it may be the case that another thread is about to
        /// insert or remove an element.
        /// </remarks>
        public override int RemainingCapacity { get { return this._capacity == int.MaxValue ? int.MaxValue : this._capacity - this._activeCount; } }

        /// <summary>Does the real work for the <see cref="AbstractQueue{T}.Drain(System.Action{T})"/>
        /// and <see cref="AbstractQueue{T}.Drain(System.Action{T},Predicate{T})"/>.</summary>
        /// <param name="action">The action.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns>The System.Int32.</returns>
        protected internal override int DoDrain(Action<T> action, Predicate<T> criteria)
        {
            if (criteria != null)
            {
                return this.DoDrain(action, int.MaxValue, criteria);
            }

            Node first;
            lock (this._putLock)
            {
                lock (this._takeLock)
                {
                    first = this._head.Next;
                    this._head.Next = null;

                    this._last = this._head;
                    int cold;
                    lock (this)
                    {
                        cold = this._activeCount;
                        this._activeCount = 0;
                    }

                    if (cold == this._capacity)
                    {
                        Monitor.PulseAll(this._putLock);
                    }
                }
            }

            // Transfer the elements outside of locks
            int n = 0;
            for (Node p = first; p != null; p = p.Next)
            {
                action(p.Item);
                p.Item = default(T);
                ++n;
            }

            return n;
        }

        /// <summary>Does the real work for all drain methods. Caller must
        /// guarantee the <paramref name="action"/> is not <c>null</c> and<paramref name="maxElements"/> is greater then zero (0).</summary>
        /// <param name="action">The action.</param>
        /// <param name="maxElements">The max Elements.</param>
        /// <param name="criteria">The criteria.</param>
        /// <seealso cref="IQueue{T}.Drain(System.Action{T})"/><seealso cref="IQueue{T}.Drain(System.Action{T}, int)"/><seealso cref="IQueue{T}.Drain(System.Action{T}, Predicate{T})"/><seealso cref="IQueue{T}.Drain(System.Action{T}, int, Predicate{T})"/>
        /// <returns>The System.Int32.</returns>
        protected internal override int DoDrain(Action<T> action, int maxElements, Predicate<T> criteria)
        {
            lock (this._putLock)
            {
                lock (this._takeLock)
                {
                    int n = 0;
                    Node p = this._head;
                    Node c = p.Next;
                    while (c != null && n < maxElements)
                    {
                        if (criteria == null || criteria(c.Item))
                        {
                            action(c.Item);
                            c.Item = default(T);
                            p.Next = c.Next;
                            ++n;
                        }
                        else
                        {
                            p = c;
                        }

                        c = c.Next;
                    }

                    if (n != 0)
                    {
                        if (c == null)
                        {
                            this._last = p;
                        }

                        int cold;
                        lock (this)
                        {
                            cold = this._activeCount;
                            this._activeCount -= n;
                        }

                        if (cold == this._capacity)
                        {
                            Monitor.PulseAll(this._putLock);
                        }
                    }

                    return n;
                }
            }
        }

        /// <summary>
        /// Gets the capacity of this queue.
        /// </summary>
        public override int Capacity { get { return this._capacity; } }

        /// <summary>Does the actual work of copying to array.</summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the 
        /// destination of the elements copied from <see cref="ICollection{T}"/>. 
        /// The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <param name="ensureCapacity">If is <c>true</c>, calls <see cref="AbstractCollection{T}.EnsureCapacity"/></param>
        /// <returns>A new array of same runtime type as <paramref name="array"/> if <paramref name="array"/> is too small to hold all elements and <paramref name="ensureCapacity"/> is <c>false</c>. Otherwise
        /// the <paramref name="array"/> instance itself.</returns>
        protected override T[] DoCopyTo(T[] array, int arrayIndex, bool ensureCapacity)
        {
            lock (this._putLock)
            {
                lock (this._takeLock)
                {
                    if (ensureCapacity)
                    {
                        array = EnsureCapacity(array, this._activeCount);
                    }

                    for (Node p = this._head.Next; p != null; p = p.Next)
                    {
                        array[arrayIndex++] = p.Item;
                    }

                    return array;
                }
            }
        }

        /// <summary>
        /// Gets the count of the queue. 
        /// </summary>
        public override int Count { get { return this._activeCount; } }

        /// <summary>test whether the queue contains <paramref name="item"/> </summary>
        /// <param name="item">the item whose containement should be checked</param>
        /// <returns><c>true</c> if item is in the queue, <c>false</c> otherwise</returns>
        public override bool Contains(T item)
        {
            lock (this._putLock)
            {
                lock (this._takeLock)
                {
                    for (Node p = this._head.Next; p != null; p = p.Next)
                    {
                        if (Equals(item, p.Item))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Indicate if current queue is broken. A broken queue never blocks.
        /// </summary>
        /// <para>
        /// When a queue is broken, all blocking actions and subsequent put
        /// actions will return immediately with unsuccesful status or 
        /// <see cref="QueueBrokenException"/> is thrown if method doesn't 
        /// return any status. Subsequent get actions will continue to sucess
        /// until the queue becomes empty, then further get actions will return
        /// unsuccesful status or throws <see cref="QueueBrokenException"/>.
        /// </para>
        /// <para>
        /// Use <see cref="Break"/> or <see cref="Stop"/> to break a queue.
        /// </para>
        /// <para>
        /// Use <see cref="Clear"/> to restore the queue back to normal state.
        /// </para>
        /// <seealso cref="Break"/>
        /// <seealso cref="Stop"/>
        /// <seealso cref="Clear"/>
        public virtual bool IsBroken { get { return this._isBroken; } }

        /// <summary>
        /// Breaks current queue. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// When a queue is broken, all blocking actions and subsequent put
        /// actions will return immediately with unsuccesful status or 
        /// <see cref="QueueBrokenException"/> is thrown if method doesn't 
        /// return any status. Subsequent get actions will continue to sucess
        /// until the queue becomes empty.
        /// </para>
        /// <para>
        /// <see cref="Break"/> allows remaining elements in the queue to
        /// be consumed by subsequent get actions. Use <see cref="Stop"/> 
        /// to break and clear the queue in the same time, which effectively
        /// ensures any subsequent get action to fail.
        /// </para>
        /// <para>
        /// Use <see cref="Clear"/> to restore the queue back to normal state.
        /// </para>
        /// </remarks>
        /// <seealso cref="IsBroken"/>
        /// <seealso cref="Stop"/>
        /// <seealso cref="Clear"/>
        public virtual void Break()
        {
            if (this._isBroken)
            {
                return;
            }

            lock (this._putLock)
            {
                lock (this._takeLock)
                {
                    this._isBroken = true;
                    Monitor.PulseAll(this._putLock);
                    Monitor.PulseAll(this._takeLock);
                }
            }
        }

        /// <summary>
        /// <see cref="Break"/>s and empties current queue. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// As soon as a queue is stopped, all blocking queue modification 
        /// actions return immediately with unsuccesful status or 
        /// <see cref="QueueBrokenException"/> is thrown if method doesn't 
        /// return any status. So do any subsequent queue modification
        /// actions.
        /// </para>
        /// <para>
        /// Use <see cref="Break"/> to only affect put operations but allowing
        /// get operations to continue untile queue is empty.
        /// </para>
        /// <para>
        /// Use <see cref="Clear"/> to restore the queue back to normal state.
        /// </para>
        /// </remarks>
        /// <seealso cref="Break"/>
        /// <seealso cref="IsBroken"/>
        /// <seealso cref="Clear"/>
        public virtual void Stop() { this.EmptyQueue(true); }

        /// <summary>
        /// Returns a string representation of this colleciton.
        /// </summary>
        /// <returns>String representation of the elements of this collection.</returns>
        public override string ToString()
        {
            lock (this._putLock)
            {
                lock (this._takeLock)
                {
                    return base.ToString();
                }
            }
        }

        /// <summary> 
        /// Removes all of the elements from this queue and reopen the queue if
        /// it was closed.
        /// </summary>
        /// <remarks>
        /// <p>
        /// The queue will be empty after this call returns.
        /// </p>
        /// </remarks>
        public override void Clear() { this.EmptyQueue(false); }

        #region IEnumerable Members

        /// <summary> 
        /// Returns an <see cref="IEnumerator{T}"/> over the elements in this 
        /// queue in proper sequence.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="IEnumerator{T}"/> is a "weakly consistent" 
        /// enumerator that will not throw <see cref="InvalidOperationException"/> 
        /// when the queue is concurrently modified, and guarantees to traverse
        /// elements as they existed upon construction of the enumerator, and
        /// may (but is not guaranteed to) reflect any modifications subsequent
        /// to construction.
        /// </remarks>
        /// <returns>
        /// An enumerator over the elements in this queue in proper sequence.
        /// </returns>
        public override IEnumerator<T> GetEnumerator() { return new LinkedBlockingQueueEnumerator(this); }

        /// <summary>
        /// Internal enumerator class
        /// </summary>
        private class LinkedBlockingQueueEnumerator : AbstractEnumerator<T>
        {
            private readonly LinkedBlockingQueue<T> _queue;
            private Node _currentNode;
            private T _currentElement;

            internal LinkedBlockingQueueEnumerator(LinkedBlockingQueue<T> queue)
            {
                this._queue = queue;
                lock (this._queue._putLock)
                {
                    lock (this._queue._takeLock)
                    {
                        this._currentNode = this._queue._head;
                    }
                }
            }

            /// <summary>The fetch current.</summary>
            /// <returns>The T.</returns>
            protected override T FetchCurrent() { return this._currentElement; }

            /// <summary>The go next.</summary>
            /// <returns>The System.Boolean.</returns>
            protected override bool GoNext()
            {
                lock (this._queue._putLock)
                {
                    lock (this._queue._takeLock)
                    {
                        this._currentNode = this._currentNode.Next;
                        if (this._currentNode == null)
                        {
                            return false;
                        }

                        this._currentElement = this._currentNode.Item;
                        return true;
                    }
                }
            }

            /// <summary>The do reset.</summary>
            protected override void DoReset()
            {
                lock (this._queue._putLock)
                {
                    lock (this._queue._takeLock)
                    {
                        this._currentNode = this._queue._head;
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private void EmptyQueue(bool @break)
        {
            lock (this._putLock)
            {
                lock (this._takeLock)
                {
                    this._head.Next = null;

                    this._last = this._head;
                    int c;
                    lock (this)
                    {
                        c = this._activeCount;
                        this._activeCount = 0;
                    }

                    bool pulsePut = c == this._capacity;
                    if (this._isBroken != @break)
                    {
                        this._isBroken = @break;
                        if (@break)
                        {
                            Monitor.PulseAll(this._takeLock);
                            pulsePut = true;
                        }
                    }

                    if (pulsePut)
                    {
                        Monitor.PulseAll(this._putLock);
                    }
                }
            }
        }

        /// <summary> 
        /// Signals a waiting take. Called only from put/offer (which do not
        /// otherwise ordinarily lock _takeLock.)
        /// </summary>
        private void SignalNotEmpty()
        {
            lock (this._takeLock)
            {
                Monitor.Pulse(this._takeLock);
            }
        }

        /// <summary> Signals a waiting put. Called only from take/poll.</summary>
        private void SignalNotFull()
        {
            lock (this._putLock)
            {
                Monitor.Pulse(this._putLock);
            }
        }

        /// <summary>
        /// Creates a node and links it at end of queue.</summary>
        /// <param name="x">the item to insert</param>
        private void Insert(T x) { this._last = this._last.Next = new Node(x); }

        /// <summary>Removes a node from head of queue,</summary>
        /// <returns>the node</returns>
        private T Extract()
        {
            Node first = this._head.Next;
            this._head = first;
            T x = first.Item;
            first.Item = default(T);
            return x;
        }

        #endregion
    }
}
