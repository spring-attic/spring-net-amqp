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
using Common.Logging;
using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class ConnectionFactoryUtils
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ConnectionFactoryUtils));

        public static void ReleaseConnection(IConnection connection, IConnectionFactory connectionFactory)
        {
            if (connection == null)
            {
                return;
            }
            try
            {
                connection.Close();
            } catch (Exception ex)
            {
                logger.Debug("Could not close RabbitMQ Connection", ex);
            }
        }

        public static bool IsChannelTransactional(IModel channel, IConnectionFactory connectionFactory )
        {
            //TODO implement
            return false;
        }

        public static IModel GetTransactionalChannel(
            IConnectionFactory cf, IConnection existingCon, bool synchedLocalTransactionAllowed)
        {
            //TODO implement
            return null;
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