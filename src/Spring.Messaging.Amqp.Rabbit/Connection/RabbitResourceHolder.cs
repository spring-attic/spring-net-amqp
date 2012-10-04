// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitResourceHolder.cs" company="The original author or authors.">
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
using Common.Logging;
using RabbitMQ.Client;
using Spring.Transaction.Support;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A rabbit resource holder.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class RabbitResourceHolder : ResourceHolderSupport
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ResourceHolderSupport));

        /// <summary>
        /// The frozen flag.
        /// </summary>
        private bool frozen = false;

        /// <summary>
        /// The connections.
        /// </summary>
        private readonly LinkedList<IConnection> connections = new LinkedList<IConnection>();

        /// <summary>
        /// The channels.
        /// </summary>
        private readonly LinkedList<IModel> channels = new LinkedList<IModel>();

        /// <summary>
        /// The channels per connection.
        /// </summary>
        private readonly IDictionary<IConnection, List<IModel>> channelsPerConnection = new Dictionary<IConnection, List<IModel>>();

        /// <summary>
        /// The delivery tags.
        /// </summary>
        private readonly IDictionary<IModel, List<long>> deliveryTags = new Dictionary<IModel, List<long>>();

        /// <summary>
        /// The transactional flag.
        /// </summary>
        private bool transactional;

        /// <summary>
        /// Release after completion.
        /// </summary>
        private readonly bool releaseAfterCompletion = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitResourceHolder"/> class.
        /// </summary>
        public RabbitResourceHolder() { }

        /// <summary>Initializes a new instance of the <see cref="RabbitResourceHolder"/> class.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="releaseAfterCompletion">The release After Completion.</param>
        public RabbitResourceHolder(IModel channel, bool releaseAfterCompletion) : this()
        {
            this.AddChannel(channel);
            this.releaseAfterCompletion = releaseAfterCompletion;
        }

        /// <summary>
        /// Gets a value indicating whether Frozen.
        /// </summary>
        public bool Frozen { get { return this.frozen; } }

        /// <summary>Gets a value indicating whether release after completion.</summary>
        public bool ReleaseAfterCompletion { get { return this.releaseAfterCompletion; } }

        /// <summary>
        /// Gets a value indicating whether IsChannelTransactional.
        /// </summary>
        public bool IsChannelTransactional { get { return this.transactional; } }

        /// <summary>Add a connection.</summary>
        /// <param name="connection">The connection.</param>
        public void AddConnection(IConnection connection)
        {
            AssertUtils.IsTrue(!this.frozen, "Cannot add Connection because RabbitResourceHolder is frozen");
            AssertUtils.ArgumentNotNull(connection, "Connection must not be null");
            if (!this.connections.Contains(connection))
            {
                this.connections.AddLast(connection);
            }
        }

        /// <summary>Add a channel.</summary>
        /// <param name="channel">The channel.</param>
        public void AddChannel(IModel channel) { this.AddChannel(channel, null); }

        /// <summary>Add a channel.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="connection">The connection.</param>
        public void AddChannel(IModel channel, IConnection connection)
        {
            AssertUtils.IsTrue(!this.frozen, "Cannot add Channel because RabbitResourceHolder is frozen");
            AssertUtils.ArgumentNotNull(channel, "Channel must not be null");
            if (!this.channels.Contains(channel))
            {
                this.channels.AddLast(channel);
                if (connection != null)
                {
                    List<IModel> tempChannels;
                    this.channelsPerConnection.TryGetValue(connection, out tempChannels);

                    if (tempChannels == null)
                    {
                        tempChannels = new List<IModel>();
                        this.channelsPerConnection.Add(connection, tempChannels);
                    }

                    this.channels.AddLast(channel);
                }
            }
        }

        /// <summary>Determine if the channel is in the channels.</summary>
        /// <param name="channel">The channel.</param>
        /// <returns>True if the channel is in channels; otherwise false.</returns>
        public bool ContainsChannel(IModel channel) { return this.channels.Contains(channel); }

        /// <summary>
        /// Gets Connection.
        /// </summary>
        public IConnection Connection { get { return (this.connections != null && this.connections.Count > 0) ? this.connections.First.Value : null; } }

        /// <summary>
        /// Gets a connection.
        /// </summary>
        /// <typeparam name="T">
        /// T, where T is IConnection
        /// </typeparam>
        /// <returns>
        /// The connection.
        /// </returns>
        public IConnection GetConnection<T>() where T : IConnection
        {
            Type type = typeof(T);
            return (IConnection)CollectionUtils.FindValueOfType(this.connections, type);
        }

        /// <summary>The get connection.</summary>
        /// <param name="connectionType">The connection type.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The Spring.Messaging.Amqp.Rabbit.Connection.IConnection.</returns>
        public IConnection GetConnection<T>(Type connectionType) where T : IConnection { return (T)CollectionUtils.FindValueOfType(this.connections, connectionType); }

        /// <summary>
        /// Gets Channel.
        /// </summary>
        public IModel Channel { get { return (this.channels != null && this.channels.Count > 0) ? this.channels.First.Value : null; } }

        /// <summary>
        /// Commit all delivery tags.
        /// </summary>
        /// <exception cref="AmqpException">
        /// </exception>
        public void CommitAll()
        {
            try
            {
                foreach (var channel in this.channels)
                {
                    if (this.deliveryTags.ContainsKey(channel))
                    {
                        foreach (var deliveryTag in this.deliveryTags[channel])
                        {
                            channel.BasicAck((ulong)deliveryTag, false);
                        }
                    }

                    channel.TxCommit();
                }
            }
            catch (Exception e)
            {
                throw new AmqpException("Failed to commit RabbitMQ transaction", e);
            }
        }

        /// <summary>
        /// Close all channels and connections.
        /// </summary>
        public void CloseAll()
        {
            foreach (var channel in this.channels)
            {
                try
                {
                    channel.Close();
                }
                catch (Exception ex)
                {
                    Logger.Debug("Could not close synchronized Rabbit Channel after transaction", ex);
                }
            }

            foreach (var connection in this.connections)
            {
                RabbitUtils.CloseConnection(connection);
            }

            this.connections.Clear();
            this.channels.Clear();
            this.channelsPerConnection.Clear();
        }

        /// <summary>Add a delivery tag to the channel.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="deliveryTag">The delivery tag.</param>
        public void AddDeliveryTag(IModel channel, long deliveryTag)
        {
            if (this.deliveryTags.ContainsKey(channel))
            {
                var existingTags = this.deliveryTags[channel];
                existingTags.Add(deliveryTag);
                this.deliveryTags[channel] = existingTags;
            }
            else
            {
                this.deliveryTags.Add(channel, new List<long> { deliveryTag });
            }
        }

        /// <summary>
        /// Rollback all.
        /// </summary>
        /// <exception cref="AmqpException">
        /// </exception>
        public void RollbackAll()
        {
            foreach (var channel in this.channels)
            {
                Logger.Debug(m => m("Rollingback messages to channel: {0}", channel));

                RabbitUtils.RollbackIfNecessary(channel);
                if (this.deliveryTags.ContainsKey(channel))
                {
                    foreach (var deliveryTag in this.deliveryTags[channel])
                    {
                        try
                        {
                            channel.BasicReject((ulong)deliveryTag, true);
                        }
                        catch (Exception ex)
                        {
                            throw new AmqpException(ex);
                        }
                    }

                    // Need to commit the reject (=nack)
                    RabbitUtils.CommitIfNecessary(channel);
                }
            }
        }
    }
}
