#region License
/*
* Copyright (C) 2002-2009 the original author or authors.
* 
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
* 
*      http://www.apache.org/licenses/LICENSE-2.0
* 
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Spring.Collections.Generic;
using Spring.Threading.Locks;
using Spring.Utility;

namespace Spring.Threading.Collections.Generic
{
    /// <summary>
    /// A <see cref="IBlockingQueue{T}">blocking queue</see> in which each 
    /// insert operation must wait for a corresponding remove operation by 
    /// another thread, and vice versa.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A synchronous queue does not have any internal capacity, not even 
    /// a capacity of one.  You cannot <see cref="Peek"/> at a synchronous 
    /// queue because an element is only present when you try to remove it; 
    /// you cannot insert an element (using any method) unless another thread 
    /// is trying to remove it; you cannot iterate as there is nothing to 
    /// iterate.  The <i>head</i> of the queue is the element that the first 
    /// queued inserting thread is trying to add to the queue; if there is no 
    /// such queued thread then no element is available for removal and
    /// <see cref="Poll(out T)"/> will return <c>false</c>.  For purposes of 
    /// other <see cref="ICollection{T}"/> methods (for example <c>Contains</c>), 
    /// a <see cref="SynchronousQueue{T}"/> acts as an empty collection.
    /// </para>
    /// <para>
    /// Synchronous queues are similar to rendezvous channels used in
    /// CSP and Ada. They are well suited for handoff designs, in which an
    /// object running in one thread must sync up with an object running
    /// in another thread in order to hand it some information, event, or
    /// task.
    /// </para>
    /// <para>
    /// This class supports an optional fairness policy for ordering
    /// waiting producer and consumer threads.  By default, this ordering
    /// is not guaranteed. However, a queue constructed with fairness set
    /// to <c>true</c> grants threads access in FIFO order. Fairness
    /// generally decreases throughput but reduces variability and avoids
    /// starvation.
    /// </para>
    /// </remarks>
    /// <author>Doug Lea</author>
    /// <author>Andreas Döhring (.NET)</author>
    /// <author>Kenneth Xu</author>
    [Serializable]
    public class SynchronousQueue<T> : AbstractBlockingQueue<T>    //BACKPORT_3_1
    {
        /*
          This implementation divides actions into two cases for puts:

          * An arriving producer that does not already have a waiting consumer
            creates a node holding item, and then waits for a consumer to take it.
          * An arriving producer that does already have a waiting consumer fills
            the slot node created by the consumer, and notifies it to continue.

          And symmetrically, two for takes:

          * An arriving consumer that does not already have a waiting producer
            creates an empty slot node, and then waits for a producer to fill it.
          * An arriving consumer that does already have a waiting producer takes
            item from the node created by the producer, and notifies it to continue.

          When a put or take waiting for the actions of its counterpart
          aborts due to interruption or timeout, it marks the node
          it created as "CANCELLED", which causes its counterpart to retry
          the entire put or take sequence.

          This requires keeping two simple queues, waitingProducers and
          waitingConsumers. Each of these can be FIFO (preserves fairness)
          or LIFO (improves throughput).
        */

        /** Lock protecting both wait queues */
        private readonly ReentrantLock _qlock;
        /** Queue holding waiting puts */
        private readonly IWaitQueue _waitingProducers;
        /** Queue holding waiting takes */
        private readonly IWaitQueue _waitingConsumers;

        /// <summary>
        /// Creates a <see cref="SynchronousQueue{T}"/> with nonfair access policy.
        /// </summary>
        public SynchronousQueue() : this(false) { }

        /// <summary>
        /// Creates a <see cref="SynchronousQueue{T}"/> with specified fairness policy.
        /// </summary>
        /// <param name="fair">
        /// if true, threads contend in FIFO order for access otherwise the order is unspecified.
        /// </param>
        public SynchronousQueue(bool fair)
        {
            if (fair)
            {
                _qlock = new ReentrantLock(true);
                _waitingProducers = new FifoWaitQueue();
                _waitingConsumers = new FifoWaitQueue();
            }
            else
            {
                _qlock = new ReentrantLock();
                _waitingProducers = new LifoWaitQueue();
                _waitingConsumers = new LifoWaitQueue();
            }
        }

        /// <summary>
        /// Queue to hold waiting puts/takes; specialized to Fifo/Lifo below.
        /// These queues have all transient fields, but are serializable
        /// in order to recover fairness settings when deserialized.
        /// </summary>
        private interface IWaitQueue
        {
            /// <summary>
            /// Creates, adds, and returns node for x
            /// </summary>
            Node Enqueue(T x);

            /// <summary>
            /// Removes and returns node, or null if empty.
            /// </summary>
            Node Dequeue();

            /// <summary>
            /// Removes a cancelled node to avoid garbage retention.
            /// </summary>
            void Unlink(Node node);

            /// <summary>
            /// Returns true if a cancelled node might be on queue.
            /// </summary>
            bool ShouldUnlink(Node node);
        }

        /// <summary>
        /// FIFO queue to hold waiting puts/takes.
        /// </summary>
        [Serializable]
        private class FifoWaitQueue : IWaitQueue
        {
            [NonSerialized]
            private Node _head;
            [NonSerialized]
            private Node _last;

            public Node Enqueue(T x)
            {
                Node p = new Node(x);
                if (_last == null)
                    _last = _head = p;
                else
                    _last = _last.Next = p;
                return p;
            }

            public Node Dequeue()
            {
                Node p = _head;
                if (p != null)
                {
                    if ((_head = p.Next) == null)
                        _last = null;
                    p.Next = null;
                }
                return p;
            }

            public bool ShouldUnlink(Node node)
            {
                return (node == _last || node.Next != null);
            }

            public void Unlink(Node node)
            {
                Node p = _head;
                Node trail = null;
                while (p != null)
                {
                    if (p == node)
                    {
                        Node next = p.Next;
                        if (trail == null)
                            _head = next;
                        else
                            trail.Next = next;
                        if (_last == node)
                            _last = trail;
                        break;
                    }
                    trail = p;
                    p = p.Next;
                }
            }
        }

        /// <summary>
        /// LIFO queue to hold waiting puts/takes.
        /// </summary>
        [Serializable]
        private class LifoWaitQueue : IWaitQueue
        {
            [NonSerialized]
            private Node _head;

            public Node Enqueue(T x)
            {
                return _head = new Node(x, _head);
            }

            public Node Dequeue()
            {
                Node p = _head;
                if (p != null)
                {
                    _head = p.Next;
                    p.Next = null;
                }
                return p;
            }

            public bool ShouldUnlink(Node node)
            {
                // Return false if already dequeued or is bottom node (in which
                // case we might retain at most one garbage node)
                return (node == _head || node.Next != null);
            }

            public void Unlink(Node node)
            {
                Node p = _head;
                Node trail = null;
                while (p != null)
                {
                    if (p == node)
                    {
                        Node next = p.Next;
                        if (trail == null)
                            _head = next;
                        else
                            trail.Next = next;
                        break;
                    }
                    trail = p;
                    p = p.Next;
                }
            }
        }

        /// <summary>
        /// Unlinks the given node from consumer queue.  Called by cancelled
        /// (timeout, interrupt) waiters to avoid garbage retention in the
        /// absence of producers.
        /// </summary>
        private void UnlinkCancelledConsumer(Node node)
        {
            // Use a form of double-check to avoid unnecessary locking and
            // traversal. The first check outside lock might
            // conservatively report true.
            if (_waitingConsumers.ShouldUnlink(node))
            {
                using (_qlock.Lock())
                {
                    if (_waitingConsumers.ShouldUnlink(node))
                        _waitingConsumers.Unlink(node);
                }
            }
        }

        /// <summary>
        /// Unlinks the given node from producer queue.  Symmetric to 
        /// <see cref="UnlinkCancelledConsumer"/>.
        /// </summary>
        private void UnlinkCancelledProducer(Node node)
        {
            if (_waitingProducers.ShouldUnlink(node))
            {
                using (_qlock.Lock())
                {
                    if (_waitingProducers.ShouldUnlink(node))
                        _waitingProducers.Unlink(node);
                }
            }
        }

        /// <summary>
        /// Nodes each maintain an item and handle waits and signals for
        /// getting and setting it. The class extends
        /// AbstractQueuedSynchronizer to manage blocking, using AQS state
        ///  0 for waiting, 1 for ack, -1 for cancelled.
        /// </summary>
        [Serializable]
        private class Node
        {
            /** Synchronization state value representing that node acked */
            private const int Ack = 1;
            /** Synchronization state value representing that node cancelled */
            private const int Cancel = -1;


            int _state;

            /** The item being transferred */
            private T _item;
            /** Next node in wait queue */
            internal Node Next;

            /** Creates a node with initial item */
            public Node(T x) { _item = x; }

            /** Creates a node with initial item and next */
            public Node(T x, Node n) { _item = x; Next = n; }

            /**
             * Takes item and nulls out field (for sake of GC)
             *
             * PRE: lock owned
             */
            private T Extract()
            {
                T x = _item;
                _item = default(T);
                return x;
            }

            /**
             * Tries to cancel on interrupt; if so rethrowing,
             * else setting interrupt state
             *
             * PRE: lock owned
             */
            private void CheckCancellationOnInterrupt(ThreadInterruptedException ie)
            {
                if (_state == 0)
                {
                    _state = Cancel;
                    Monitor.Pulse(this);
                    throw SystemExtensions.PreserveStackTrace(ie);
                }
                Thread.CurrentThread.Interrupt();
            }

            /**
             * Fills in the slot created by the consumer and signal consumer to
             * continue.
             */
            public bool SetItem(T x)
            {
                lock (this)
                {
                    if (_state != 0) return false;
                    _item = x;
                    _state = Ack;
                    Monitor.Pulse(this);
                    return true;
                }
            }

            /**
             * Removes item from slot created by producer and signal producer
             * to continue.
             */
            public bool GetItem(out T item)
            {
                lock (this)
                {
                    if (_state != 0)
                    {
                        item = default(T);
                        return false;
                    }
                    _state = Ack;
                    Monitor.Pulse(this);
                    item = Extract();
                    return true;
                }
            }

            /**
             * Waits for a consumer to take item placed by producer.
             */
            public void WaitForTake()
            {
                lock (this)
                {
                    try
                    {
                        while (_state == 0) Monitor.Wait(this);
                    }
                    catch (ThreadInterruptedException ie)
                    {
                        CheckCancellationOnInterrupt(ie);
                    }
                }
            }

            /**
             * Waits for a producer to put item placed by consumer.
             */
            public T WaitForPut()
            {
                lock (this)
                {
                    try
                    {
                        while (_state == 0) Monitor.Wait(this);
                    }
                    catch (ThreadInterruptedException ie)
                    {
                        CheckCancellationOnInterrupt(ie);
                    }
                    return Extract();
                }
            }

            private bool Attempt(TimeSpan duration)
            {
                if (_state != 0) return true;
                if (duration.Ticks <= 0)
                {
                    _state = Cancel;
                    Monitor.Pulse(this);
                    return false;
                }
                DateTime deadline = WaitTime.Deadline(duration);
                while (true)
                {
                    Monitor.Wait(this, WaitTime.Cap(duration));
                    if (_state != 0) return true;
                    duration = deadline.Subtract(DateTime.UtcNow);
                    if (duration.Ticks <= 0)
                    {
                        _state = Cancel;
                        Monitor.Pulse(this);
                        return false;
                    }
                }
            }

            /**
             * Waits for a consumer to take item placed by producer or time out.
             */
            public bool WaitForTake(TimeSpan duration)
            {
                lock (this)
                {
                    try
                    {
                        if (!Attempt(duration)) return false;
                    }
                    catch (ThreadInterruptedException ie)
                    {
                        CheckCancellationOnInterrupt(ie);
                    }
                    return true;
                }
            }

            /**
             * Waits for a producer to put item placed by consumer, or time out.
             */
            public bool WaitForPut(TimeSpan duration, out T element)
            {
                lock (this)
                {
                    try
                    {
                        if (!Attempt(duration))
                        {
                            element = default(T);
                            return false;
                        }
                    }
                    catch (ThreadInterruptedException ie)
                    {
                        CheckCancellationOnInterrupt(ie);
                    }
                    element = Extract();
                    return true;
                }
            }
        }

        /// <summary> 
        /// Inserts the specified element into this queue, waiting if necessary
        /// another thread to receive it.
        /// </summary>
        /// <param name="element">the element to add</param>
        /// <exception cref="ThreadInterruptedException">
        /// if interrupted while waiting.
        /// </exception>
        public override void Put(T element)
        {
            for (; ; )
            {
                Node node;
                bool mustWait;
                //if (Thread.Interrupted) throw new InterruptedException();
                using (_qlock.Lock())
                {
                    node = _waitingConsumers.Dequeue();
                    mustWait = (node == null);
                    if (mustWait)
                        node = _waitingProducers.Enqueue(element);
                }

                if (mustWait)
                {
                    try
                    {
                        node.WaitForTake();
                        return;
                    }
                    catch (ThreadInterruptedException tie)
                    {
                        UnlinkCancelledProducer(node);
                        throw SystemExtensions.PreserveStackTrace(tie);
                    }
                }

                else if (node.SetItem(element))
                    return;

                // else consumer cancelled, so retry
            }
        }

        /// <summary> 
        /// Inserts the specified element into this queue, waiting up to the
        /// specified wait time if necessary for another thread to receive it.
        /// </summary>
        /// <param name="element">The element to add.</param>
        /// <param name="duration">How long to wait before giving up.</param>
        /// <returns>
        /// <see langword="true"/> if successful, or <see langword="false"/> if
        /// the specified waiting time elapses before space is available.
        /// </returns>
        /// <exception cref="ThreadInterruptedException">
        /// if interrupted while waiting.
        /// </exception>
        public override bool Offer(T element, TimeSpan duration)
        {
            for (; ; )
            {
                Node node;
                bool mustWait;
                //if (Thread.interrupted()) throw new InterruptedException();
                using (_qlock.Lock())
                {
                    node = _waitingConsumers.Dequeue();
                    mustWait = (node == null);
                    if (mustWait)
                        node = _waitingProducers.Enqueue(element);
                }

                if (mustWait)
                {
                    try
                    {
                        bool x = node.WaitForTake(duration);
                        if (!x)
                            UnlinkCancelledProducer(node);
                        return x;
                    }
                    catch (ThreadInterruptedException tie)
                    {
                        UnlinkCancelledProducer(node);
                        throw SystemExtensions.PreserveStackTrace(tie);
                    }
                }

                else if (node.SetItem(element))
                    return true;

                // else consumer cancelled, so retry
            }
        }

        /// <summary> 
        /// Retrieves and removes the head of this queue, waiting if necessary
        /// until another thread inserts it.
        /// </summary>
        /// <returns> the head of this queue</returns>
        /// <exception cref="ThreadInterruptedException">
        /// if interrupted while waiting.
        /// </exception>
        public override T Take()
        {
            for (; ; )
            {
                Node node;
                bool mustWait;

                //if (Thread.interrupted()) throw new InterruptedException();
                using (_qlock.Lock())
                {
                    node = _waitingProducers.Dequeue();
                    mustWait = (node == null);
                    if (mustWait)
                        node = _waitingConsumers.Enqueue(default(T));
                }

                if (mustWait)
                {
                    try
                    {
                        return node.WaitForPut();
                    }
                    catch (ThreadInterruptedException e)
                    {
                        UnlinkCancelledConsumer(node);
                        throw SystemExtensions.PreserveStackTrace(e);
                    }
                }
                else
                {
                    T x;
                    if (node.GetItem(out x))
                        return x;
                    // else cancelled, so retry
                }
            }
        }

        /// <summary> 
        /// Retrieves and removes the head of this queue, waiting up to the
        /// specified wait time if necessary for another thread to insert it.
        /// </summary>
        /// <param name="element">
        /// Set to the head of this queue. <c>default(T)</c> if queue is empty.
        /// </param>
        /// <param name="duration">How long to wait before giving up.</param>
        /// <returns> 
        /// <c>false</c> if the queue is still empty after waited for the time 
        /// specified by the <paramref name="duration"/>. Otherwise <c>true</c>.
        /// </returns>
        public override bool Poll(TimeSpan duration, out T element)
        {
            for (; ; )
            {
                Node node;
                bool mustWait;

                //if (Thread.interrupted()) throw new InterruptedException();
                using (_qlock.Lock())
                {
                    node = _waitingProducers.Dequeue();
                    mustWait = (node == null);
                    if (mustWait)
                        node = _waitingConsumers.Enqueue(default(T));
                }

                if (mustWait)
                {
                    try
                    {
                        T x;
                        bool success = node.WaitForPut(duration, out x);
                        if (!success) UnlinkCancelledConsumer(node);
                        element = x;
                        return success;
                    }
                    catch (ThreadInterruptedException e)
                    {
                        UnlinkCancelledConsumer(node);
                        throw SystemExtensions.PreserveStackTrace(e);
                    }
                }
                else
                {
                    T x;
                    if (node.GetItem(out x))
                    {
                        element = x;
                        return true;
                    }
                    // else cancelled, so retry
                }
            }
        }

        /// <summary>
        /// Inserts the specified element into this queue if another thread if
        /// waiting to receive it. Otherwise return <c>false</c> immediately.
        /// </summary>
        /// <param name="element">The element to add.</param>
        /// <returns>
        /// <c>true</c> if the element was added to this queue. Otherwise 
        /// <c>false</c>.
        /// </returns>
        public override bool Offer(T element)
        {
            for (; ; )
            {
                Node node;
                using (_qlock.Lock())
                {
                    node = _waitingConsumers.Dequeue();
                }
                if (node == null)
                    return false;

                else if (node.SetItem(element))
                    return true;
                // else retry
            }
        }

        /// <summary>
        /// Retrieves and removes the head of this queue into out parameter
        /// <paramref name="element"/>, if another thead is currently making
        /// an element available. Otherwise return <c>false</c> immediately.
        /// </summary>
        /// <param name="element">
        /// Set to the head of this queue. <c>default(T)</c> if queue is empty.
        /// </param>
        /// <returns>
        /// <c>false</c> if the queue is empty. Otherwise <c>true</c>.
        /// </returns>
        public override bool Poll(out T element)
        {
            for (; ; )
            {
                Node node;
                using (_qlock.Lock())
                {
                    node = _waitingProducers.Dequeue();
                }
                if (node == null)
                {
                    element = default(T);
                    return false;
                }

                else
                {
                    T x;
                    if (node.GetItem(out x))
                    {
                        element = x;
                        return true;
                    }
                    // else retry
                }
            }
        }

        /// <summary>
        /// Always returns <c>true</c>.
        /// A <see cref="SynchronousQueue{T}"/> has no internal capacity.
        /// </summary>
        public override bool IsEmpty
        {
            get { return true; }
        }

        /// <summary>
        /// Always returns zero(0).
        /// A <see cref="SynchronousQueue{T}"/> has no internal capacity.
        /// </summary>
        public override int Count
        {
            get { return 0; }
        }

        /// <summary>
        /// Always returns zero.
        /// A <see cref="SynchronousQueue{T}"/> has no internal capacity.
        /// </summary>
        public override int RemainingCapacity
        {
            get { return 0; }
        }

        /// <summary>
        /// Does nothing. A <see cref="SynchronousQueue{T}"/>
        /// has no internal capacity.
        /// </summary>
        public override void Clear()
        {
        }

        /// <summary>
        /// Always returns <c>false</c>. A <see cref="SynchronousQueue{T}"/>
        /// has no internal capacity.
        /// </summary>
        /// <returns>Always <c>false</c>.</returns>
        /// <param name="item">
        /// The object to locate in the <see cref="SynchronousQueue{T}"/>.
        /// </param>
        public override bool Contains(T item)
        {
            return false;
        }

        /// <summary>
        /// Always returns <c>false</c>. A <see cref="SynchronousQueue{T}"/>
        /// has no internal capacity.
        /// </summary>
        /// <returns>Always <c>false</c>.</returns>
        /// <param name="item">
        /// The object to remove from the <see cref="ICollection{T}"/>.
        /// </param>
        public override bool Remove(T item)
        {
            return false;
        }

        /// <summary>
        /// Always returns <c>false</c>. A <see cref="SynchronousQueue{T}"/>
        /// does not return elements unless actively waited on.
        /// </summary>
        /// <param name="element">
        /// Always set to <c>default(T)</c>.
        /// </param>
        /// <returns>Always <c>false</c>.</returns>
        public override bool Peek(out T element)
        {
            element = default(T);
            return false;
        }

        private static readonly IList<T> EmptyList = new T[0];

        /// <summary>
        /// Returns an empty enumerator in which <see cref="IEnumerator.MoveNext"/>
        /// always returns <c>false</c>.
        /// </summary>
        /// <returns>
        /// An empty <see cref="IEnumerator{T}"/>.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override IEnumerator<T> GetEnumerator()
        {
            return EmptyList.GetEnumerator();
        }


        /// <summary>
        /// Does nothing. A <see cref="SynchronousQueue{T}"/>
        /// has no internal capacity.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the 
        /// destination of the elements copied from <see cref="ICollection{T}"/>. 
        /// The <see cref="Array"/> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// Ignored.
        /// </param>
        /// <param name="ensureCapacity">
        /// Ignored.
        /// </param>
        /// <returns>
        /// The <paramref name="array"/> instance itself.
        /// </returns>
        protected override T[] DoCopyTo(T[] array, int arrayIndex, bool ensureCapacity)
        {
            return array ?? new T[0];
        }

        /// <summary> 
        /// Does the real work for all <c>Drain</c> methods. Caller must
        /// guarantee the <paramref name="action"/> is not <c>null</c> and
        /// <paramref name="maxElements"/> is greater then zero (0).
        /// </summary>
        /// <remarks>
        /// Since the queue has no capacity. This method does nothing and 
        /// returns zero(0) if <paramref name="criteria"/> is not <c>null</c>.
        /// Otherwise, one or zero element will be processed depends on the
        /// result of <see cref="Poll(out T)"/>.
        /// </remarks>
        /// <seealso cref="IBlockingQueue{T}.DrainTo(ICollection{T})"/>
        /// <seealso cref="IBlockingQueue{T}.DrainTo(ICollection{T}, int)"/>
        /// <seealso cref="IQueue{T}.Drain(System.Action{T})"/>
        /// <seealso cref="IBlockingQueue{T}.DrainTo(ICollection{T},int)"/>
        internal protected override int DoDrain(Action<T> action, int maxElements, Predicate<T> criteria)
        {
            if (criteria == null)
            {
                int n = 0;
                T element;
                while (n < maxElements && Poll(out element))
                {
                    action(element);
                    ++n;
                }
                return n;
            }
            return 0;
        }

        /// <summary>
        /// Always returns zero.
        /// A <see cref="SynchronousQueue{T}"/> has no internal capacity.
        /// </summary>
        public override int Capacity
        {
            get { return 0; }
        }
    }
}