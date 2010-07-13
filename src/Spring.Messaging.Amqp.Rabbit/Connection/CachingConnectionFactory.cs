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
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// 
    /// </summary>
    /// <author>Mark Pollack</author>
    public class CachingConnectionFactory : SingleConnectionFactory, IDisposable
    {

        #region Logging Definition

        private static readonly ILog LOG = LogManager.GetLogger(typeof(CachingConnectionFactory));

        #endregion

        private int channelCacheSize = 1;

        private LinkedList<IModel> cachedChannels = new LinkedList<IModel>();

        private volatile bool active = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public CachingConnectionFactory() : base()
        {
            
        }

        public CachingConnectionFactory(ConnectionParameters connectionParameters)
            : this(connectionParameters, "localhost:" + RabbitUtils.DEFAULT_PORT)
        {
            
        }

        public CachingConnectionFactory(ConnectionParameters connectionParameters, string address) : base (new ConnectionFactory(), address )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingConnectionFactory"/> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="address">The address.</param>
        public CachingConnectionFactory(ConnectionFactory connectionFactory, string address) : base(connectionFactory, address)
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
     
        public override IModel GetChannel(IConnection connection)
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
                IModel targetModel = base.CreateChannel(connection);
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
        /// <returns>The wrapped Model</returns>
        protected virtual IModel GetCachedModelWrapper(IModel targetModel, LinkedList<IModel> modelList)
        {
            return new CachedModel(targetModel, modelList, this);
        }


        public override void ResetConnection()
        {
            this.active = false;
            lock (this.cachedChannels)
            {
                foreach (IModel channel in cachedChannels)
                {
                    try
                    {
                        channel.Close();
                    }
                    catch (Exception ex)
                    {
                        LOG.Trace("Could not close cached Rabbit Channel", ex);
                    }
                }
            }
            this.cachedChannels.Clear();

            this.active = true;
            base.ResetConnection();
        }
    }

   
}