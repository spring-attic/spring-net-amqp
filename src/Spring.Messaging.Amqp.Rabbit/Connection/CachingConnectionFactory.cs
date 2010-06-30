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
using System.Collections;
using System.Collections.Generic;
using Common.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Objects.Factory;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class CachingConnectionFactory : IConnectionFactory,  IInitializingObject, IDisposable
    {

        #region Logging Definition

        private static readonly ILog LOG = LogManager.GetLogger(typeof(CachingConnectionFactory));

        #endregion

        //TODO need to sync on ConnectionFactory properties between Java/.NET 
        //Note .NET is multi-protocol client (IProtocol parameters)

        private string address = "localhost:" + RabbitUtils.DEFAULT_PORT;

        private volatile int portNumber = RabbitUtils.DEFAULT_PORT;

        private int channelCacheSize = 1;

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

        private LinkedList<IModel> cachedChannels = new LinkedList<IModel>();

        private volatile bool active = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public CachingConnectionFactory()
        {
            rabbitConnectionFactory = new ConnectionFactory();
        }

        public CachingConnectionFactory(ConnectionParameters connectionParameters)
            : this(connectionParameters, "localhost:" + RabbitUtils.DEFAULT_PORT)
        {
            
        }

        public CachingConnectionFactory(ConnectionParameters connectionParameters, string address) : this (new ConnectionFactory(), address )
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
        public CachingConnectionFactory(ConnectionFactory connectionFactory, string address)
        {
            AssertUtils.ArgumentNotNull(connectionFactory, "connectionFactory", "Target ConnectionFactory must not be null");
            AssertUtils.ArgumentHasText(address, "address", "Address must not be null or empty.");
            this.rabbitConnectionFactory = connectionFactory;
            this.address = address;
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

        public bool Active
        {
            get { return active; }
            set { active = value; }
        }

        public int ChannelCacheSize
        {
            get { return channelCacheSize; }
            set
            {
                AssertUtils.IsTrue(value >= 1, "Channel cache size must be 1 or higher");
                channelCacheSize = value;
            }
        }

        public ConnectionFactory RabbitConnectionFactory
        {
            get { return rabbitConnectionFactory; }
        }

        #region Implementation of IInitializingObject

        public void AfterPropertiesSet()
        {
            AssertUtils.ArgumentNotNull(RabbitConnectionFactory, "RabbitMQ.Client.ConnectionFactory is required");            
        }

        #endregion

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

        public void ResetConnection()
        {
               //TODO 
        }

        #endregion

        public IModel GetChannel(IConnection connection)
        {
            LinkedList<IModel> channelList = this.cachedChannels;
            IModel channel = null;
            lock (channelList)
            {
                if (channelList.Count > 0)
                {
                    channel = channelList.First.Value;
                    channelList.RemoveFirst();
                }
            }
            if (channel != null)
            {
                if (LOG.IsDebugEnabled)
                {
                    LOG.Debug("Found cached Rabbit Channel");
                }
            }
            else
            {
                IModel targetModel = CreateModel(connection);
                if (LOG.IsDebugEnabled)
                {
                    LOG.Debug("Creating cached Rabbit Channel");
                }
                channel = GetCachedModelWrapper(targetModel, channelList);
            }
            return channel;
        }

        /// <summary>
        /// Wraps the given Model so that it delegates every method call to the target model but
        /// adapts close calls. This is useful for allowing application code to
        /// handle a special framework Model just like an ordinary Model.
        /// </summary>
        /// <param name="targetModel">The original Model to wrap.</param>
        /// <param name="modelList">The List of cached Model that the given Model belongs to.</param>
        /// <returns>The wrapped Session</returns>
        protected virtual IModel GetCachedModelWrapper(IModel targetModel, LinkedList<IModel> modelList)
        {
            return new CachedModel(targetModel, modelList, this);
        }

        protected virtual IModel CreateModel(IConnection conn)
        {
            return conn.CreateModel();
        }

        /// <summary>
        /// Wraps the given Session so that it delegates every method call to the target session but
        /// adapts close calls. This is useful for allowing application code to
        /// handle a special framework Session just like an ordinary Session.
        /// </summary>
        /// <param name="targetSession">The original Session to wrap.</param>
        /// <param name="sessionList">The List of cached Sessions that the given Session belongs to.</param>
        /// <returns>The wrapped Session</returns>
        protected virtual IModel GetCachedSessionWrapper(IModel targetModel, LinkedList<IModel> channelList)
        {
            return new CachedModel(targetModel, channelList, this);
        }
    }

   
}