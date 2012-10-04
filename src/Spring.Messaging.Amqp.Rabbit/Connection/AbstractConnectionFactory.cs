// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AbstractConnectionFactory.cs" company="The original author or authors.">
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
using System.Collections.Generic;
using System.Net;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A <see cref="IConnectionFactory"/> implementation that returns the same Connection from all
    /// <see cref="IConnectionFactory.CreateConnection"/> calls, and ignores call to 
    /// <see cref="IConnection.Close()"/>
    /// </summary>
    /// <author>Mark Pollack</author>
    public abstract class AbstractConnectionFactory : IConnectionFactory, IDisposable
    {
        #region Logging Definition

        /// <summary>
        /// The Logger.
        /// </summary>
        protected readonly ILog Logger = LogManager.GetLogger(typeof(AbstractConnectionFactory));
        #endregion

        /// <summary>
        /// The connection factory.
        /// </summary>
        private readonly ConnectionFactory rabbitConnectionFactory;

        /// <summary>
        /// The connection listener.
        /// </summary>
        private readonly CompositeConnectionListener connectionListener = new CompositeConnectionListener();

        /// <summary>
        /// The channel listener.
        /// </summary>
        private readonly CompositeChannelListener channelListener = new CompositeChannelListener();

        // private volatile IExecutorService executorService;
        private volatile AmqpTcpEndpoint[] addresses;

        /// <summary>Initializes a new instance of the <see cref="AbstractConnectionFactory"/> class.</summary>
        /// <param name="rabbitConnectionFactory">The rabbit connection factory.</param>
        public AbstractConnectionFactory(ConnectionFactory rabbitConnectionFactory)
        {
            AssertUtils.ArgumentNotNull(rabbitConnectionFactory, "Target ConnectionFactory must not be null");
            this.rabbitConnectionFactory = rabbitConnectionFactory;
        }

        #region Implementation of IConnectionFactory

        /// <summary>
        /// Sets UserName.
        /// </summary>
        public string UserName { set { this.rabbitConnectionFactory.UserName = value; } }

        /// <summary>
        /// Sets Password.
        /// </summary>
        public string Password { set { this.rabbitConnectionFactory.Password = value; } }

        /// <summary>
        /// Gets or sets Host.
        /// </summary>
        public string Host { get { return this.rabbitConnectionFactory.HostName; } set { this.rabbitConnectionFactory.HostName = value; } }

        /// <summary>
        /// Gets or sets VirtualHost.
        /// </summary>
        public string VirtualHost { get { return this.rabbitConnectionFactory.VirtualHost; } set { this.rabbitConnectionFactory.VirtualHost = value; } }

        /// <summary>
        /// Gets or sets Port.
        /// </summary>
        public int Port { get { return this.rabbitConnectionFactory.Port; } set { this.rabbitConnectionFactory.Port = value; } }

        /// <summary>Sets the addresses.</summary>
        public string Addresses
        {
            set
            {
                var addressArray = AmqpTcpEndpoint.ParseMultiple(Protocols.DefaultProtocol, value);
                if (addressArray != null && addressArray.Length > 0)
                {
                    this.addresses = addressArray;
                }
            }
        }

        /// <summary>
        /// Gets the channel listener.
        /// </summary>
        public virtual IChannelListener ChannelListener { get { return this.channelListener; } }

        /// <summary>
        /// Gets the connection listener.
        /// </summary>
        public virtual IConnectionListener ConnectionListener { get { return this.connectionListener; } }

        /// <summary>
        /// Sets the connection listeners.
        /// </summary>
        /// <value>
        /// The connection listeners.
        /// </value>
        public virtual IList<IConnectionListener> ConnectionListeners { set { this.connectionListener.Delegates = value; } }

        /// <summary>
        /// Sets the channel listeners.
        /// </summary>
        /// <value>
        /// The channel listeners.
        /// </value>
        public virtual IList<IChannelListener> ChannelListeners { set { this.channelListener.Delegates = value; } }

        /// <summary>Add a connection listener.</summary>
        /// <param name="connectionListener">The listener.</param>
        public virtual void AddConnectionListener(IConnectionListener connectionListener) { this.connectionListener.AddDelegate(connectionListener); }

        /// <summary>Add a connection listener.</summary>
        /// <param name="channelListener">The listener.</param>
        public virtual void AddChannelListener(IChannelListener channelListener) { this.channelListener.AddDelegate(channelListener); }

        /// <summary>
        /// Create a connection.
        /// </summary>
        /// <returns>The connection.</returns>
        public abstract IConnection CreateConnection();

        /// <summary>
        /// Create a connection.
        /// </summary>
        /// <returns>The connection.</returns>
        public virtual IConnection CreateBareConnection()
        {
            try
            {
                if (this.addresses != null)
                {
                    // TODO: Waiting on RabbitMQ.Client to catch up to the Java equivalent here.
                    // return new SimpleConnection(this.rabbitConnectionFactory.CreateConnection(this.addresses));
                    return new SimpleConnection(this.rabbitConnectionFactory.CreateConnection());
                }
                else
                {
                    return new SimpleConnection(this.rabbitConnectionFactory.CreateConnection());
                }
            }
            catch (Exception ex)
            {
                throw RabbitUtils.ConvertRabbitAccessException(ex);
            }
        }

        /// <summary>
        /// Get the default host name.
        /// </summary>
        /// <returns>The host name.</returns>
        protected string GetDefaultHostName()
        {
            string temp;
            try
            {
                temp = Dns.GetHostName().ToUpper();
                this.Logger.Debug("Using hostname [" + temp + "] for hostname.");
            }
            catch (Exception e)
            {
                this.Logger.Warn("Could not get host name, using 'localhost' as default value", e);
                temp = "localhost";
            }

            return temp;
        }

        #endregion

        #region Implementation of IDisposable

        /// <summary>
        /// Close the underlying shared connection.
        /// </summary>
        public virtual void Dispose() { }
        #endregion
    }
}
