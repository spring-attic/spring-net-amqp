// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SharedConnectionProxy.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using Common.Logging;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Rabbit.Connection;
using IConnection = Spring.Messaging.Amqp.Rabbit.Connection.IConnection;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Connection
{
    /// <summary>
    /// A shared connection proxy.
    /// </summary>
    public class SharedConnectionProxy : IConnection, IConnectionProxy
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The target connection.
        /// </summary>
        private volatile IConnection target;

        /// <summary>
        /// The outer single connection factory.
        /// </summary>
        private readonly SingleConnectionFactory outer;

        /// <summary>Initializes a new instance of the <see cref="SharedConnectionProxy"/> class.</summary>
        /// <param name="target">The target.</param>
        /// <param name="outer">The outer.</param>
        public SharedConnectionProxy(IConnection target, SingleConnectionFactory outer)
        {
            this.target = target;
            this.outer = outer;
        }

        /// <summary>Create a new channel, using an internally allocated channel number.</summary>
        /// <param name="transactional">Transactional true if the channel should support transactions.</param>
        /// <returns>A new channel descriptor, or null if none is available.</returns>
        public IModel CreateChannel(bool transactional)
        {
            if (!this.IsOpen())
            {
                lock (this)
                {
                    if (!this.IsOpen())
                    {
                        Logger.Debug("Detected closed connection. Opening a new one before creating Channel.");
                        this.target = this.outer.CreateBareConnection();
                        this.outer.ConnectionListener.OnCreate(this.target);
                    }
                }
            }

            var channel = this.target.CreateChannel(transactional);
            this.outer.ChannelListener.OnCreate(channel, transactional);
            return channel;
        }

        /// <summary>
        /// Close this connection and all its channels with the {@link com.rabbitmq.client.AMQP#REPLY_SUCCESS} close code and message 'OK'.
        /// Waits for all the close operations to complete.
        /// </summary>
        public void Close() { }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            if (this.target != null)
            {
                this.outer.ConnectionListener.OnClose(this.target);
                RabbitUtils.CloseConnection(this.target);
            }

            this.target = null;
        }

        /// <summary>
        /// Flag to indicate the status of the connection.
        /// </summary>
        /// <returns>True if the connection is open</returns>
        public bool IsOpen() { return this.target != null && this.target.IsOpen(); }

        /// <summary>
        /// Return the target Channel of this proxy. This will typically be the native provider IConnection
        /// </summary>
        /// <returns>The underlying connection (never null).</returns>
        public IConnection GetTargetConnection() { return this.target; }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() { return 31 + ((this.target == null) ? 0 : this.target.GetHashCode()); }

        /// <summary>Determines whether the specified <see cref="System.Object"/> is equal to this instance.</summary>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }

            var other = (SharedConnectionProxy)obj;
            if (this.target == null)
            {
                if (other.target != null)
                {
                    return false;
                }
            }
            else if (!this.target.Equals(other.target))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString() { return "Shared Rabbit Connection: " + this.target; }
    }
}
