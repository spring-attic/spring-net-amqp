
using System;
using Spring.Transaction.Support;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /**
 * Callback for resource cleanup at the end of a non-native RabbitMQ transaction (e.g. when participating in a
 * JtaTransactionManager transaction).
 * @see org.springframework.transaction.jta.JtaTransactionManager
 */

    /// <summary>
    /// Rabbit resource synchronization implementation.
    /// </summary>
    internal class RabbitResourceSynchronization : TransactionSynchronizationAdapter
    {
        /// <summary>
        /// </summary>
        private readonly bool transacted;

        /// <summary>
        /// </summary>
        private readonly RabbitResourceHolder resourceHolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitResourceSynchronization"/> class.
        /// </summary>
        /// <param name="resourceHolder">
        /// The resource holder.
        /// </param>
        /// <param name="resourceKey">
        /// The resource key.
        /// </param>
        /// <param name="transacted">
        /// The transacted.
        /// </param>
        public RabbitResourceSynchronization(RabbitResourceHolder resourceHolder, object resourceKey, bool transacted)
        {
            //super(resourceHolder, resourceKey);
            this.resourceHolder = resourceHolder;
            this.transacted = transacted;
        }

        /// <summary>
        /// Flag indicating whether the resources should be released before completion.
        /// </summary>
        /// <returns>
        /// True if resources should be released; False if not.
        /// </returns>
        protected bool ShouldReleaseBeforeCompletion()
        {
            return !this.transacted;
        }

        /// <summary>
        /// Process resources after commit.
        /// </summary>
        /// <param name="resourceHolder">
        /// The resource holder.
        /// </param>
        protected void ProcessResourceAfterCommit(RabbitResourceHolder resourceHolder)
        {
            resourceHolder.CommitAll();
        }

        /// <summary>
        /// Actions to be done after completion.
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        public void AfterCompletion(int status)
        {
            if (status != (int)TransactionSynchronizationStatus.Committed)
            {
                this.resourceHolder.RollbackAll();
            }
            
            base.AfterCompletion((TransactionSynchronizationStatus)status);
        }

        /// <summary>
        /// Release the resource.
        /// </summary>
        /// <param name="resourceHolder">
        /// The resource holder.
        /// </param>
        /// <param name="resourceKey">
        /// The resource key.
        /// </param>
        protected void ReleaseResource(RabbitResourceHolder resourceHolder, object resourceKey)
        {
            ConnectionFactoryUtils.ReleaseResources(resourceHolder);
        }
    }
}
