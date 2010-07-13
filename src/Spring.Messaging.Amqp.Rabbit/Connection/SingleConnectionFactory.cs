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
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Objects.Factory;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A <see cref="IConnectionFactory"/> implementation that returns the same Connection from all
    /// <see cref="IConnectionFactory.CreateConnection"/> calls, and ignores call to 
    /// <see cref="IConnection.Close()"/>
    /// </summary>
    /// <author>Mark Pollack</author>
    public class SingleConnectionFactory : IConnectionFactory, IInitializingObject
    {
        #region Logging Definition

        private static readonly ILog LOG = LogManager.GetLogger(typeof(SingleConnectionFactory));

        #endregion


        //TODO need to sync on ConnectionFactory properties between Java/.NET 
        //Note .NET is multi-protocol client (IProtocol parameters)

        private string address = "localhost:" + RabbitUtils.DEFAULT_PORT;

        private volatile int portNumber = RabbitUtils.DEFAULT_PORT;
        
        private ConnectionFactory rabbitConnectionFactory;

        /// <summary>
        /// Raw Rabbit Connection
        /// </summary>
        private IConnection targetConnection;

        /// <summary>
        /// Proxy Connection
        /// </summary>
        private IConnection connection;

        /// <summary>
        /// Synchronization monitor for the shared Connection
        /// </summary>
        private object connectionMonitor = new object();

        public SingleConnectionFactory()
        {
            rabbitConnectionFactory = new ConnectionFactory();
        }

        public SingleConnectionFactory(ConnectionParameters connectionParameters)
            : this(connectionParameters, "localhost:" + RabbitUtils.DEFAULT_PORT)
        {
            
        }

        public SingleConnectionFactory(ConnectionParameters connectionParameters, string address)
            : this(new ConnectionFactory(), address)
        {
            rabbitConnectionFactory.Parameters.Password = connectionParameters.Password;
            rabbitConnectionFactory.Parameters.RequestedChannelMax = connectionParameters.RequestedChannelMax;
            rabbitConnectionFactory.Parameters.RequestedFrameMax = connectionParameters.RequestedFrameMax;
            rabbitConnectionFactory.Parameters.RequestedHeartbeat = connectionParameters.RequestedHeartbeat;
            rabbitConnectionFactory.Parameters.Ssl = connectionParameters.Ssl;
            rabbitConnectionFactory.Parameters.UserName = connectionParameters.UserName;
            rabbitConnectionFactory.Parameters.VirtualHost = connectionParameters.VirtualHost;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="address">The address.</param>
        public SingleConnectionFactory(ConnectionFactory connectionFactory, string address)
        {
            AssertUtils.ArgumentNotNull(connectionFactory, "connectionFactory", "Target ConnectionFactory must not be null");
            AssertUtils.ArgumentHasText(address, "address", "Address must not be null or empty.");
            this.rabbitConnectionFactory = connectionFactory;
            this.address = address;
        }

        public virtual IModel GetChannel(IConnection connection)
        {
            return this.CreateChannel(connection);
        }

        protected virtual IModel CreateChannel(IConnection connection)
        {
            return connection.CreateModel();
        }

        #region Implementation of IConnectionFactory

        public IConnection CreateConnection()
        {
            //TODO Add Protocols.FromEnvironment

            //return rabbitConnectionFactory.CreateConnection(address);           
            lock (connectionMonitor)
            {
                if (connection == null)
                {
                    InitConnection();
                }
                return connection;
            }

        }

        #endregion

        /// <summary>
        /// Initialize the underlying shared Connection. Closes and reinitializes the Connection if an underlying
        /// Connection is present already.
        /// </summary>
        public void InitConnection()
        {
            if (RabbitConnectionFactory == null)
            {
                throw new ArgumentException(
                    "'RabbitConnectionFactory' is required for lazily initializing a Connection");
            }
            lock (connectionMonitor)
            {
                if (this.targetConnection != null)
                {
                    CloseConnection(this.targetConnection);
                }
                this.targetConnection = DoCreateConnection();
                PrepareConnection(this.targetConnection);
                if (LOG.IsDebugEnabled)
                {
                    LOG.Info("Established shared RabbitMQ Connection: " + this.targetConnection);
                }
                this.connection = GetSharedConnection(targetConnection);
            }
        }

        public ConnectionFactory RabbitConnectionFactory
        {
            get { return rabbitConnectionFactory; }
        }


        #region Implementation of IDisposable

        /// <summary>
        /// Close the underlying shared connection.
        /// </summary>
        /// <remarks>
        /// The provider of this ConnectionFactory needs to care for proper shutdown.
        /// As this bean implements IDisposable, the application context will
        /// automatically invoke this on destruction of its cached singletons.
        /// </remarks>
        public void Dispose()
        {
            ResetConnection();
        }

        public virtual void ResetConnection()
        {
            lock (this.connectionMonitor)
            {
                if (this.targetConnection != null)
                {
                    CloseConnection(this.targetConnection);
                }
                this.targetConnection = null;
                this.connection = null;
            }
        }

        #endregion

        /// <summary>
        /// Closes the given connection.
        /// </summary>
        /// <param name="con">The connection.</param>
        protected virtual void CloseConnection(IConnection con)
        {
            if (LOG.IsDebugEnabled)
            {
                LOG.Debug("Closing shared RabbitMQ Connection: " + this.targetConnection);
            }
            try
            {
                con.Close();
            }
            catch (Exception ex)
            {
                LOG.Warn("Could not close shared RabbitMQ connection.", ex);
            }
        }


        protected virtual IConnection DoCreateConnection()
        {
            return rabbitConnectionFactory.CreateConnection(address);
        }


        protected virtual void PrepareConnection(IConnection connection)
        {
            // Potentially configure shutdown exceptions, investigate reconnection exceptions.
        }


        /// <summary>
        /// Wrap the given Connection with a proxy that delegates every method call to it
        /// but suppresses close calls. This is useful for allowing application code to
        /// handle a special framework Connection just like an ordinary Connection from a
        /// ConnectionFactory.
        /// </summary>
        /// <param name="target">The original connection to wrap.</param>
        /// <returns>the wrapped connection</returns>
        protected virtual IConnection GetSharedConnection(IConnection target)
        {
            lock (connectionMonitor)
            {
                return new CloseSuppressingConnection(this, target);
            }
        }

        #region Implementation of IInitializingObject

        public void AfterPropertiesSet()
        {
            AssertUtils.ArgumentNotNull(RabbitConnectionFactory, "RabbitMQ.Client.ConnectionFactory is required");
        }

        #endregion

        public override string ToString()
        {
            return "SingleConnectionFactory [hostName=" + address + ", portNumber=" + portNumber + "]";
        }
    }

} 