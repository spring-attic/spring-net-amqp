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

    /**
     * NOTE: this ConnectionFactory implementation is considered <b>experimental</b> at this stage. There are concerns to be
     * addressed in relation to the statefulness of channels. Therefore, we recommend using {@link SingleConnectionFactory}
     * for now.
     * 
     * A {@link ConnectionFactory} implementation that returns the same Connections from all {@link #createConnection()}
     * calls, and ignores calls to {@link com.rabbitmq.client.Connection#close()} and caches
     * {@link com.rabbitmq.client.Channel}.
     * 
     * <p>
     * By default, only one single Session will be cached, with further requested Channels being created and disposed on
     * demand. Consider raising the {@link #setChannelCacheSize(int) "channelCacheSize" value} in case of a high-concurrency
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
    /// <author>Mark Pollack</author>
    public class CachingConnectionFactory : SingleConnectionFactory, IDisposable
    {
        #region Logging Definition

        /// <summary>
        /// The logger.
        /// </summary>
        private static readonly ILog logger = LogManager.GetLogger(typeof(CachingConnectionFactory));

        #endregion

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
        private ChannelCachingConnectionProxy targetConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class 
        /// initializing the hostname to be the value returned from Dns.GetHostName() or "localhost"
        /// if Dns.GetHostName() throws an exception.
        /// </summary>
        public CachingConnectionFactory() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class given a host name and port
        /// </summary>
        /// <param name="hostName">
        /// The host name.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        public CachingConnectionFactory(string hostName, int port) : base(hostName, port)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class given a port
        /// </summary>
        /// <param name="port">
        /// The port.
        /// </param>
        public CachingConnectionFactory(int port) : base(port)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class given a host name
        /// </summary>
        /// <param name="hostName">
        /// The host name.
        /// </param>
        public CachingConnectionFactory(string hostName) : base(hostName)
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
        /// <value>
        ///   <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active
        {
            get { return active; }
            set { active = value; }
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
                #region Logging
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Found cached Rabbit Channel");
                } 
                #endregion
            }
            else
            {
                #region Logging
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Creating cached Rabbit Channel");
                } 
                #endregion
                channel = this.GetCachedModelWrapper(channelList, transactional);
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
        /// <returns>The wrapped Model</returns>
        protected virtual IChannelProxy GetCachedModelWrapper(LinkedList<IChannelProxy> channelList, bool transactional)
        {
            IModel targetModel = CreateBareChannel(transactional);
            #region Logging
            if (logger.IsDebugEnabled)
            {
                logger.Debug("Creating cached Rabbit Channel from " + targetModel);
            } 
            #endregion
            return new CachedModel(targetModel, channelList, transactional, this);

        }

        #region For Proxy Approach
        /*/// <summary>
        /// Get a cached channel proxy.
        /// </summary>
        /// <param name="channelList">
        /// The channel list.
        /// </param>
        /// <param name="transactional">
        /// The transactional.
        /// </param>
        /// <returns>
        /// The channel proxy.
        /// </returns>
        private IChannelProxy GetCachedChannelProxy(LinkedList<IChannelProxy> channelList, bool transactional)
        {
            // #1 Wrapper Implementation
            

            // #2 Alternate (Proxy) Implementation.
            var targetChannel = CreateBareChannel(transactional);
            if (logger.IsDebugEnabled)
            {
                logger.Debug("Creating cached Rabbit Channel from " + targetChannel);
            }

            //ProxyFactory factory = new ProxyFactory(myBusinessInterfaceImpl);
            //factory.AddAdvice(myMethodInterceptor);
            //factory.AddAdvisor(myAdvisor);
            //return (IChannelProxy)factory.GetProxy();
            // TODO: FIx this!
            //return (IChannelProxy) Proxy.NewProxyInstance(IChannelProxy.class.getClassLoader(),
            //new Class[] { ChannelProxy.class }, new CachedChannelInvocationHandler(targetChannel, channelList,
            //        transactional));
            // TODO: Remove return null!!
            return null;
        }*/
        #endregion
        
        /// <summary>
        /// Create a bare channel.
        /// </summary>
        /// <param name="transactional">
        /// The transactional.
        /// </param>
        /// <returns>
        /// The bare channel.
        /// </returns>
        public RabbitMQ.Client.IModel CreateBareChannel(bool transactional)
        {
            return this.targetConnection.CreateBareChannel(transactional);
        }

        /// <summary>
        /// Create a connection.
        /// </summary>
        /// <returns>
        /// The connection.
        /// </returns>
        protected override IConnection DoCreateConnection()
        {
            targetConnection = new ChannelCachingConnectionProxy(base.DoCreateConnection(), this);
            return targetConnection;
        }

        /// <summary>
        /// Reset the Channel cache and underlying shared Connection, to be reinitialized on next access.
        /// </summary>
        public override void Dispose()
        {
            this.active = false;
            lock (this.cachedChannelsNonTransactional)
            {
                foreach (var channel in cachedChannelsNonTransactional)
                {
                    try
                    {
                        channel.GetTargetChannel().Close();
                    }
                    catch (Exception ex)
                    {
                        logger.Trace("Could not close cached Rabbit Channel", ex);
                    }
                }
                this.cachedChannelsNonTransactional.Clear();
            }
            this.active = true;
            base.Dispose();
            this.targetConnection = null;
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

    /*internal class CachedChannelInvocationHandler : IInterceptor {

		private volatile RabbitMQ.Client.IModel target;

		private readonly LinkedList<IChannelProxy> channelList;

		private readonly static object targetMonitor = new object();

		private readonly bool transactional;

		public CachedChannelInvocationHandler(RabbitMQ.Client.IModel target, LinkedList<IChannelProxy> channelList, bool transactional) 
        {
			this.target = target;
			this.channelList = channelList;
			this.transactional = transactional;
		}

		public object Invoke(Object proxy, Method method, Object[] args)
        {
			string methodName = method.Name;
			if (methodName.equals("txSelect") && !this.transactional) {
				throw new UnsupportedOperationException("Cannot start transaction on non-transactional channel");
			}
			if (methodName.equals("equals")) {
				// Only consider equal when proxies are identical.
				return (proxy == args[0]);
			} else if (methodName.equals("hashCode")) {
				// Use hashCode of Channel proxy.
				return System.identityHashCode(proxy);
			} else if (methodName.equals("toString")) {
				return "Cached Rabbit Channel: " + this.target;
			} else if (methodName.equals("close")) {
				// Handle close method: don't pass the call on.
				if (active) {
					synchronized (this.channelList) {
						if (this.channelList.size() < getChannelCacheSize()) {
							logicalClose((ChannelProxy) proxy);
							// Remain open in the channel list.
							return null;
						}
					}
				}

				// If we get here, we're supposed to shut down.
				physicalClose();
				return null;
			} else if (methodName.equals("getTargetChannel")) {
				// Handle getTargetChannel method: return underlying Channel.
				return this.target;
			}
			try {
				synchronized (targetMonitor) {
					if (this.target == null) {
						this.target = createBareChannel(transactional);
					}
				}
				return method.invoke(this.target, args);
			} catch (InvocationTargetException ex) {
				if (!this.target.isOpen()) {
					// Basic re-connection logic...
					logger.debug("Detected closed channel on exception.  Re-initializing: " + target);
					synchronized (targetMonitor) {
						if (!this.target.isOpen()) {
							this.target = createBareChannel(transactional);
						}
					}
				}
				throw ex.getTargetException();
			}
		}

		private void logicalClose(ChannelProxy proxy) throws Exception {
			if (!this.target.isOpen()) {
				synchronized (targetMonitor) {
					if (!this.target.isOpen()) {
						this.target = null;
						return;
					}
				}
			}
			// Allow for multiple close calls...
			if (!this.channelList.contains(proxy)) {
				if (logger.isTraceEnabled()) {
					logger.trace("Returning cached Channel: " + this.target);
				}
				this.channelList.addLast(proxy);
			}
		}

		private void physicalClose() throws Exception {
			if (logger.isDebugEnabled()) {
				logger.debug("Closing cached Channel: " + this.target);
			}
			if (this.target == null) {
				return;
			}
			if (this.target.isOpen()) {
				synchronized (targetMonitor) {
					if (this.target.isOpen()) {
						this.target.close();
					}
					this.target = null;
				}
			}
		}

	}*/
    #endregion

    /// <summary>
    /// A channel caching connection proxy.
    /// </summary>
    internal sealed class ChannelCachingConnectionProxy : IConnection, IConnectionProxy
    {
        /// <summary>
        /// The target connection.
        /// </summary>
        private volatile IConnection target;

        private volatile CachingConnectionFactory outer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelCachingConnectionProxy"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        public ChannelCachingConnectionProxy(IConnection target, CachingConnectionFactory outer)
        {
            this.target = target;
            this.outer = outer;
        }

        /// <summary>
        /// Create a bare channel, given a flag indicating whether it should be transactional or not.
        /// </summary>
        /// <param name="transactional">
        /// The transactional.
        /// </param>
        /// <returns>
        /// The channel.
        /// </returns>
        internal RabbitMQ.Client.IModel CreateBareChannel(bool transactional)
        {
            return this.target.CreateChannel(transactional);
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
            this.target.Close();
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
        /// <returns>
        /// The target connection.
        /// </returns>
        public IConnection GetTargetConnection()
        {
            return this.target;
        }

        /// <summary>
        /// Get the hash code.
        /// </summary>
        /// <returns>
        /// The hash code.
        /// </returns>
        public override int GetHashCode()
        {
            return 31 + ((this.target == null) ? 0 : this.target.GetHashCode());
        }

        /// <summary>
        /// Determine equality of this object with the supplied object.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <returns>
        /// True if the same, else false.
        /// </returns>
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

            if (!(obj is ChannelCachingConnectionProxy))
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
        /// <returns>
        /// String representation of object.
        /// </returns>
        public override string ToString()
        {
            return "Shared Rabbit Connection: " + this.target;
        }

    }
}