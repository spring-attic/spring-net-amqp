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
using org.apache.qpid.client;
using Spring.Messaging.Amqp.Qpid.Core;
using Spring.Transaction.Support;
using Spring.Util;

namespace Spring.Messaging.Amqp.Qpid.Client
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class QpidResourceHolder : ResourceHolderSupport
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ResourceHolderSupport));

        private IClientFactory clientFactory;

        private bool frozen = false;

        private readonly IList<IClient> connections = new List<IClient>();

        private readonly IList<IClientSession> channels = new List<IClientSession>();

        private readonly IDictionary<IClient, List<IClientSession>> channelsPerConnection = 
            new Dictionary<IClient, List<IClientSession>>();

        public QpidResourceHolder()
        {
        }

        public QpidResourceHolder(IClientFactory connectionFactory)
        {
            this.clientFactory = connectionFactory;
        }

        public QpidResourceHolder(IClientSession channel)
        {
            AddChannel(channel);
            this.frozen = true;
        }

        public QpidResourceHolder(IClient connection, IClientSession channel)
        {
            AddConnection(connection);
            AddChannel(channel, connection);
            this.frozen = true;
        }

        public QpidResourceHolder(IClientFactory connectionFactory, IClient connection, IClientSession channel)
        {
            this.clientFactory = connectionFactory;
            AddConnection(connection);
            AddChannel(channel);
            this.frozen = true;
        }

        public bool Frozen
        {
            get { return frozen; }
        }

        private void AddConnection(IClient connection)
        {
            AssertUtils.IsTrue(!this.frozen, "Cannot add Connection because RabbitResourceHolder is frozen");
            AssertUtils.ArgumentNotNull(connection, "Connection must not be null");
            if (!this.connections.Contains(connection))
            {
                this.connections.Add(connection);
            }
        }

        public void AddChannel(IClientSession channel)
        {
            AddChannel(channel, null);
        }

        public void AddChannel(IClientSession channel, IClient connection)
        {
            AssertUtils.IsTrue(!this.frozen, "Cannot add Channel because RabbitResourceHolder is frozen");
            AssertUtils.ArgumentNotNull(channel, "Channel must not be null");
            if (!this.channels.Contains(channel))
            {
                this.channels.Add(channel);
                if (connection != null)
                {
                    List<IClientSession> channels = this.channelsPerConnection[connection];
                    //TODO double check, what about TryGet..
                    if (channels == null)
                    {
                        channels = new List<IClientSession>();
                        this.channelsPerConnection.Add(connection, channels);
                    }
                    channels.Add(channel);
                }
            }
        }

        public bool ContainsChannel(IClientSession channel)
        {
            return channels.Contains(channel);
        }

        public IClient Connection
        {
            get
            {
                return (this.connections.Count != 0 ? this.connections[0] : null);
            }
        }

        //TODO public Connection getConnection(Class<? extends Connection> connectionType)

        public IClientSession Channel
        {
            get { return (this.channels.Count !=0 ? this.channels[0] : null); }
        }

        //TODO public Channel getChannel(Class<? extends Channel> channelType) {

        //TODO public Channel getChannel(Class<? extends Channel> channelType, Connection connection) {

        public void CommitAll()
        {
            foreach (IClientSession channel in channels)
            {
                channel.TxCommit();
            }
        }

        public void CloseAll()
        {
            foreach (IClientSession channel in channels)
            {
                try
                {
                    channel.Close();
                } catch (Exception ex)
                {
                    logger.Debug("Could not close synchronized Rabbit Channel after transaction", ex);
                }
            }

            foreach (IClient connection in connections)
            {
                ClientFactoryUtils.ReleaseConnection(connection, this.clientFactory);
            }

            this.connections.Clear();
            this.channels.Clear();
            this.channelsPerConnection.Clear();
        }


    }
}