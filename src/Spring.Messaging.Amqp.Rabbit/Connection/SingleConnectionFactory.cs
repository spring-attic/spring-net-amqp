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
using System.Collections.Generic;
using System.Net;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A <see cref="IConnectionFactory"/> implementation that returns the same Connection from all
    /// <see cref="IConnectionFactory.CreateConnection"/> calls, and ignores call to 
    /// <see cref="IConnection.Close()"/>
    /// </summary>
    /// <author>Mark Pollack</author>
    public class SingleConnectionFactory : IConnectionFactory
    {
        #region Logging Definition

        /// <summary>
        /// The logger.
        /// </summary>
        private static readonly ILog logger = LogManager.GetLogger(typeof(SingleConnectionFactory));

        #endregion

        /* TODO need to sync on ConnectionFactory properties between Java/.NET 
           Note .NET is multi-protocol client (IProtocol parameters) */

        /// <summary>
        /// The connection factory.
        /// </summary>
        private ConnectionFactory rabbitConnectionFactory;

        /// <summary>
        /// Proxy connection.
        /// </summary>
        private SharedConnectionProxy connection;

        /// <summary>
        /// Synchronization monitor for the shared Connection
        /// </summary>
        private readonly object connectionMonitor = new object();

        /// <summary>
        /// The listener.
        /// </summary>
        private readonly CompositeConnectionListener listener = new CompositeConnectionListener();

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class 
        /// initializing the hostname to be the value returned from Dns.GetHostName() or "localhost"
        /// if Dns.GetHostName() throws an exception.
        /// </summary>
        public SingleConnectionFactory() : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.
        /// </summary>
        /// <param name="port">
        /// The port.
        /// </param>
        public SingleConnectionFactory(int port) : this(string.Empty, port)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.
        /// </summary>
        /// <param name="hostname">
        /// The hostname.
        /// </param>
        public SingleConnectionFactory(string hostname) : this(hostname, RabbitMQ.Client.Protocols.DefaultProtocol.DefaultPort)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.
        /// </summary>
        /// <param name="hostname">
        /// The hostname.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        public SingleConnectionFactory(string hostname, int port)
        {
            if (!StringUtils.HasText(hostname))
            {
                hostname = GetDefaultHostName();
            }

            this.rabbitConnectionFactory = new RabbitMQ.Client.ConnectionFactory { HostName = hostname, Port = port };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.
        /// </summary>
        /// <param name="rabbitConnectionFactory">
        /// The rabbit connection factory.
        /// </param>
        public SingleConnectionFactory(RabbitMQ.Client.ConnectionFactory rabbitConnectionFactory)
        {
            AssertUtils.ArgumentNotNull(rabbitConnectionFactory, "Target ConnectionFactory must not be null");
            this.rabbitConnectionFactory = rabbitConnectionFactory;
        }

        #region Implementation of IConnectionFactory

        /// <summary>
        /// Sets UserName.
        /// </summary>
        public string UserName
        {
            set { this.rabbitConnectionFactory.UserName = value; }
        }

        /// <summary>
        /// Sets Password.
        /// </summary>
        public string Password
        {
            set { this.rabbitConnectionFactory.Password = value; }
        }

        /// <summary>
        /// Gets or sets Host.
        /// </summary>
        public string Host
        {
            get { return this.rabbitConnectionFactory.HostName; }
            set { this.rabbitConnectionFactory.HostName = value; }
        }

        /// <summary>
        /// Gets or sets VirtualHost.
        /// </summary>
        public string VirtualHost
        {
            get { return this.rabbitConnectionFactory.VirtualHost; }
            set { this.rabbitConnectionFactory.VirtualHost = value; }
        }

        /// <summary>
        /// Gets or sets Port.
        /// </summary>
        public int Port
        {
            get { return this.rabbitConnectionFactory.Port; }
            set { this.rabbitConnectionFactory.Port = value; }
        }

        /// <summary>
        /// Sets the connection listeners.
        /// </summary>
        /// <value>
        /// The connection listeners.
        /// </value>
        public IList<IConnectionListener> ConnectionListeners
        {
            set { this.listener.Delegates = value; }
        }    

        /// <summary>
        /// Add a connection listener.
        /// </summary>
        /// <param name="connectionListener">
        /// The listener.
        /// </param>
        public void AddConnectionListener(IConnectionListener connectionListener)
        {
            this.listener.AddDelegate(connectionListener);

            // If the connection is already alive we assume that the new listener wants to be notified
            if (this.connection != null)
            {
                listener.OnCreate(this.connection);
            }
        }

        /// <summary>
        /// Create a connection.
        /// </summary>
        /// <returns>
        /// The connection.
        /// </returns>
        public IConnection CreateConnection()
        {
            lock (this.connectionMonitor)
            {
                if (this.connection == null)
                {
                    var target = this.DoCreateConnection();
                    this.connection = new SharedConnectionProxy(target, this.listener, this);

                    // invoke the listener *after* this.connection is assigned
                    this.listener.OnCreate(target);
                }
            }

            return this.connection;
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
        public virtual void Dispose()
        {
            lock (this.connectionMonitor)
            {
                if (this.connection != null)
                {
                    #region Logging
                    if (logger.IsDebugEnabled)
                    {
                        logger.Debug("Closing shared RabbitMQ Connection: " + this.connection);
                    } 
                    #endregion

                    try
                    {
                        this.connection.Close();
                        this.connection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Could not close shared RabbitMQ connection.", ex);
                    }
                }

                this.connection = null;
            }
            Reset();
        }

        #endregion


        /// <summary>
        /// Default implementation does nothing.  Called on <see cref="Dispose"/>.
        /// </summary>
        public virtual void Reset()
        {
            
        }

        /// <summary>
        /// Create a Connection. This implementation just delegates to the underlying Rabbit ConnectionFactory. Subclasses
        /// typically will decorate the result to provide additional features.
        /// </summary>
        /// <returns>
        /// The connection.
        /// </returns>
        protected virtual IConnection DoCreateConnection()
        {
            IConnection con = this.CreateBareConnection();
            return con;
        }

        /// <summary>
        /// Create a bare connection.
        /// </summary>
        /// <returns>
        /// The connection.
        /// </returns>
        /// <exception cref="SystemException">
        /// </exception>
        internal IConnection CreateBareConnection()
        {   
            try
            {
                return new SimpleConnection(this.rabbitConnectionFactory.CreateConnection());
            }
            catch (Exception e)
            {
                throw RabbitUtils.ConvertRabbitAccessException(e);
            }
        }

        /// <summary>
        /// Get the default host name.
        /// </summary>
        /// <returns>
        /// The host name.
        /// </returns>
        private string GetDefaultHostName()
        {
            // TODO: Should we be using RabbitMQ.Client.ConnectionFactory.DefaultVHost instead?
            string temp;
            try
            {
                temp = Dns.GetHostName();
                logger.Debug("Using hostname [" + temp + "] for hostname.");
            }
            catch (Exception e)
            {
                logger.Warn("Could not get host name, using 'localhost' as default value", e);
                temp = "localhost";
            }

            return temp;
        }

        /// <summary>
        /// Get the string representation of the object.
        /// </summary>
        /// <returns>
        /// The string representation of the object.
        /// </returns>
        public override string ToString()
        {
            return "SingleConnectionFactory [host=" + this.rabbitConnectionFactory.HostName + ", port=" + this.rabbitConnectionFactory.Port + "]";
        }
    }

    /// <summary>
    /// Wrap a raw Connection with a proxy that delegates every method call to it but suppresses close calls. This is
    /// useful for allowing application code to handle a special framework Connection just like an ordinary Connection
    /// from a Rabbit ConnectionFactory.
    /// </summary>
    internal class SharedConnectionProxy : IConnection, IConnectionProxy, IDisposable
    {
        #region Logging Definition

        private static readonly ILog logger = LogManager.GetLogger(typeof(SharedConnectionProxy));

        #endregion

        /// <summary>
        /// The target.
        /// </summary>
        private volatile IConnection target;

        /// <summary>
        /// The listener.
        /// </summary>
        private volatile CompositeConnectionListener listener;

        /// <summary>
        /// The outer SingleConnectionFactory.
        /// </summary>
        private volatile SingleConnectionFactory outer;

        /// <summary>
        /// An object used for locking.
        /// </summary>
        private readonly object syncLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedConnectionProxy"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="listener">
        /// The listener.
        /// </param>
        /// <param name="outer">
        /// The outer.
        /// </param>
        public SharedConnectionProxy(IConnection target, CompositeConnectionListener listener, SingleConnectionFactory outer)
        {
            this.target = target;
            this.listener = listener;
            this.outer = outer;
        }

        /// <summary>
        /// Create a channel, given a flag indicating whether it should be transactional or not.
        /// </summary>
        /// <param name="transactional">
        /// The transactional.
        /// </param>
        /// <returns>
        /// The channel.
        /// </returns>
        public IModel CreateChannel(bool transactional)
        {
            if (this.target == null || !this.target.IsOpen())
            {
                lock (this.syncLock)
                {
                    if (this.target == null || !this.target.IsOpen())
                    {
                        logger.Debug("Detected closed connection. Opening a new one before creating Channel.");
                        this.target = this.outer.CreateBareConnection();
                        this.listener.OnCreate(this.target);
                    }
                }
            }

            var channel = this.target.CreateChannel(transactional);
            return channel;
        }

        /// <summary>
        /// Suppresses call to close.
        /// </summary>
        public void Close()
        {
        }

        /// <summary>
        /// Destroy the connection.
        /// </summary>
        public void Dispose()
        {
            if (this.target != null)
            {
                this.listener.OnClose(this.target);
                RabbitUtils.CloseConnection(this.target);
            }

            this.target = null;
        }

        /// <summary>
        /// A flag indicating whether the connection is open.
        /// </summary>
        /// <returns>
        /// True if the connection is open, else false.
        /// </returns>
        public bool IsOpen()
        {
            return this.target != null && this.target.IsOpen();
        }

        /// <summary>
        /// Get the target connection.
        /// </summary>
        /// <returns>
        /// The target connection.
        /// </returns>
        public IConnection GetTargetConnection()
        {
            return this.target;
        }

        /// <summary>
        /// Get the hash code for the object.
        /// </summary>
        /// <returns>
        /// The hash code.
        /// </returns>
        public new int GetHashCode()
        {
            return 31 + ((this.target == null) ? 0 : this.target.GetHashCode());
        }

        /// <summary>
        /// Determine if the supplied object is equal to this object.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <returns>
        /// True if the same, else false.
        /// </returns>
        public new bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (!(obj is SharedConnectionProxy))
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
        /// Convert object to string representation.
        /// </summary>
        /// <returns>
        /// String representation of the object.
        /// </returns>
        public override string ToString()
        {
            return "Shared Rabbit Connection: " + this.target;
        }
    }
} 