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
using Common.Logging;
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
    /// A caching connection factory implementation.
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

        private int channelCacheSize = 1;

        /// <summary>
        /// The cached channels.
        /// </summary>
        private readonly LinkedList<IChannelProxy> cachedChannels = new LinkedList<IChannelProxy>();

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
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class.
        /// </summary>
        public CachingConnectionFactory() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class.
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
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class.
        /// </summary>
        /// <param name="port">
        /// The port.
        /// </param>
        public CachingConnectionFactory(int port) : base(port)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class.
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
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="address">The address.</param>
        public CachingConnectionFactory(RabbitMQ.Client.ConnectionFactory connectionFactory, string address) : base(connectionFactory)
        {
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

        public override RabbitMQ.Client.IModel GetChannel(IConnection connection)
        {
            LinkedList<RabbitMQ.Client.IModel> channelList = this.cachedChannels;
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
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Found cached Rabbit Channel");
                }
            }
            else
            {
                RabbitMQ.Client.IModel targetModel = base.CreateModel(connection);
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Creating cached Rabbit Channel");
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
        /// <returns>The wrapped Model</returns>
        protected virtual RabbitMQ.Client.IModel GetCachedModelWrapper(RabbitMQ.Client.IModel targetModel, LinkedList<RabbitMQ.Client.IModel> modelList)
        {
            return new CachedModel(targetModel, modelList, this);
        }


        public override void ResetConnection()
        {
            this.active = false;
            lock (this.cachedChannels)
            {
                foreach (RabbitMQ.Client.IModel channel in cachedChannels)
                {
                    try
                    {
                        channel.Close();
                    }
                    catch (Exception ex)
                    {
                        logger.Trace("Could not close cached Rabbit Channel", ex);
                    }
                }
            }
            this.cachedChannels.Clear();

            this.active = true;
            base.ResetConnection();
        }
    }

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
        private RabbitMQ.Client.IModel CreateBareChannel(bool transactional)
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
            var channel = outer.GetChannel(transactional);
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
        public new int GetHashCode()
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
        public new string ToString()
        {
            return "Shared Rabbit Connection: " + this.target;
        }

    }
}