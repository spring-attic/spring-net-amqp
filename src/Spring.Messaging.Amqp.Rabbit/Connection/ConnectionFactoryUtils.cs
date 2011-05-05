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

        public static IModel GetTransactionalChannel(
            IConnectionFactory cf, IConnection existingCon, bool synchedLocalTransactionAllowed)
        {
            //TODO implement
            return null;
        }

        /**
 * Obtain a RabbitMQ Channel that is synchronized with the current transaction, if any.
 * @param connectionFactory the RabbitMQ ConnectionFactory to bind for (used as TransactionSynchronizationManager
 * key)
 * @param resourceFactory the ResourceFactory to use for extracting or creating RabbitMQ resources
 * @return the transactional Channel, or <code>null</code> if none found
 */
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

        public static IModel DoGetTransactionalChannel(IConnectionFactory connectionFactory, IResourceFactory resourceFactory)
        {
            //TODO implement
            return null;
        }

        /// <summary>
        /// Return the innermost target IModel of the given IModel. If the given
        /// IModel is a decorated model, it will be unwrapped until a non-decorated
        /// IModel is found. Otherwise, the passed-in IModel will be returned as-is.
        /// </summary>
        /// <param name="model">The model to unwrap</param>
        /// <returns>The innermost target IModel, or the passed-in one if no decorator</returns>
        public static IModel GetTargetModel(IModel model)
        {
            IModel modelToUse = model;
            while (modelToUse is IDecoratorModel)
            {
                modelToUse = ((IDecoratorModel)modelToUse).TargetModel;
            }
            return modelToUse;
        }

        /// <summary>
        /// Return the innermost target IConnection of the given IConnection. If the given
        /// IConnection is a decorated connection, it will be unwrapped until a non-decorated
        /// IConnection is found. Otherwise, the passed-in IConnection will be returned as-is.
        /// </summary>
        /// <param name="model">The connection to unwrap</param>
        /// <returns>The innermost target IConnection, or the passed-in one if no decorator</returns>
        public static IConnection GetTargetConnection(IConnection connection)
        {
            IConnection connectionToUse = connection;
            while (connectionToUse is IDecoratorConnection)
            {
                connectionToUse = ((IDecoratorConnection)connectionToUse).TargetConnection;
            }
            return connectionToUse;
        }
    }
}