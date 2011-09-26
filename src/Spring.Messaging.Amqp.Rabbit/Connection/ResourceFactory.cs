
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFactory"/> class.
        /// </summary>
        /// <param name="connectionFactory">
        /// The connection factory.
        /// </param>
        /// <param name="synchedLocalTransactionAllowed">
        /// The synched local transaction allowed.
        /// </param>
        public ResourceFactory(IConnectionFactory connectionFactory, bool synchedLocalTransactionAllowed)
        {
            this.connectionFactory = connectionFactory;
            this.synchedLocalTransactionAllowed = synchedLocalTransactionAllowed;
        }

        /// <summary>
        /// Gets a value indicating whether IsSynchedLocalTransactionAllowed.
        /// </summary>
        public bool IsSynchedLocalTransactionAllowed
        {
            get { return this.synchedLocalTransactionAllowed; }
        }

        /// <summary>
        /// Gets a channel.
        /// </summary>
        /// <param name="holder">
        /// The holder.
        /// </param>
        /// <returns>
        /// The channel.
        /// </returns>
        public IModel GetChannel(RabbitResourceHolder holder)
        {
            return holder.Channel;
        }

        /// <summary>
        /// Gets a connection.
        /// </summary>
        /// <param name="holder">
        /// The holder.
        /// </param>
        /// <returns>
        /// The connection.
        /// </returns>
        public IConnection GetConnection(RabbitResourceHolder holder)
        {
            return holder.Connection;
        }

        /// <summary>
        /// Creates a connection.
        /// </summary>
        /// <returns>
        /// The connection.
        /// </returns>
        public IConnection CreateConnection()
        {
            return this.connectionFactory.CreateConnection();
        }

        /// <summary>
        /// Creates a channel.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        /// <returns>
        /// The channel.
        /// </returns>
        public IModel CreateChannel(IConnection connection)
        {
            return connection.CreateChannel(this.IsSynchedLocalTransactionAllowed);
        }
    }
}
