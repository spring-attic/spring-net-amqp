// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitResourceSynchronization.cs" company="The original author or authors.">
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
using Spring.Transaction.Support;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// Callback for resource cleanup at the end of a non-native RabbitMQ transaction (e.g. when participating in a
    /// JtaTransactionManager transaction).
    /// </summary>
    internal class RabbitResourceSynchronization : TransactionSynchronizationAdapter
    {
        /// <summary>
        /// </summary>
        private readonly bool transacted;

        /// <summary>
        /// </summary>
        private readonly RabbitResourceHolder resourceHolder;

        /// <summary>Initializes a new instance of the <see cref="RabbitResourceSynchronization"/> class.</summary>
        /// <param name="resourceHolder">The resource holder.</param>
        /// <param name="resourceKey">The resource key.</param>
        /// <param name="transacted">The transacted.</param>
        public RabbitResourceSynchronization(RabbitResourceHolder resourceHolder, object resourceKey, bool transacted)
        {
            // super(resourceHolder, resourceKey);
            this.resourceHolder = resourceHolder;
            this.transacted = transacted;
        }

        /// <summary>
        /// Flag indicating whether the resources should be released before completion.
        /// </summary>
        /// <returns>
        /// True if resources should be released; False if not.
        /// </returns>
        protected bool ShouldReleaseBeforeCompletion() { return !this.transacted; }

        /// <summary>Process resources after commit.</summary>
        /// <param name="resourceHolder">The resource holder.</param>
        protected void ProcessResourceAfterCommit(RabbitResourceHolder resourceHolder) { resourceHolder.CommitAll(); }

        /// <summary>Actions to be done after completion.</summary>
        /// <param name="status">The status.</param>
        public void AfterCompletion(int status)
        {
            if (status != (int)TransactionSynchronizationStatus.Committed)
            {
                this.resourceHolder.RollbackAll();
            }

            if (this.resourceHolder.ReleaseAfterCompletion)
            {
                this.resourceHolder.SynchronizedWithTransaction = false;
            }

            this.AfterCompletion((TransactionSynchronizationStatus)status);
        }

        /// <summary>Release the resource.</summary>
        /// <param name="resourceHolder">The resource holder.</param>
        /// <param name="resourceKey">The resource key.</param>
        protected void ReleaseResource(RabbitResourceHolder resourceHolder, object resourceKey) { ConnectionFactoryUtils.ReleaseResources(resourceHolder); }
    }
}
