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
using AopAlliance.Intercept;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Aop.Framework;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    using System.Reflection;

    /**
     * A {@link IConnectionFactory} implementation that returns the same Connections from all {@link #createConnection()}
     * calls, and ignores calls to {@link com.rabbitmq.client.Connection#close()} and caches
     * {@link com.rabbitmq.client.Channel}.
     * 
     * <p>
     * By default, only one Channel will be cached, with further requested Channels being created and disposed on demand.
     * Consider raising the {@link #setChannelCacheSize(int) "channelCacheSize" value} in case of a high-concurrency
     * environment.
     * 
     * <p>
     * <b>NOTE: This ConnectionFactory requires explicit closing of all Channels obtained form its shared Connection.</b>
     * This is the usual recommendation for native Rabbit access code anyway. However, with this ConnectionFactory, its use
     * is mandatory in order to actually allow for Channel reuse.
     * 
     * @author Mark Pollack
     * @author Mark Fisher
     * @author Dave Syer
     */

    /// <summary>
    /// A caching connection factory implementation.  The default channel cache size is 1, please modify to 
    /// meet your scaling needs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="IConnectionFactory"/> implementation that returns the same Connections from all <see cref="IConnectionFactory.CreateConnection()"/>
    /// calls, and ignores calls to <see cref="RabbitMQ.Client.IConnection.Close()"/> and caches <see cref="IModel"/>.
    /// </para>
    /// <para>
    /// By default, only one Channel will be cached, with further requested Channels being created and disposed on demand.
    /// Consider raising the <see cref="ChannelCacheSize"/> value in case of a high-concurrency environment.
    /// </para>
    /// <para>
    /// <b>NOTE: This ConnectionFactory requires explicit closing of all Channels obtained form its shared Connection.</b>
    /// This is the usual recommendation for native Rabbit access code anyway. However, with this ConnectionFactory, its use
    /// is mandatory in order to actually allow for Channel reuse.
    /// </para>
    /// </remarks>
    /// <author>Mark Pollack</author>
    /// <author>Mark Fisher</author>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public class CachingConnectionFactory : AbstractConnectionFactory
    {   
        /// <summary>
        /// The channel cache size.  Default size is 1
        /// </summary>
        private int channelCacheSize = 1;

        /// <summary>
        /// The cached channels.
        /// </summary>
        private readonly LinkedList<IChannelProxy> cachedChannelsNonTransactional = new LinkedList<IChannelProxy>();

        /// <summary>
        /// The caches transactional channels.
        /// </summary>
        private readonly LinkedList<IChannelProxy> cachedChannelsTransactional = new LinkedList<IChannelProxy>();

        /// <summary>
        /// Flag for active state.
        /// </summary>
        private volatile bool active = true;

        /// <summary>
        /// The target connection.
        /// </summary>
        private ChannelCachingConnectionProxy connection;

        /// <summary>
        /// Synchronization monitor for the shared Connection.
        /// </summary>
        private readonly object connectionMonitor = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class
        /// initializing the hostname to be the value returned from Dns.GetHostName() or "localhost"
        /// if Dns.GetHostName() throws an exception.
        /// </summary>
        public CachingConnectionFactory() : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class given a host name and port
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        public CachingConnectionFactory(string hostname, int port) : base(new ConnectionFactory())
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                hostname = this.GetDefaultHostName();
            }

            this.Host = hostname;
            this.Port = port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class given a port
        /// </summary>
        /// <param name="port">
        /// The port.
        /// </param>
        public CachingConnectionFactory(int port) : this(string.Empty, port)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class given a host name.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        public CachingConnectionFactory(string hostname) : this(hostname, RabbitMQ.Client.Protocols.DefaultProtocol.DefaultPort)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class.
        /// </summary>
        /// <param name="rabbitConnectionFactory">
        /// The rabbit connection factory.
        /// </param>
        public CachingConnectionFactory(RabbitMQ.Client.ConnectionFactory rabbitConnectionFactory) : base(rabbitConnectionFactory)
        {
        }

        /// <summary>
        /// Gets or sets ChannelCacheSize.
        /// </summary>
        public int ChannelCacheSize
        {
            get
            {
                return this.channelCacheSize;
            }

            set
            {
                AssertUtils.IsTrue(value >= 1, "Channel cache size must be 1 or higher");
                this.channelCacheSize = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CachingConnectionFactory"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        internal bool Active
        {
            get { return this.active; }
            set { this.active = value; }
        }

        /// <summary>
        /// Sets the connection listeners.
        /// </summary>
        /// <value>The connection listeners.</value>
        public new IList<IConnectionListener> ConnectionListeners
        {
            set
            {
                base.ConnectionListeners = value;

                if (this.connection != null)
                {
                    this.ConnectionListener.OnCreate(this.connection);
                }
            }
        }

        /// <summary>
        /// Add a connection listener.
        /// </summary>
        /// <param name="listener">The listener.</param>
        public new void AddConnectionListener(IConnectionListener listener)
        {
            base.AddConnectionListener(listener);

            if (this.connection != null)
            {
                listener.OnCreate(this.connection);
            }
        }

        /// <summary>
        /// Get a channel, given a flag indicating whether it should be transactional or not.
        /// </summary>
        /// <param name="transactional">
        /// The transactional.
        /// </param>
        /// <returns>
        /// The channel.
        /// </returns>
        internal RabbitMQ.Client.IModel GetChannel(bool transactional)
        {
            var channelList = transactional ? this.cachedChannelsTransactional : this.cachedChannelsNonTransactional;
            RabbitMQ.Client.IModel channel = null;
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
                if (this.Logger.IsTraceEnabled)
                {
                    this.Logger.Trace("Found cached Rabbit Channel");
                }
            }
            else
            {
                if (this.Logger.IsDebugEnabled)
                {
                    this.Logger.Debug("Creating cached Rabbit Channel");
                } 

                channel = this.GetCachedChannelProxy(channelList, transactional);
            }

            return channel;
        }

        /// <summary>
        /// Wraps the given Model so that it delegates every method call to the target model but
        /// adapts close calls. This is useful for allowing application code to
        /// handle a special framework Model just like an ordinary Model.
        /// </summary>
        /// <param name="channelList">The channel list.</param>
        /// <param name="transactional">if set to <c>true</c> [transactional].</param>
        /// <returns>The wrapped Model</returns>
        protected virtual IChannelProxy GetCachedChannelProxy(LinkedList<IChannelProxy> channelList, bool transactional)
        {
            var targetChannel = this.CreateBareChannel(transactional);

            var useAdvisedClass = false;
            if (useAdvisedClass)
            {
                // Legacy Code - will wait until the new code tests correctly before removing.
                this.ChannelListener.OnCreate(targetChannel, transactional);
                return new CachedModel(targetChannel, channelList, transactional, this);
            }
            else
            {
                // var factory = new ProxyFactory(typeof(IChannelProxy), new CachedChannelInvocationHandler(targetChannel, channelList, transactional, this));
                // return (IChannelProxy)factory.GetProxy();

                var factory = new ProxyFactory(typeof(IChannelProxy), new CachedChannelInvocationHandler(targetChannel, channelList, transactional, this));
                var channelProxy = (IChannelProxy)factory.GetProxy();
                return channelProxy;
            }
        }
        
        /// <summary>
        /// Create a bare channel.
        /// </summary>
        /// <param name="transactional">
        /// The transactional.
        /// </param>
        /// <returns>
        /// The bare channel.
        /// </returns>
        internal RabbitMQ.Client.IModel CreateBareChannel(bool transactional)
        {
            if (this.connection == null || !this.connection.IsOpen())
            {
                this.connection = null;
                
                // Use CreateConnection here not DoCreateConnection so that the old one is properly disposed
                this.CreateConnection();
            }

            return this.connection.CreateBareChannel(transactional);
        }

        /// <summary>
        /// Create a connection.
        /// </summary>
        /// <returns>The connection.</returns>
        public override IConnection CreateConnection()
        {
            lock (this.connectionMonitor)
            {
                if (this.connection == null)
                {
                    this.connection = new ChannelCachingConnectionProxy(base.CreateBareConnection(), this);
                    
                    // invoke the listener *after* this.connection is assigned
                    this.ConnectionListener.OnCreate(this.connection);
                }
            }

            return this.connection;
        }

        /// <summary>
        /// Reset the Channel cache and underlying shared Connection, to be reinitialized on next access.
        /// </summary>
        public override void Dispose()
        {
            lock (this.connectionMonitor)
            {
                if (this.connection != null)
                {
                    this.connection.Dispose();
                    this.connection = null;
                }
            }

            this.Reset();
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        internal void Reset()
        {
            this.active = false;
            lock (this.cachedChannelsNonTransactional)
            {
                foreach (var channel in this.cachedChannelsNonTransactional)
                {
                    try
                    {
                        channel.GetTargetChannel().Close();
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Trace("Could not close cached Rabbit Channel", ex);
                    }
                }

                this.cachedChannelsNonTransactional.Clear();
            }

            lock (this.cachedChannelsTransactional)
            {
                foreach (var channel in this.cachedChannelsTransactional)
                {
                    try
                    {
                        channel.GetTargetChannel().Close();
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Trace("Could not close cached Rabbit Channel", ex);
                    }
                }

                this.cachedChannelsTransactional.Clear();
            }

            this.active = true;
            this.connection = null;
        }

        /// <summary>
        /// Convert object to string representation.
        /// </summary>
        /// <returns>
        /// String representation of the object.
        /// </returns>
        public override string ToString()
        {
            return "CachingConnectionFactory [channelCacheSize=" + this.channelCacheSize + ", host=" + this.Host + ", port=" +
                   this.Port + ", active=" + this.active + "]";
        }
    }

    #region CachedChannelInvocationHandler - For a Rainy Day

    /// <summary>
    /// A cached channel invocation handler.
    /// </summary>
    internal class CachedChannelInvocationHandler : IMethodInterceptor
    {
        private readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private volatile RabbitMQ.Client.IModel target;

        private readonly LinkedList<IChannelProxy> channelList;

        private readonly object targetMonitor = new object();

        private readonly bool transactional;

        private readonly CachingConnectionFactory outer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedChannelInvocationHandler"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="channelList">The channel list.</param>
        /// <param name="transactional">if set to <c>true</c> [transactional].</param>
        /// <param name="outer">The outer.</param>
        public CachedChannelInvocationHandler(RabbitMQ.Client.IModel target, LinkedList<IChannelProxy> channelList, bool transactional, CachingConnectionFactory outer)
        {
            this.target = target;
            this.channelList = channelList;
            this.transactional = transactional;
            this.outer = outer;
        }

        /// <summary>
        /// Implement this method to perform extra treatments before and after
        /// the call to the supplied <paramref name="invocation"/>.
        /// </summary>
        /// <param name="invocation">The method invocation that is being intercepted.</param>
        /// <returns>The result of the call to the
        /// <see cref="M:AopAlliance.Intercept.IJoinpoint.Proceed"/> method of
        /// the supplied <paramref name="invocation"/>; this return value may
        /// well have been intercepted by the interceptor.</returns>
        /// <exception cref="T:System.Exception">
        /// If any of the interceptors in the chain or the target object itself
        /// throws an exception.
        /// </exception>
        public object Invoke(IMethodInvocation invocation)
        {
            this.Logger.Info(string.Format("Method Intercepted: {0}", invocation.Method.Name));

            var methodName = invocation.Method.Name;
            if (methodName == "TxSelect" && !this.transactional)
            {
                throw new InvalidOperationException("Cannot start transaction on non-transactional channel");
            }

            if (methodName == "Equals")
            {
                // Only consider equal when proxies are identical.
                return invocation.Proxy == invocation.Arguments[0];
            }
            else if (methodName == "GetHashCode")
            {
                // Use hashCode of Channel proxy.
                return invocation.Proxy.GetHashCode();
            }
            else if (methodName == "ToString")
            {
                return "Cached Rabbit Channel: " + this.target;
            }
            else if (methodName == "Close")
            {
                // Handle close method: don't pass the call on.
                if (this.outer.Active)
                {
                    lock (this.channelList)
                    {
                        if (this.channelList.Count < this.outer.ChannelCacheSize)
                        {
                            this.LogicalClose((IChannelProxy)invocation.Proxy);

                            // Remain open in the channel list.
                            return null;
                        }
                    }
                }

                // If we get here, we're supposed to shut down.
                this.PhysicalClose();
                return null;
            }
            else if (methodName == "GetTargetChannel")
            {
                // Handle getTargetChannel method: return underlying Channel.
                return this.target;
            }
            else if (methodName == "get_IsOpen")
            {
                // Handle isOpen method: we are closed if the target is closed
                return this.target != null && this.target.IsOpen;
            }

            try
            {
                if (this.target == null || !this.target.IsOpen)
                {
                    this.target = null;
                }

                lock (this.targetMonitor)
                {
                    if (this.target == null)
                    {
                        this.target = this.outer.CreateBareChannel(this.transactional);
                    }

                    return invocation.Method.Invoke(this.target, invocation.Arguments);
                }
            }
            catch (TargetInvocationException ex)
            {
                if (this.target == null || !this.target.IsOpen)
                {
                    // Basic re-connection logic...
                    this.target = null;
                    this.Logger.Debug("Detected closed channel on exception.  Re-initializing: " + this.target);
                    lock (this.targetMonitor)
                    {
                        if (this.target == null)
                        {
                            this.target = this.outer.CreateBareChannel(this.transactional);
                        }
                    }
                }

                throw ex.GetBaseException();
            }
        }

        /// <summary>
        /// GUARDED by ChannelList
        /// </summary>
        /// <param name="proxy">The channel to close.</param>
        private void LogicalClose(IChannelProxy proxy)
        {
            if (this.target != null && !this.target.IsOpen)
            {
                lock (this.targetMonitor)
                {
                    if (!this.target.IsOpen)
                    {
                        this.target = null;
                        return;
                    }
                }
            }

            // Allow for multiple close calls...
            // TODO: Figure out why the proxied channels don't work with this.channelList.Contains()
            // if (!this.channelList.Contains(proxy))
            // {
                if (this.Logger.IsTraceEnabled)
                {
                    this.Logger.Trace("Returning cached Channel: " + this.target);
                }
                this.channelList.AddLast(proxy);
            // }
        }

        /// <summary>
        /// Closes the cached channel.
        /// </summary>
        private void PhysicalClose()
        {
            if (this.Logger.IsDebugEnabled)
            {
                this.Logger.Debug("Closing cached Channel: " + this.target);
            }

            if (this.target == null)
            {
                return;
            }

            if (this.target.IsOpen)
            {
                lock (this.targetMonitor)
                {
                    if (this.target.IsOpen)
                    {
                        this.target.Close();
                    }
                    this.target = null;
                }
            }
        }
    }
    #endregion

    /// <summary>
    /// A channel caching connection proxy.
    /// </summary>
    internal class ChannelCachingConnectionProxy : IConnection, IConnectionProxy, IDisposable
    {
        /// <summary>
        /// The target connection.
        /// </summary>
        private volatile IConnection target;

        /// <summary>
        /// The outer caching connection factory.
        /// </summary>
        private volatile CachingConnectionFactory outer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelCachingConnectionProxy"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="outer">The outer.</param>
        public ChannelCachingConnectionProxy(IConnection target, CachingConnectionFactory outer)
        {
            this.target = target;
            this.outer = outer;
        }

        /// <summary>
        /// Create a bare channel, given a flag indicating whether it should be transactional or not.
        /// </summary>
        /// <param name="transactional">The transactional.</param>
        /// <returns>The channel.</returns>
        internal RabbitMQ.Client.IModel CreateBareChannel(bool transactional)
        {
            return this.target.CreateChannel(transactional);
        }

        /// <summary>
        /// Create a channel, given a flag indicating whether it should be transactional or not.
        /// </summary>
        /// <param name="transactional">The transactional.</param>
        /// <returns>The channel.</returns>
        public RabbitMQ.Client.IModel CreateChannel(bool transactional)
        {
            var channel = this.outer.GetChannel(transactional);
            return channel;
        }

        /// <summary>
        /// Close the connection.
        /// </summary>
        public void Close()
        {
        }

        public void Dispose()
        {
            if (this.target != null)
            {
                this.outer.ConnectionListener.OnClose(this.target);
                RabbitUtils.CloseConnection(this.target);
            }

            this.outer.Reset();
            this.target = null;
        }

        /// <summary>
        /// Determine if the connection is open.
        /// </summary>
        /// <returns>
        /// True if open, else false.
        /// </returns>
        public bool IsOpen()
        {
            return this.target != null && this.target.IsOpen();
        }

        /// <summary>
        /// Get the targetconnection.
        /// </summary>
        /// <returns>The target connection.</returns>
        public IConnection GetTargetConnection()
        {
            return this.target;
        }

        /// <summary>
        /// Get the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return 31 + ((this.target == null) ? 0 : this.target.GetHashCode());
        }

        /// <summary>
        /// Determine equality of this object with the supplied object.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>True if the same, else false.</returns>
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

            var other = (ChannelCachingConnectionProxy)obj;
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
            /// Convert to string.
            /// </summary>
            /// <returns>String representation of object.</returns>
        public override string ToString()
        {
            return "Shared Rabbit Connection: " + this.target;
        }

    }
}