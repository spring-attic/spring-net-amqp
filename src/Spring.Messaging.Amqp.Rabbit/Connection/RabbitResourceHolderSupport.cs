// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitResourceHolderSupport.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Transaction;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// Convenient base class for resource holders. Features rollback-only support for nested transactions.
    /// Can expire after a certain number of seconds or milliseconds, to determine transactional timeouts.
    /// </summary>
    /// <author>Juergen Hoeller</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public abstract class RabbitResourceHolderSupport : IResourceHolder
    {
        private bool synchronizedWithTransaction;

        private bool rollbackOnly;

        private DateTime deadline;

        private int referenceCount;

        private bool isVoid;

        /// <summary>Gets or sets a value indicating whether synchronized with transaction. Mark the resource as synchronized with a transaction.</summary>
        public bool SynchronizedWithTransaction { get { return this.synchronizedWithTransaction; } set { this.synchronizedWithTransaction = value; } }

        /// <summary>Gets or sets a value indicating whether rollback only.</summary>
        public bool RollbackOnly { get { return this.rollbackOnly; } set { this.rollbackOnly = value; } }

        /// <summary>Sets the timeout in seconds.</summary>
        public int TimeoutInSeconds { set { this.TimeoutInMillis = value * 1000; } }

        /// <summary>Sets the timeout in millis.</summary>
        public long TimeoutInMillis { set { this.deadline = DateTime.UtcNow.AddMilliseconds(value); } }

        /// <summary>Return whether this object has an associated timeout.</summary>
        /// <returns>The System.Boolean.</returns>
        public bool HasTimeout() { return this.deadline != default(DateTime); }

        /// <summary>Return the expiration deadline of this object.</summary>
        /// <returns>The deadline as a System.DateTime.</returns>
        public DateTime Deadline { get { return this.deadline; } }

        /// <summary>Return the time to live for this object in seconds. Rounds up eagerly, e.g. 9.00001 returns as 10.</summary>
        /// <returns>Number of seconds until expiration.</returns>
        public int TimeToLiveInSeconds
        {
            get
            {
                var diff = this.TimeToLiveInMilliseconds / 1000;
                var secs = (int)Math.Ceiling(diff);
                this.CheckTransactionTimeout(secs <= 0);
                return secs;
            }
        }

        /// <summary>Return the time to live for this object in milliseconds.</summary>
        /// <returns>Number of millseconds until expiration.</returns>
        public double TimeToLiveInMilliseconds
        {
            get
            {
                if (this.deadline == DateTime.MinValue)
                {
                    throw new InvalidOperationException("No deadline specified for this resource holder.");
                }

                var timeToLive = this.deadline.ToMilliseconds() - DateTime.UtcNow.ToMilliseconds();
                this.CheckTransactionTimeout(timeToLive <= 0.0);
                return timeToLive;
            }
        }

        /// <summary>Set the transaction rollback-only if the deadline has been reached, and throw a TransactionTimedOutException.</summary>
        /// <param name="deadlineReached">Flag indicating deadline reached.</param>
        private void CheckTransactionTimeout(bool deadlineReached)
        {
            if (deadlineReached)
            {
                this.RollbackOnly = true;
                throw new TransactionTimedOutException("Transaction timed out: deadline was " + this.Deadline);
            }
        }

        /// <summary>Increase the reference count by one because the holder has been requested (i.e. someone requested the resource held by it).</summary>
        public void Requested() { this.referenceCount++; }

        /// <summary>Decrease the reference count by one because the holder has been released (i.e. someone released the resource held by it).</summary>
        public void Released() { this.referenceCount--; }

        /// <summary>Return whether there are still open references to this holder.</summary>
        /// <returns><value>Tre</value> if open references exist, else <value>False</value>.</returns>
        public bool IsOpen() { return this.referenceCount > 0; }

        /// <summary>Clear the transactional state of this resource holder.</summary>
        public void Clear()
        {
            this.synchronizedWithTransaction = false;
            this.rollbackOnly = false;
            this.deadline = default(DateTime);
        }

        /// <summary>Reset this resource holder - transactional state as well as reference count.</summary>
        public void Reset()
        {
            this.Clear();
            this.referenceCount = 0;
        }

        /// <summary>The unbound.</summary>
        public void Unbound() { this.isVoid = true; }

        /// <summary>The is void.</summary>
        /// <returns>The System.Boolean.</returns>
        public bool IsVoid() { return this.isVoid; }
    }
}
