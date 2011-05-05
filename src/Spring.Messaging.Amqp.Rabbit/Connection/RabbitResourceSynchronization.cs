using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spring.Transaction.Support;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    	/**
	 * Callback for resource cleanup at the end of a non-native RabbitMQ transaction (e.g. when participating in a
	 * JtaTransactionManager transaction).
	 * @see org.springframework.transaction.jta.JtaTransactionManager
	 */
	internal class RabbitResourceSynchronization : TransactionSynchronizationAdapter
    {
        private readonly bool transacted;

		private readonly RabbitResourceHolder resourceHolder;

		public RabbitResourceSynchronization(RabbitResourceHolder resourceHolder, Object resourceKey, bool transacted) : base()
        {
			//super(resourceHolder, resourceKey);
			this.resourceHolder = resourceHolder;
			this.transacted = transacted;
		}

		protected bool ShouldReleaseBeforeCompletion() {
			return !this.transacted;
		}

		protected void ProcessResourceAfterCommit(RabbitResourceHolder resourceHolder) 
        {
			resourceHolder.CommitAll();
		}

		public void AfterCompletion(int status) {
			if (status != TransactionSynchronization.STATUS_COMMITTED) 
            {
				resourceHolder.rollbackAll();
			}
			//super.afterCompletion(status);
		}

		protected void releaseResource(RabbitResourceHolder resourceHolder, Object resourceKey) 
        {
			ConnectionFactoryUtils.ReleaseResources(resourceHolder);
		}
	}
}
