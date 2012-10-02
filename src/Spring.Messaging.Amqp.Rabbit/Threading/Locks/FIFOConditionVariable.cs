// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FIFOConditionVariable.cs" company="The original author or authors.">
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
using System.Threading;
using Spring.Threading.Helpers;
#endregion

namespace Spring.Threading.Locks
{
    /// <summary>The fifo condition variable.</summary>
    /// <author>Doug Lea</author><author>Griffin Caprio (.NET)</author><author>Kenneth Xu</author>
    [Serializable]
    internal class FIFOConditionVariable : ConditionVariable
    {
        // BACKPORT_3_1
        private static readonly IQueuedSync _sync = new Sync();

        private readonly IWaitQueue _wq = new FIFOWaitQueue();

        private class Sync : IQueuedSync
        {
            /// <summary>The recheck.</summary>
            /// <param name="node">The node.</param>
            /// <returns>The System.Boolean.</returns>
            public bool Recheck(WaitNode node) { return false; }

            /// <summary>The take over.</summary>
            /// <param name="node">The node.</param>
            public void TakeOver(WaitNode node) { }
        }

        /// <summary>Gets the wait queue length.</summary>
        protected internal override int WaitQueueLength
        {
            get
            {
                this.AssertOwnership();
                return this._wq.Length;
            }
        }

        /// <summary>Gets the waiting threads.</summary>
        protected internal override ICollection<Thread> WaitingThreads
        {
            get
            {
                this.AssertOwnership();
                return this._wq.WaitingThreads;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="FIFOConditionVariable"/> class. Create a new <see cref="FIFOConditionVariable"/> that relies on the
        /// given mutual exclusion lock.</summary>
        /// <param name="lock">A non-reentrant mutual exclusion lock.</param>
        internal FIFOConditionVariable(IExclusiveLock @lock)
            : base(@lock) { }

        /// <summary>The await uninterruptibly.</summary>
        public override void AwaitUninterruptibly() { this.DoWait(n => n.DoWaitUninterruptibly(_sync)); }

        /// <summary>The await.</summary>
        public override void Await() { this.DoWait(n => n.DoWait(_sync)); }

        /// <summary>The await.</summary>
        /// <param name="timespan">The timespan.</param>
        /// <returns>The System.Boolean.</returns>
        public override bool Await(TimeSpan timespan)
        {
            bool success = false;
            this.DoWait(n => success = n.DoTimedWait(_sync, timespan));
            return success;
        }

        /// <summary>The await until.</summary>
        /// <param name="deadline">The deadline.</param>
        /// <returns>The System.Boolean.</returns>
        public override bool AwaitUntil(DateTime deadline) { return this.Await(deadline.Subtract(DateTime.UtcNow)); }

        /// <summary>The signal.</summary>
        public override void Signal()
        {
            this.AssertOwnership();
            for (;;)
            {
                WaitNode w = this._wq.Dequeue();
                if (w == null)
                {
                    return; // no one to signal
                }

                if (w.Signal(_sync))
                {
                    return; // notify if still waiting, else skip
                }
            }
        }

        /// <summary>The signal all.</summary>
        public override void SignalAll()
        {
            this.AssertOwnership();
            for (;;)
            {
                WaitNode w = this._wq.Dequeue();
                if (w == null)
                {
                    return; // no more to signal
                }

                w.Signal(_sync);
            }
        }

        /// <summary>Gets a value indicating whether has waiters.</summary>
        protected internal override bool HasWaiters
        {
            get
            {
                this.AssertOwnership();
                return this._wq.HasNodes;
            }
        }

        private void DoWait(Action<WaitNode> action)
        {
            int holdCount = this.Lock.HoldCount;
            if (holdCount == 0)
            {
                throw new SynchronizationLockException();
            }

            var n = new WaitNode();
            this._wq.Enqueue(n);
            for (int i = holdCount; i > 0; i--)
            {
                this.Lock.Unlock();
            }

            try
            {
                action(n);
            }
            finally
            {
                for (int i = holdCount; i > 0; i--)
                {
                    this.Lock.Lock();
                }
            }
        }
    }
}
