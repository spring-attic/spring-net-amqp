// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTransactionManager.cs" company="The original author or authors.">
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
using System.Data;
using Common.Logging;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Objects.Factory;
using Spring.Transaction;
using Spring.Transaction.Support;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Transaction
{
    /**
/// <para>
/// {@link org.springframework.transaction.PlatformTransactionManager} implementation for a single Rabbit
/// {@link ConnectionFactory}. Binds a Rabbit Channel from the specified ConnectionFactory to the thread, potentially
/// allowing for one thread-bound channel per ConnectionFactory.
/// </para>
/// <para>
/// This local strategy is an alternative to executing Rabbit operations within, and synchronized with, external
/// transactions. This strategy is <i>not</i> able to provide XA transactions, for example in order to share transactions
/// between messaging and database access.
/// </para>
/// <para>
/// Application code is required to retrieve the transactional Rabbit resources via
/// {@link ConnectionFactoryUtils#getTransactionalResourceHolder(ConnectionFactory, boolean)} instead of a standard
/// {@link Connection#createChannel()} call with subsequent Channel creation. Spring's {@link RabbitTemplate} will
/// autodetect a thread-bound Channel and automatically participate in it.
/// </para>
/// <para>
/// <b>The use of {@link CachingConnectionFactory} as a target for this transaction manager is strongly recommended.</b>
/// CachingConnectionFactory uses a single Rabbit Connection for all Rabbit access in order to avoid the overhead of
/// repeated Connection creation, as well as maintaining a cache of Channels. Each transaction will then share the same
/// Rabbit Connection, while still using its own individual Rabbit Channel.
/// </para>
/// <para>
/// Transaction synchronization is turned off by default, as this manager might be used alongside a datastore-based
/// Spring transaction manager such as the JDBC {@link org.springframework.jdbc.datasource.DataSourceTransactionManager},
/// which has stronger needs for synchronization.
/// </para>
 * @author Dave Syer
 */

    /// <summary>
    /// A rabbit transaction manager.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IPlatformTransactionManager"/> implementation for a single Rabbit
    /// <see cref="IConnectionFactory"/>. Binds a Rabbit Channel from the specified ConnectionFactory to the thread, potentially
    /// allowing for one thread-bound channel per ConnectionFactory.
    /// </para>
    /// <para>
    /// This local strategy is an alternative to executing Rabbit operations within, and synchronized with, external
    /// transactions. This strategy is <i>not</i> able to provide XA transactions, for example in order to share transactions
    /// between messaging and database access.
    /// </para>
    /// <para>
    /// Application code is required to retrieve the transactional Rabbit resources via
    /// <see cref="ConnectionFactoryUtils.GetTransactionalResourceHolder"/> instead of a standard
    /// <see cref="IConnection.CreateChannel"/> call with subsequent Channel creation. Spring's <see cref="RabbitTemplate"/> will
    /// autodetect a thread-bound Channel and automatically participate in it.
    /// </para>
    /// <para>
    /// <b>The use of <see cref="CachingConnectionFactory"/> as a target for this transaction manager is strongly recommended.</b>
    /// CachingConnectionFactory uses a single Rabbit Connection for all Rabbit access in order to avoid the overhead of
    /// repeated Connection creation, as well as maintaining a cache of Channels. Each transaction will then share the same
    /// Rabbit Connection, while still using its own individual Rabbit Channel.
    /// </para>
    /// <para>
    /// Transaction synchronization is turned off by default, as this manager might be used alongside a datastore-based
    /// Spring transaction manager such as the JDBC {@link org.springframework.jdbc.datasource.DataSourceTransactionManager},
    /// which has stronger needs for synchronization.
    /// </para>
    /// </remarks>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class RabbitTransactionManager : AbstractPlatformTransactionManager, IResourceTransactionManager, IInitializingObject
    {
        #region Logging

        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        #endregion

        /// <summary>
        /// The connection factory.
        /// </summary>
        private IConnectionFactory connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitTransactionManager"/> class.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Create a new RabbitTransactionManager for object-style usage.
        /// </para>
        /// <para>
        /// Note: The ConnectionFactory has to be set before using the instance. This constructor can be used to prepare a
        /// RabbitTemplate via a ObjectFactory, typically setting the ConnectionFactory via setConnectionFactory.
        /// </para>
        /// <para>
        /// Turns off transaction synchronization by default, as this manager might be used alongside a datastore-based
        /// Spring transaction manager like DataSourceTransactionManager, which has stronger needs for synchronization. Only
        /// one manager is allowed to drive synchronization at any point of time.
        /// </para>
        /// </remarks>
        public RabbitTransactionManager() { this.TransactionSynchronization = TransactionSynchronizationState.Never; }

        /// <summary>Initializes a new instance of the <see cref="RabbitTransactionManager"/> class.</summary>
        /// <param name="connectionFactory">The connection factory.</param>
        public RabbitTransactionManager(IConnectionFactory connectionFactory) : this()
        {
            this.connectionFactory = connectionFactory;
            this.AfterPropertiesSet();
        }

        /// <summary>
        /// Gets or sets ConnectionFactory.
        /// </summary>
        public IConnectionFactory ConnectionFactory { get { return this.connectionFactory; } set { this.connectionFactory = value; } }

        /// <summary>
        /// Actions to perform after properties are set. Make sure the ConnectionFactory has been set.
        /// </summary>
        public void AfterPropertiesSet()
        {
            if (this.ConnectionFactory == null)
            {
                throw new ArgumentException("Property 'connectionFactory' is required");
            }
        }

        /// <summary>
        /// Gets ResourceFactory.
        /// </summary>
        public object ResourceFactory { get { return this.ConnectionFactory; } }

        /// <summary>
        /// Get the transaction.
        /// </summary>
        /// <returns>
        /// The transaction.
        /// </returns>
        protected override object DoGetTransaction()
        {
            var transactionObject = new RabbitTransactionObject();
            transactionObject.ResourceHolder = (RabbitResourceHolder)TransactionSynchronizationManager.GetResource(this.ConnectionFactory);
            return transactionObject;
        }

        /// <summary>Determines if the supplied object is an existing transaction.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <returns>True if the object is an existing transaction, else false.</returns>
        protected override bool IsExistingTransaction(object transaction)
        {
            var transactionObject = (RabbitTransactionObject)transaction;
            return transactionObject.ResourceHolder != null;
        }

        /// <summary>Do begin.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="definition">The definition.</param>
        protected override void DoBegin(object transaction, ITransactionDefinition definition)
        {
            // TODO: Figure out the right isolation level. https://jira.springsource.org/browse/SPRNET-1444
            if (definition.TransactionIsolationLevel != IsolationLevel.Unspecified)
            {
                throw new InvalidIsolationLevelException("AMQP does not support an isolation level concept");
            }

            var transactionObject = (RabbitTransactionObject)transaction;
            RabbitResourceHolder resourceHolder = null;
            try
            {
                resourceHolder = ConnectionFactoryUtils.GetTransactionalResourceHolder(this.ConnectionFactory, true);
                Logger.Debug(m => m("Created AMQP transaction on channel [{0}]", resourceHolder.Channel));

                // resourceHolder.DeclareTransactional();
                transactionObject.ResourceHolder = resourceHolder;
                transactionObject.ResourceHolder.SynchronizedWithTransaction = true;
                var timeout = this.DetermineTimeout(definition);
                if (timeout != DefaultTransactionDefinition.TIMEOUT_DEFAULT)
                {
                    transactionObject.ResourceHolder.TimeoutInSeconds = timeout;
                }

                TransactionSynchronizationManager.BindResource(this.ConnectionFactory, transactionObject.ResourceHolder);
            }
            catch (AmqpException ex)
            {
                if (resourceHolder != null)
                {
                    ConnectionFactoryUtils.ReleaseResources(resourceHolder);
                }

                throw new CannotCreateTransactionException("Could not create AMQP transaction", ex);
            }
        }

        /// <summary>Do suspend.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <returns>The object.</returns>
        protected override object DoSuspend(object transaction)
        {
            var transactionObject = (RabbitTransactionObject)transaction;
            transactionObject.ResourceHolder = null;
            return TransactionSynchronizationManager.UnbindResource(this.ConnectionFactory);
        }

        /// <summary>Do resume.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="suspendedResources">The suspended resources.</param>
        protected override void DoResume(object transaction, object suspendedResources)
        {
            var conHolder = (RabbitResourceHolder)suspendedResources;
            TransactionSynchronizationManager.BindResource(this.ConnectionFactory, conHolder);
        }

        /// <summary>Do commit.</summary>
        /// <param name="status">The status.</param>
        protected override void DoCommit(DefaultTransactionStatus status)
        {
            var transactionObject = (RabbitTransactionObject)status.Transaction;
            var resourceHolder = transactionObject.ResourceHolder;
            resourceHolder.CommitAll();
        }

        /// <summary>Do rollback.</summary>
        /// <param name="status">The status.</param>
        protected override void DoRollback(DefaultTransactionStatus status)
        {
            var transactionObject = (RabbitTransactionObject)status.Transaction;
            var resourceHolder = transactionObject.ResourceHolder;
            resourceHolder.RollbackAll();
        }

        /// <summary>Do set rollback only.</summary>
        /// <param name="status">The status.</param>
        protected override void DoSetRollbackOnly(DefaultTransactionStatus status)
        {
            var transactionObject = (RabbitTransactionObject)status.Transaction;
            transactionObject.ResourceHolder.RollbackOnly = true;
        }

        /// <summary>Do cleanup after completion.</summary>
        /// <param name="transaction">The transaction.</param>
        protected void DoCleanupAfterCompletion(object transaction)
        {
            var transactionObject = (RabbitTransactionObject)transaction;
            TransactionSynchronizationManager.UnbindResource(this.ConnectionFactory);
            transactionObject.ResourceHolder.CloseAll();
            transactionObject.ResourceHolder.Clear();
        }
    }

    /// <summary>
    /// A rabbit transaction object, representing a RabbitResourceHolder. Used as transaction object by RabbitTransactionManager.
    /// </summary>
    internal class RabbitTransactionObject : ISmartTransactionObject
    {
        /// <summary>
        /// The resource holder.
        /// </summary>
        private RabbitResourceHolder resourceHolder;

        /// <summary>
        /// Gets or sets ResourceHolder.
        /// </summary>
        public RabbitResourceHolder ResourceHolder { get { return this.resourceHolder; } set { this.resourceHolder = value; } }

        /// <summary>
        /// Gets a value indicating whether RollbackOnly.
        /// </summary>
        public bool RollbackOnly { get { return this.resourceHolder.RollbackOnly; } }

        /// <summary>
        /// Flush the object.
        /// </summary>
        public void Flush()
        {
            // no-op
        }
    }
}
