// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WaitNode.cs" company="The original author or authors.">
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
using System.Threading;
#endregion

namespace Spring.Threading.Helpers
{
    /// <summary>
    /// The wait node used by implementations of <see cref="IWaitQueue"/>.
    /// NOTE: this class is NOT present in java.util.concurrent.
    /// </summary>
    /// <author>Doug Lea</author>
    /// <author>Griffin Caprio (.NET)</author>
    /// <author>Kenneth Xu</author>
    internal class WaitNode
    {
        // was WaitQueue.WaitNode in BACKPORT_3_1
        internal Thread _owner;
        internal bool _waiting = true;
        internal WaitNode _nextWaitNode;

        /// <summary>Initializes a new instance of the <see cref="WaitNode"/> class.</summary>
        public WaitNode() { this._owner = Thread.CurrentThread; }

        internal virtual Thread Owner { get { return this._owner; } }

        internal virtual bool IsWaiting { get { return this._waiting; } }

        internal virtual WaitNode NextWaitNode { get { return this._nextWaitNode; } set { this._nextWaitNode = value; } }

        /// <summary>The signal.</summary>
        /// <param name="sync">The sync.</param>
        /// <returns>The System.Boolean.</returns>
        public virtual bool Signal(IQueuedSync sync)
        {
            lock (this)
            {
                bool signalled = this._waiting;
                if (signalled)
                {
                    this._waiting = false;
                    Monitor.Pulse(this);
                    sync.TakeOver(this);
                }

                return signalled;
            }
        }

        /// <summary>The do timed wait.</summary>
        /// <param name="sync">The sync.</param>
        /// <param name="duration">The duration.</param>
        /// <returns>The System.Boolean.</returns>
        /// <exception cref="ThreadInterruptedException"></exception>
        public virtual bool DoTimedWait(IQueuedSync sync, TimeSpan duration)
        {
            lock (this)
            {
                if (sync.Recheck(this) || !this._waiting)
                {
                    return true;
                }

                if (duration.Ticks <= 0)
                {
                    this._waiting = false;
                    return false;
                }

                DateTime deadline = DateTime.UtcNow.Add(duration);
                try
                {
                    for (;;)
                    {
                        Monitor.Wait(this, duration);
                        if (!this._waiting)
                        {
                            // definitely signalled
                            return true;
                        }

                        duration = deadline.Subtract(DateTime.UtcNow);
                        if (duration.Ticks <= 0)
                        {
                            // time out
                            this._waiting = false;
                            return false;
                        }
                    }
                }
                catch (ThreadInterruptedException ex)
                {
                    if (this._waiting)
                    {
                        // no notification
                        this._waiting = false; // invalidate for the signaller
                        throw ex.PreserveStackTrace();
                    }

                    // thread was interrupted after it was notified
                    Thread.CurrentThread.Interrupt();
                    return true;
                }
            }
        }

        /// <summary>The do wait.</summary>
        /// <param name="sync">The sync.</param>
        /// <exception cref="ThreadInterruptedException"></exception>
        public virtual void DoWait(IQueuedSync sync)
        {
            lock (this)
            {
                if (!sync.Recheck(this))
                {
                    try
                    {
                        while (this._waiting)
                        {
                            Monitor.Wait(this);
                        }
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        if (this._waiting)
                        {
                            // no notification
                            this._waiting = false; // invalidate for the signaller
                            throw ex.PreserveStackTrace();
                        }

                        // thread was interrupted after it was notified
                        Thread.CurrentThread.Interrupt();
                        return;
                    }
                }
            }
        }

        /// <summary>The do wait uninterruptibly.</summary>
        /// <param name="sync">The sync.</param>
        public virtual void DoWaitUninterruptibly(IQueuedSync sync)
        {
            lock (this)
            {
                if (!sync.Recheck(this))
                {
                    bool wasInterrupted = false;
                    while (this._waiting)
                    {
                        try
                        {
                            Monitor.Wait(this);
                        }
                        catch (ThreadInterruptedException)
                        {
                            wasInterrupted = true;

                            // no need to notify; if we were signalled, we
                            // must be not waiting, and we'll act like signalled
                        }
                    }

                    if (wasInterrupted)
                    {
                        Thread.CurrentThread.Interrupt();
                    }
                }
            }
        }
    }
}
