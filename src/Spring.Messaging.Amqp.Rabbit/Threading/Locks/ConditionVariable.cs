// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConditionVariable.cs" company="The original author or authors.">
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
#endregion

namespace Spring.Threading.Locks
{
    /// <summary>The condition variable.</summary>
    /// <author>Doug Lea</author><author>Griffin Caprio (.NET)</author><author>Kenneth Xu</author>
    [Serializable]
    internal class ConditionVariable : ICondition
    {
        // BACKPORT_3_1
        private const string _notSupportedMessage = "Use FAIR version";
        protected internal IExclusiveLock _lock;

        /// <summary>Initializes a new instance of the <see cref="ConditionVariable"/> class. 
        /// Create a new <see cref="ConditionVariable"/> that relies on the given mutual
        /// exclusion lock.</summary>
        /// <param name="lock">A non-reentrant mutual exclusion lock.</param>
        internal ConditionVariable(IExclusiveLock @lock) { this._lock = @lock; }

        /// <summary>Gets the lock.</summary>
        protected internal virtual IExclusiveLock Lock { get { return this._lock; } }

        /// <summary>Gets the wait queue length.</summary>
        /// <exception cref="NotSupportedException"></exception>
        protected internal virtual int WaitQueueLength { get { throw new NotSupportedException(_notSupportedMessage); } }

        /// <summary>Gets the waiting threads.</summary>
        /// <exception cref="NotSupportedException"></exception>
        protected internal virtual ICollection<Thread> WaitingThreads { get { throw new NotSupportedException(_notSupportedMessage); } }

        /// <summary>Gets a value indicating whether has waiters.</summary>
        /// <exception cref="NotSupportedException"></exception>
        protected internal virtual bool HasWaiters { get { throw new NotSupportedException(_notSupportedMessage); } }

        #region ICondition Members

        /// <summary>The await uninterruptibly.</summary>
        /// <exception cref="SynchronizationLockException"></exception>
        public virtual void AwaitUninterruptibly()
        {
            int holdCount = this._lock.HoldCount;
            if (holdCount == 0)
            {
                throw new SynchronizationLockException();
            }

            bool wasInterrupted = false;
            try
            {
                lock (this)
                {
                    for (int i = holdCount; i > 0; i--)
                    {
                        this._lock.Unlock();
                    }

                    try
                    {
                        Monitor.Wait(this);
                    }
                    catch (ThreadInterruptedException)
                    {
                        wasInterrupted = true;

                        // may have masked the signal and there is no way
                        // to tell; we must wake up spuriously.
                    }
                }
            }
            finally
            {
                for (int i = holdCount; i > 0; i--)
                {
                    this._lock.Lock();
                }

                if (wasInterrupted)
                {
                    Thread.CurrentThread.Interrupt();
                }
            }
        }

        /// <summary>The await.</summary>
        /// <exception cref="SynchronizationLockException"></exception>
        /// <exception cref="ThreadInterruptedException"></exception>
        public virtual void Await()
        {
            int holdCount = this._lock.HoldCount;
            if (holdCount == 0)
            {
                throw new SynchronizationLockException();
            }

            // This requires sleep(0) to implement in .Net, too expensive!
            // if (Thread.interrupted()) throw new InterruptedException();
            try
            {
                lock (this)
                {
                    for (int i = holdCount; i > 0; i--)
                    {
                        this._lock.Unlock();
                    }

                    try
                    {
                        Monitor.Wait(this);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Monitor.Pulse(this);
                        throw e.PreserveStackTrace();
                    }
                }
            }
            finally
            {
                for (int i = holdCount; i > 0; i--)
                {
                    this._lock.Lock();
                }
            }
        }

        /// <summary>The await.</summary>
        /// <param name="durationToWait">The duration to wait.</param>
        /// <returns>The System.Boolean.</returns>
        /// <exception cref="SynchronizationLockException"></exception>
        /// <exception cref="ThreadInterruptedException"></exception>
        public virtual bool Await(TimeSpan durationToWait)
        {
            int holdCount = this._lock.HoldCount;
            if (holdCount == 0)
            {
                throw new SynchronizationLockException();
            }

            // This requires sleep(0) to implement in .Net, too expensive!
            // if (Thread.interrupted()) throw new InterruptedException();
            try
            {
                lock (this)
                {
                    for (int i = holdCount; i > 0; i--)
                    {
                        this._lock.Unlock();
                    }

                    try
                    {
                        // .Net implementation is a little different than backport 3.1 
                        // by taking advantage of the return value from Monitor.Wait.
                        return (durationToWait.Ticks > 0) && Monitor.Wait(this, durationToWait);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Monitor.Pulse(this);
                        throw e.PreserveStackTrace();
                    }
                }
            }
            finally
            {
                for (int i = holdCount; i > 0; i--)
                {
                    this._lock.Lock();
                }
            }
        }

        /// <summary>The await until.</summary>
        /// <param name="deadline">The deadline.</param>
        /// <returns>The System.Boolean.</returns>
        /// <exception cref="SynchronizationLockException"></exception>
        /// <exception cref="ThreadInterruptedException"></exception>
        public virtual bool AwaitUntil(DateTime deadline)
        {
            int holdCount = this._lock.HoldCount;
            if (holdCount == 0)
            {
                throw new SynchronizationLockException();
            }

            if (deadline.Subtract(DateTime.UtcNow).Ticks <= 0)
            {
                return false;
            }

            // This requires sleep(0) to implement in .Net, too expensive!
            // if (Thread.interrupted()) throw new InterruptedException();
            try
            {
                lock (this)
                {
                    for (int i = holdCount; i > 0; i--)
                    {
                        this._lock.Unlock();
                    }

                    try
                    {
                        // .Net has DateTime precision issue so we need to retry.
                        TimeSpan durationToWait;
                        while ((durationToWait = deadline.Subtract(DateTime.UtcNow)).Ticks > 0)
                        {
                            // .Net implementation is different than backport 3.1 
                            // by taking advantage of the return value from Monitor.Wait.
                            if (Monitor.Wait(this, durationToWait))
                            {
                                return true;
                            }
                        }
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Monitor.Pulse(this);
                        throw e.PreserveStackTrace();
                    }
                }
            }
            finally
            {
                for (int i = holdCount; i > 0; i--)
                {
                    this._lock.Lock();
                }
            }

            return false;
        }

        /// <summary>The signal.</summary>
        public virtual void Signal()
        {
            lock (this)
            {
                this.AssertOwnership();
                Monitor.Pulse(this);
            }
        }

        /// <summary>The signal all.</summary>
        public virtual void SignalAll()
        {
            lock (this)
            {
                this.AssertOwnership();
                Monitor.PulseAll(this);
            }
        }

        #endregion

        /// <summary>The assert ownership.</summary>
        /// <exception cref="SynchronizationLockException"></exception>
        protected void AssertOwnership()
        {
            if (!this._lock.IsHeldByCurrentThread)
            {
                throw new SynchronizationLockException();
            }
        }

        internal interface IExclusiveLock : ILock
        {
            /// <summary>Gets a value indicating whether is held by current thread.</summary>
            bool IsHeldByCurrentThread { get; }

            /// <summary>Gets the hold count.</summary>
            int HoldCount { get; }
        }
    }
}
