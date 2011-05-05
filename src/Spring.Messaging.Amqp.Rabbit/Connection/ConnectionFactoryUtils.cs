#region License

/*
 * Copyright 2002-2010 the original author or authors.
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
using System.IO;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Transaction.Support;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// Utility methods for connection factory.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class ConnectionFactoryUtils
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private static readonly ILog logger = LogManager.GetLogger(typeof(ConnectionFactoryUtils));

        /// <summary>
        /// Release a connection.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        public static void ReleaseConnection(IConnection connection)
        {
            if (connection == null)
            {
                return;
            }
            try
            {
                connection.Close();
            }
            catch (Exception ex)
            {
                logger.Debug("Could not close RabbitMQ Connection", ex);
            }
        }

        /// <summary>
        /// Determine whether the given RabbitMQ Channel is transactional, that is, bound to the current thread by Spring's transaction facilities.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="connectionFactory">
        /// The connection factory.
        /// </param>
        /// <returns>
        /// Whether the Channel is transactional
        /// </returns>
        public static bool IsChannelTransactional(IModel channel, IConnectionFactory connectionFactory)
        {
            if (channel == null || connectionFactory == null)
            {
                return false;
            }

            var resourceHolder = (RabbitResourceHolder)TransactionSynchronizationManager.GetResource(connectionFactory);
            return resourceHolder != null && resourceHolder.ContainsChannel(channel);
        }

        /// <summary>
        /// Obtain a RabbitMQ Channel that is synchronized with the current transaction, if any.
        /// </summary>
        /// <param name="connectionFactory">
        /// The connection factory.
        /// </param>
        /// <param name="synchedLocalTransactionAllowed">
        /// The synched local transaction allowed.
        /// </param>
        /// <returns>
        /// The transactional Channel, or null if none found.
        /// </returns>
        public static RabbitResourceHolder GetTransactionalResourceHolder(IConnectionFactory connectionFactory, bool synchedLocalTransactionAllowed)
        {
            var holder = DoGetTransactionalResourceHolder(connectionFactory, new ResourceFactory(connectionFactory, synchedLocalTransactionAllowed));
            if (synchedLocalTransactionAllowed)
            {
                // holder.declareTransactional();
            }

            return holder;
        }

        /// <summary>
        /// Obtain a RabbitMQ Channel that is synchronized with the current transaction, if any.
        /// </summary>
        /// <param name="connectionFactory">
        /// The connection factory.
        /// </param>
        /// <param name="resourceFactory">
        /// The resource factory.
        /// </param>
        /// <returns>
        /// The transactional Channel, or null if none found.
        /// </returns>
        /// <exception cref="AmqpException">
        /// </exception>
        private static RabbitResourceHolder DoGetTransactionalResourceHolder(IConnectionFactory connectionFactory, IResourceFactory resourceFactory)
        {
            AssertUtils.ArgumentNotNull(connectionFactory, "ConnectionFactory must not be null");
            AssertUtils.ArgumentNotNull(resourceFactory, "ResourceFactory must not be null");

            var resourceHolder = (RabbitResourceHolder)TransactionSynchronizationManager.GetResource(connectionFactory);
            if (resourceHolder != null)
            {
                var tempchannel = resourceFactory.GetChannel(resourceHolder);
                if (tempchannel != null)
                {
                    return resourceHolder;
                }
            }

            var resourceHolderToUse = resourceHolder;
            if (resourceHolderToUse == null)
            {
                resourceHolderToUse = new RabbitResourceHolder();
            }

            var connection = resourceFactory.GetConnection(resourceHolderToUse);
            IModel channel = null;
            try
            {
                bool isExistingCon = connection != null;
                if (!isExistingCon)
                {
                    connection = resourceFactory.CreateConnection();
                    resourceHolderToUse.AddConnection(connection);
                }
                channel = resourceFactory.CreateChannel(connection);
                resourceHolderToUse.AddChannel(channel, connection);

                if (resourceHolderToUse != resourceHolder)
                {
                    BindResourceToTransaction(resourceHolderToUse, connectionFactory, resourceFactory.IsSynchedLocalTransactionAllowed);
                }

                return resourceHolderToUse;

            }
            catch (Exception ex)
            {
                RabbitUtils.CloseChannel(channel);
                RabbitUtils.CloseConnection(connection);
                throw new AmqpException(ex);
            }
        }

        /// <summary>
        /// Release the resources.
        /// </summary>
        /// <param name="resourceHolder">
        /// The resource holder.
        /// </param>
        public static void ReleaseResources(RabbitResourceHolder resourceHolder)
        {
            if (resourceHolder == null || resourceHolder.SynchronizedWithTransaction)
            {
                return;
            }
            RabbitUtils.CloseChannel(resourceHolder.Channel);
            ReleaseConnection(resourceHolder.Connection);
        }

        /// <summary>
        /// Bind a resource to a transaction.
        /// </summary>
        /// <param name="resourceHolder">
        /// The resource holder.
        /// </param>
        /// <param name="connectionFactory">
        /// The connection factory.
        /// </param>
        /// <param name="synched">
        /// The synched.
        /// </param>
        public static void BindResourceToTransaction(RabbitResourceHolder resourceHolder, IConnectionFactory connectionFactory, bool synched)
        {
            if (TransactionSynchronizationManager.HasResource(connectionFactory) || !TransactionSynchronizationManager.ActualTransactionActive || !synched)
            {
                return;
            }

            TransactionSynchronizationManager.BindResource(connectionFactory, resourceHolder);
            resourceHolder.SynchronizedWithTransaction = true;
            if (TransactionSynchronizationManager.SynchronizationActive)
            {
                TransactionSynchronizationManager.RegisterSynchronization(new RabbitResourceSynchronization(resourceHolder, connectionFactory, synched));
            }
        }


        /// <summary>
        /// Register a delivery tag.
        /// </summary>
        /// <param name="connectionFactory">
        /// The connection factory.
        /// </param>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="tag">
        /// The tag.
        /// </param>
        public static void RegisterDeliveryTag(IConnectionFactory connectionFactory, IModel channel, long tag)
        {

            AssertUtils.ArgumentNotNull(connectionFactory, "ConnectionFactory must not be null");

            var resourceHolder = (RabbitResourceHolder)TransactionSynchronizationManager.GetResource(connectionFactory);
            if (resourceHolder != null)
            {
                resourceHolder.AddDeliveryTag(channel, tag);
            }
        }
    }
}