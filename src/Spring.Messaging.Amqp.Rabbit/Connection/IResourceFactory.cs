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

using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A rabbit resource factory interface.
    /// </summary>
    /// <author>Mark Pollack</author>
    public interface IResourceFactory
    {
        /// <summary>
        /// Get the channel.
        /// </summary>
        /// <param name="holder">
        /// The holder.
        /// </param>
        /// <returns>
        /// The channel.
        /// </returns>
        IModel GetChannel(RabbitResourceHolder holder);

        /// <summary>
        /// Get the connection.
        /// </summary>
        /// <param name="rabbitResourceHolder">
        /// The rabbit resource holder.
        /// </param>
        /// <returns>
        /// The connection.
        /// </returns>
        IConnection GetConnection(RabbitResourceHolder rabbitResourceHolder);

        /// <summary>
        /// Create the connection.
        /// </summary>
        /// <returns>
        /// The connection.
        /// </returns>
        IConnection CreateConnection();

        /// <summary>
        /// Create the channel.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        /// <returns>
        /// The channel.
        /// </returns>
        IModel CreateChannel(IConnection connection);

        /// <summary>
        /// Gets a value indicating whether IsSynchedLocalTransactionAllowed.
        /// </summary>
        bool IsSynchedLocalTransactionAllowed { get; }
    }

}