// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlockingQueueConsumer.cs" company="The original author or authors.">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    /// Specialized consumer encapsulating knowledge of the broker connections and having its own lifecycle (start and stop).
    /// </summary>
    /// <author>Mark Pollack</author>
    public class BlockingQueueConsumer
    {
        #region Private Fields

        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        // This must be an unbounded queue or we risk blocking the Connection thread.
        internal readonly BlockingCollection<Delivery> queue = new BlockingCollection<Delivery>();

        // When this is non-null the connection has been closed (should never happen in normal operation).
        internal volatile ShutdownEventArgs shutdown;

        private readonly string[] queues;

        private readonly int prefetchCount;

        private readonly bool transactional;

        private IModel channel;

        private InternalConsumer consumer;

        internal readonly AtomicBoolean cancelled = new AtomicBoolean(false);

        internal readonly AtomicBoolean cancelReceived = new AtomicBoolean(false);

        internal readonly AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode;

        private readonly IConnectionFactory connectionFactory;

        private readonly IMessagePropertiesConverter messagePropertiesConverter;

        internal readonly ActiveObjectCounter<BlockingQueueConsumer> activeObjectCounter;

        /// <summary>
        /// The delivery tags.
        /// </summary>
        internal readonly LinkedList<long> deliveryTags = new LinkedList<long>();

        private readonly bool defaultRequeueRejected;
        #endregion

        #region Constructors

        /// <summary>Initializes a new instance of the <see cref="BlockingQueueConsumer"/> class.  Create a consumer. The consumer must not attempt to use the connection factory or communicate with the broker until it is started.</summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="messagePropertiesConverter">The message properties converter.</param>
        /// <param name="activeObjectCounter">The active object counter.</param>
        /// <param name="acknowledgeMode">The acknowledge mode.</param>
        /// <param name="transactional">if set to <c>true</c> [transactional].</param>
        /// <param name="prefetchCount">The prefetch count.</param>
        /// <param name="queues">The queues.</param>
        public BlockingQueueConsumer(
            IConnectionFactory connectionFactory, 
            IMessagePropertiesConverter messagePropertiesConverter, 
            ActiveObjectCounter<BlockingQueueConsumer> activeObjectCounter, 
            AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode, 
            bool transactional, 
            int prefetchCount, 
            params string[] queues) : this(connectionFactory, messagePropertiesConverter, activeObjectCounter, acknowledgeMode, transactional, prefetchCount, true, queues) { }

        /// <summary>Initializes a new instance of the <see cref="BlockingQueueConsumer"/> class.  Create a consumer. The consumer must not attempt to use the connection factory or communicate with the broker until it is started.</summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="messagePropertiesConverter">The message properties converter.</param>
        /// <param name="activeObjectCounter">The active object counter.</param>
        /// <param name="acknowledgeMode">The acknowledge mode.</param>
        /// <param name="transactional">if set to <c>true</c> [transactional].</param>
        /// <param name="prefetchCount">The prefetch count.</param>
        /// <param name="defaultRequeueRejected">The default requeue rejected.</param>
        /// <param name="queues">The queues.</param>
        public BlockingQueueConsumer(
            IConnectionFactory connectionFactory, 
            IMessagePropertiesConverter messagePropertiesConverter, 
            ActiveObjectCounter<BlockingQueueConsumer> activeObjectCounter, 
            AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode, 
            bool transactional, 
            int prefetchCount, 
            bool defaultRequeueRejected, 
            params string[] queues)
        {
            this.connectionFactory = connectionFactory;
            this.messagePropertiesConverter = messagePropertiesConverter;
            this.activeObjectCounter = activeObjectCounter;
            this.acknowledgeMode = acknowledgeMode;
            this.transactional = transactional;
            this.prefetchCount = prefetchCount;
            this.defaultRequeueRejected = defaultRequeueRejected;
            this.queues = queues;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the channel.
        /// </summary>
        public IModel Channel { get { return this.channel; } }

        /// <summary>
        /// Retrieve the consumer tag this consumer is
        /// registered as; to be used when discussing this consumer
        /// with the server, for instance with
        /// IModel.BasicCancel().
        /// </summary>
        public string ConsumerTag { get { return this.consumer.ConsumerTag; } }
        #endregion

        /// <summary>
        /// Check if we are in shutdown mode and if so throw an exception.
        /// </summary>
        private void CheckShutdown()
        {
            if (this.shutdown != null)
            {
                throw new Exception(string.Format("Shutdown event occurred. Cause: {0}", this.shutdown));
            }
        }

        /// <summary>Handle the delivery.</summary>
        /// <param name="delivery">The delivery.</param>
        /// <returns>The message.</returns>
        private Message Handle(Delivery delivery)
        {
            if (delivery == null && this.shutdown != null)
            {
                throw new Exception(string.Format("Shutdown event occurred. Cause: {0}", this.shutdown));
            }

            if (delivery == null)
            {
                return null;
            }

            var body = delivery.Body;
            var envelope = delivery.Envelope;

            var messageProperties = this.messagePropertiesConverter.ToMessageProperties(delivery.Properties, envelope, "UTF-8");
            messageProperties.MessageCount = 0;
            var message = new Message(body, messageProperties);
            Logger.Debug(m => m("Received message: {0}", message.ToString()));

            this.deliveryTags.AddOrUpdate(messageProperties.DeliveryTag);
            return message;
        }

        /// <summary>
        /// Main application-side API: wait for the next message delivery and return it.
        /// </summary>
        /// <returns>
        /// The next message.
        /// </returns>
        public Message NextMessage()
        {
            Logger.Trace(m => m("Retrieving delivery for {0}", this));
            return this.Handle(this.queue.Take());
        }

        /// <summary>Main application-side API: wait for the next message delivery and return it.</summary>
        /// <param name="timeout">The timeout.</param>
        /// <returns>The next message.</returns>
        public Message NextMessage(long timeout)
        {
            Logger.Debug(m => m("Retrieving delivery for {0}", this));

            this.CheckShutdown();
            Delivery delivery;
            var result = this.queue.TryTake(out delivery, (int)timeout);
            var message = this.Handle(delivery);
            if (message == null && this.cancelReceived.Value)
            {
                throw new ConsumerCancelledException();
            }

            return message;
        }

        /// <summary>The start.</summary>
        public void Start()
        {
            Logger.Debug(m => m("Starting consumer {0}", this));

            this.channel = ConnectionFactoryUtils.GetTransactionalResourceHolder(this.connectionFactory, this.transactional).Channel;
            this.consumer = new InternalConsumer(this.channel, this);
            this.deliveryTags.Clear();
            this.activeObjectCounter.Add(this);
            var passiveDeclareTries = 3; // mirrored queue might be being moved
            while (passiveDeclareTries > 0)
            {
                passiveDeclareTries--;
                try
                {
                    if (!this.acknowledgeMode.IsAutoAck())
                    {
                        // Set basicQos before calling basicConsume (otherwise if we are not acking the broker
                        // will send blocks of 100 messages)
                        // The Java client includes a convenience method BasicQos(ushort prefetchCount), which sets 0 as the prefetchSize and false as global
                        this.channel.BasicQos(0, (ushort)this.prefetchCount, false);
                    }

                    foreach (var t in this.queues)
                    {
                        this.channel.QueueDeclarePassive(t);
                    }

                    passiveDeclareTries = 0;
                }
                catch (Exception e)
                {
                    if (passiveDeclareTries > 0 && this.channel.IsOpen)
                    {
                        Logger.Warn(m => m("Reconnect failed; retries left=" + (passiveDeclareTries - 1)), e);
                        try
                        {
                            Thread.Sleep(5000);
                        }
                        catch (ThreadInterruptedException e1)
                        {
                            Thread.CurrentThread.Interrupt();
                        }
                    }
                    else
                    {
                        this.activeObjectCounter.Release(this);
                        throw new FatalListenerStartupException("Cannot prepare queue for listener. Either the queue doesn't exist or the broker will not allow us to use it.", e);
                    }
                }
            }

            try
            {
                foreach (var t in this.queues)
                {
                    this.channel.BasicConsume(t, this.acknowledgeMode.IsAutoAck(), this.consumer);
                    Logger.Debug(m => m("Started on queue '{0}': {1}", t, this));
                }
            }
            catch (Exception e)
            {
                throw RabbitUtils.ConvertRabbitAccessException(e);
            }
        }

        /// <summary>
        /// Stop the channel.
        /// </summary>
        public void Stop()
        {
            this.cancelled.LazySet(true);
            if (this.consumer != null && this.consumer.Model != null && this.consumer.ConsumerTag != null && !this.cancelReceived.Value)
            {
                try
                {
                    RabbitUtils.CloseMessageConsumer(this.consumer.Model, this.consumer.ConsumerTag, this.transactional);
                }
                catch (Exception ex)
                {
                    Logger.Debug(m => m("Error closing consumer"), ex);
                }
            }

            Logger.Debug("Closing Rabbit Channel: " + this.channel);
            RabbitUtils.SetPhysicalCloseRequired(true);

            // This one never throws exceptions...
            RabbitUtils.CloseChannel(this.channel);
            this.deliveryTags.Clear();
            this.consumer = null;
        }

        /// <summary>The to string.</summary>
        /// <returns>The System.String.</returns>
        public override string ToString() { return string.Format("Consumer: tag=[{0}], channel={1}, acknowledgeMode={2}, local queue size={3}", this.consumer != null ? this.consumer.ConsumerTag : null, this.channel, this.acknowledgeMode, this.queue.Count); }

        /// <summary>Perform a rollback, handling rollback exceptions properly.</summary>
        /// <param name="ex">The thrown application exception.</param>
        public virtual void RollbackOnExceptionIfNecessary(Exception ex)
        {
            var ackRequired = !this.acknowledgeMode.IsAutoAck() && !this.acknowledgeMode.IsManual();
            try
            {
                if (this.transactional)
                {
                    Logger.Debug(m => m("Initiating transaction rollback on application exception"), ex);
                    RabbitUtils.RollbackIfNecessary(this.channel);
                }

                if (ackRequired)
                {
                    Logger.Debug(m => m("Rejecting message"));

                    var shouldRequeue = this.defaultRequeueRejected;
                    var t = ex;
                    while (shouldRequeue && t != null)
                    {
                        if (t is AmqpRejectAndDontRequeueException)
                        {
                            shouldRequeue = false;
                        }

                        t = t.InnerException;
                    }

                    foreach (var deliveryTag in this.deliveryTags)
                    {
                        // With newer RabbitMQ brokers could use basicNack here...
                        this.channel.BasicReject((ulong)deliveryTag, shouldRequeue);
                    }

                    if (this.transactional)
                    {
                        // Need to commit the reject (=nack)
                        RabbitUtils.CommitIfNecessary(this.channel);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(m => m("Application exception overriden by rollback exception"), ex);
                throw;
            }
            finally
            {
                this.deliveryTags.Clear();
            }
        }

        /// <summary>Perform a commit or message acknowledgement, as appropriate</summary>
        /// <param name="locallyTransacted">if set to <c>true</c> [locally transacted].</param>
        /// <returns>True if committed, else false.</returns>
        public bool CommitIfNecessary(bool locallyTransacted)
        {
            if (this.deliveryTags == null || this.deliveryTags.Count < 1)
            {
                return false;
            }

            try
            {
                var ackRequired = !this.acknowledgeMode.IsAutoAck() && !this.acknowledgeMode.IsManual();

                if (ackRequired)
                {
                    if (this.transactional && !locallyTransacted)
                    {
                        // Not locally transacted but it is transacted so it
                        // could be synchronized with an external transaction
                        foreach (var deliveryTag in this.deliveryTags)
                        {
                            ConnectionFactoryUtils.RegisterDeliveryTag(this.connectionFactory, this.channel, deliveryTag);
                        }
                    }
                    else
                    {
                        if (this.deliveryTags != null && this.deliveryTags.Count > 0)
                        {
                            var copiedTags = new List<long>(this.deliveryTags);
                            if (copiedTags.Count > 0)
                            {
                                var deliveryTag = copiedTags[copiedTags.Count - 1];
                                this.channel.BasicAck((ulong)deliveryTag, true);
                            }
                        }
                    }
                }

                if (locallyTransacted)
                {
                    // For manual acks we still need to commit
                    RabbitUtils.CommitIfNecessary(this.channel);
                }
            }
            finally
            {
                this.deliveryTags.Clear();
            }

            return true;
        }
    }

    /// <summary>
    /// An internal consumer.
    /// </summary>
    internal class InternalConsumer : DefaultBasicConsumer
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The outer blocking queue consumer.
        /// </summary>
        private readonly BlockingQueueConsumer outer;

        /// <summary>Initializes a new instance of the <see cref="InternalConsumer"/> class.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="outer">The outer.</param>
        public InternalConsumer(IModel channel, BlockingQueueConsumer outer) : base(channel) { this.outer = outer; }

        /// <summary>Handle model shutdown, given a consumerTag.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="reason">The reason.</param>
        public override void HandleModelShutdown(IModel channel, ShutdownEventArgs reason)
        {
            Logger.Warn(m => m("Received shutdown signal for consumer tag {0}, cause: {1}", this.ConsumerTag, reason.Cause));

            this.outer.shutdown = reason;

            // The delivery tags will be invalid if the channel shuts down
            this.outer.deliveryTags.Clear();
        }

        /// <summary>The handle basic cancel.</summary>
        /// <param name="consumerTag">The consumer tag.</param>
        public override void HandleBasicCancel(string consumerTag)
        {
            Logger.Warn(m => m("Cancel received"));
            this.outer.cancelReceived.LazySet(true);
        }

        /// <summary>Handle cancel ok.</summary>
        /// <param name="consumerTag">The consumer tag.</param>
        public override void HandleBasicCancelOk(string consumerTag)
        {
            Logger.Debug(m => m("Received cancellation notice for {0}", this.outer.ToString()));

            // Signal to the container that we have been cancelled
            this.outer.activeObjectCounter.Release(this.outer);
        }

        /// <summary>Handle basic deliver.</summary>
        /// <param name="consumerTag">The consumer tag.</param>
        /// <param name="envelope">The envelope.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="body">The body.</param>
        public void HandleBasicDeliver(string consumerTag, BasicGetResult envelope, IBasicProperties properties, byte[] body)
        {
            if (this.outer.cancelled.Value)
            {
                if (this.outer.acknowledgeMode.TransactionAllowed())
                {
                    return;
                }
            }

            Logger.Debug(m => m("Storing delivery for {0}", this.outer.ToString()));

            try
            {
                // N.B. we can't use a bounded queue and offer() here with a timeout
                // in case the connection thread gets blocked
                this.outer.queue.Add(new Delivery(envelope, properties, body));
            }
            catch (ThreadInterruptedException e)
            {
                Thread.CurrentThread.Interrupt();
            }
        }

        /// <summary>Handle basic deliver.</summary>
        /// <param name="consumerTag">The consumer tag.</param>
        /// <param name="deliveryTag">The delivery tag.</param>
        /// <param name="redelivered">The redelivered.</param>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="body">The body.</param>
        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            var envelope = new BasicGetResult(deliveryTag, redelivered, exchange, routingKey, 1, properties, body);
            this.HandleBasicDeliver(consumerTag, envelope, properties, body);
        }
    }

    /// <summary>
    /// Encapsulates an arbitrary message - simple "object" holder structure.
    /// </summary>
    internal class Delivery
    {
        /// <summary>
        /// The envelope.
        /// </summary>
        private readonly BasicGetResult envelope;

        /// <summary>
        /// The properties.
        /// </summary>
        private readonly IBasicProperties properties;

        /// <summary>
        /// The body.
        /// </summary>
        private readonly byte[] body;

        /// <summary>Initializes a new instance of the <see cref="Delivery"/> class.</summary>
        /// <param name="envelope">The envelope.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="body">The body.</param>
        public Delivery(BasicGetResult envelope, IBasicProperties properties, byte[] body)
        {
            this.envelope = envelope;
            this.properties = properties;
            this.body = body;
        }

        /// <summary>
        /// Gets Envelope.
        /// </summary>
        public BasicGetResult Envelope { get { return this.envelope; } }

        /// <summary>
        /// Gets Properties.
        /// </summary>
        public IBasicProperties Properties { get { return this.properties; } }

        /// <summary>
        /// Gets Body.
        /// </summary>
        public byte[] Body { get { return this.body; } }
    }
}
