// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourceFactory.cs" company="The original author or authors.">
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
using RabbitMQ.Client;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// Callback interface implementation.
    /// </summary>
    /// <author>Joe Fitzgerald</author>
    public class ResourceFactory : IResourceFactory
    {
        /// <summary>
        /// The connection factory.
        /// </summary>
        private readonly IConnectionFactory connectionFactory;

        /// <summary>
        /// The flag indicating whether a synched local transaction is allowed.
        /// </summary>
        private readonly bool synchedLocalTransactionAllowed;

        /// <summary>Initializes a new instance of the <see cref="ResourceFactory"/> class.</summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="synchedLocalTransactionAllowed">The synched local transaction allowed.</param>
        public ResourceFactory(IConnectionFactory connectionFactory, bool synchedLocalTransactionAllowed)
        {
            this.connectionFactory = connectionFactory;
            this.synchedLocalTransactionAllowed = synchedLocalTransactionAllowed;
        }

        /// <summary>
        /// Gets a value indicating whether IsSynchedLocalTransactionAllowed.
        /// </summary>
        public bool IsSynchedLocalTransactionAllowed { get { return this.synchedLocalTransactionAllowed; } }

        /// <summary>Gets a channel.</summary>
        /// <param name="holder">The holder.</param>
        /// <returns>The channel.</returns>
        public IModel GetChannel(RabbitResourceHolder holder) { return holder.Channel; }

        /// <summary>Gets a connection.</summary>
        /// <param name="holder">The holder.</param>
        /// <returns>The connection.</returns>
        public IConnection GetConnection(RabbitResourceHolder holder) { return holder.Connection; }

        /// <summary>
        /// Creates a connection.
        /// </summary>
        /// <returns>
        /// The connection.
        /// </returns>
        public IConnection CreateConnection() { return this.connectionFactory.CreateConnection(); }

        /// <summary>Creates a channel.</summary>
        /// <param name="connection">The connection.</param>
        /// <returns>The channel.</returns>
        public IModel CreateChannel(IConnection connection) { return connection.CreateChannel(this.IsSynchedLocalTransactionAllowed); }
    }
}
