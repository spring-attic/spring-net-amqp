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
using Spring.Transaction.Support;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class RabbitResourceHolder : ResourceHolderSupport
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ResourceHolderSupport));

        private IConnectionFactory connectionFactory;

        private bool frozen = false;

        private readonly IList<IConnection> connections = new List<IConnection>();

        private readonly IList<IModel> channels = new List<IModel>();

        private readonly IDictionary<IConnection, List<IModel>> channelsPerConnection = 
            new Dictionary<IConnection, List<IModel>>();

        public RabbitResourceHolder()
        {
        }

        public RabbitResourceHolder(IConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public RabbitResourceHolder(IModel channel)
        {
            AddChannel(channel);
            this.frozen = true;
        }

        public RabbitResourceHolder(IConnection connection, IModel channel)
        {
            AddConnection(connection);
            AddChannel(channel, connection);
            this.frozen = true;
        }

        public RabbitResourceHolder(IConnectionFactory connectionFactory, IConnection connection, IModel channel)
        {
            this.connectionFactory = connectionFactory;
            AddConnection(connection);
            AddChannel(channel);
            this.frozen = true;
        }

        public bool Frozen
        {
            get { return frozen; }
        }

        private void AddConnection(IConnection connection)
        {
            AssertUtils.IsTrue(!this.frozen, "Cannot add Connection because RabbitResourceHolder is frozen");
            AssertUtils.ArgumentNotNull(connection, "Connection must not be null");
            if (!this.connections.Contains(connection))
            {
                this.connections.Add(connection);
            }
        }

        public void AddChannel(IModel channel)
        {
            AddChannel(channel, null);
        }

        public void AddChannel(IModel channel, IConnection connection)
        {
            AssertUtils.IsTrue(!this.frozen, "Cannot add Channel because RabbitResourceHolder is frozen");
            AssertUtils.ArgumentNotNull(channel, "Channel must not be null");
            if (!this.channels.Contains(channel))
            {
                this.channels.Add(channel);
                if (connection != null)
                {
                    List<IModel> channels = this.channelsPerConnection[connection];
                    //TODO double check, what about TryGet..
                    if (channels == null)
                    {
                        channels = new List<IModel>();
                        this.channelsPerConnection.Add(connection, channels);
                    }
                    channels.Add(channel);
                }
            }
        }

        public bool ContainsChannel(IModel channel)
        {
            return channels.Contains(channel);
        }

        public IConnection Connection
        {
            get
            {
               return (this.connections.Count != 0 ? this.connections[0] : null);
            }
        }

        //TODO public Connection getConnection(Class<? extends Connection> connectionType)

        public IModel Channel
        {
            get { return (this.channels.Count !=0 ? this.channels[0] : null); }
        }

        //TODO public Channel getChannel(Class<? extends Channel> channelType) {

        //TODO public Channel getChannel(Class<? extends Channel> channelType, Connection connection) {

        public void CommitAll()
        {
            foreach (IModel channel in channels)
            {
                channel.TxCommit();
            }
        }

        public void CloseAll()
        {
            foreach (IModel channel in channels)
            {
                try
                {
                    channel.Close();
                } catch (Exception ex)
                {
                    logger.Debug("Could not close synchronized Rabbit Channel after transaction", ex);
                }
            }

            foreach (IConnection connection in connections)
            {
                ConnectionFactoryUtils.ReleaseConnection(connection, this.connectionFactory);
            }

            this.connections.Clear();
            this.channels.Clear();
            this.channelsPerConnection.Clear();
        }


    }

}