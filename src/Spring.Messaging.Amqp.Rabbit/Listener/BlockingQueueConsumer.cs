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
using System.Threading;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Threading.AtomicTypes;
using Spring.Threading.Collections.Generic;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    /// Specialized consumer encapsulating knowledge of the broker connections and having its own lifecycle (start and stop).
    /// </summary>
    /// <author>Mark Pollack</author>
    public class BlockingQueueConsumer : DefaultBasicConsumer
    {
        #region Private Fields
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(BlockingQueueConsumer));

        // This must be an unbounded queue or we risk blocking the Connection thread.
        internal readonly IBlockingQueue<Delivery> queue = new LinkedBlockingQueue<Delivery>();

        // When this is non-null the connection has been closed (should never happen in normal operation).
        internal volatile ShutdownEventArgs shutdown;

        private readonly string[] queues;

        private readonly int prefetchCount;

        private readonly int prefetchSize;

        private readonly bool transactional;

        private IModel channel;

        private InternalConsumer consumer;

        internal readonly AtomicBoolean cancelled = new AtomicBoolean(false);

        internal readonly AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode;

        private readonly IConnectionFactory connectionFactory;

        internal readonly ActiveObjectCounter<BlockingQueueConsumer> activeObjectCounter;

        #endregion

        #region Constructors
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockingQueueConsumer"/> class.  Create a consumer. The consumer must not attempt to use the connection factory or communicate with the broker until it is started.
        /// </summary>
        /// <param name="connectionFactory">
        /// The connection factory.
        /// </param>
        /// <param name="activeObjectCounter">
        /// The active object counter.
        /// </param>
        /// <param name="acknowledgeMode">
        /// The acknowledge mode.
        /// </param>
        /// <param name="transactional">
        /// The transactional.
        /// </param>
        /// <param name="prefetchCount">
        /// The prefetch count.
        /// </param>
        /// <param name="queues">
        /// The queues.
        /// </param>
        public BlockingQueueConsumer(IConnectionFactory connectionFactory, ActiveObjectCounter<BlockingQueueConsumer> activeObjectCounter, AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode, bool transactional, int prefetchCount, int prefetchSize, params string[] queues)
        {
            this.connectionFactory = connectionFactory;
            this.activeObjectCounter = activeObjectCounter;
            this.acknowledgeMode = acknowledgeMode;
            this.transactional = transactional;
            this.prefetchCount = prefetchCount;
            this.prefetchSize = prefetchSize;
            this.queues = queues;
        }

        #endregion

        #region Properties

        public IModel Channel
        {
            get { return this.channel; }
        }

        public string ConsumerTag
        {
            get { return this.consumer.ConsumerTag; }
        }
        #endregion

        /// <summary>
        /// Check if we are in shutdown mode and if so throw an exception.
        /// </summary>
        /// <exception cref="Exception">
        /// </exception>
        private void CheckShutdown()
        {
            if (this.shutdown != null)
            {
                throw new Exception(string.Format("Shutdown event occurred. Cause: {0}", this.shutdown.ToString()));
            }
        }

        /// <summary>
        /// Handle the delivery.
        /// </summary>
        /// <param name="delivery">
        /// The delivery.
        /// </param>
        /// <returns>
        /// The message.
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        private Message Handle(Delivery delivery)
        {
            if ((delivery == null && this.shutdown != null))
            {
                throw new Exception(string.Format("Shutdown event occurred. Cause: {0}", this.shutdown.ToString()));
            }

            if (delivery == null)
            {
                return null;
            }

            var body = delivery.Body;
            var envelope = delivery.Envelope;

            var messageProperties = RabbitUtils.CreateMessageProperties(delivery.Properties, envelope, "UTF-8");
            messageProperties.MessageCount = 0;
            var message = new Message(body, messageProperties);
            if (this.logger.IsDebugEnabled)
            {
                this.logger.Debug("Received message: " + message);
            }

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
            this.logger.Trace("Retrieving delivery for " + this);
            return this.Handle(this.queue.Take());
        }

        /// <summary>
        /// Main application-side API: wait for the next message delivery and return it.
        /// </summary>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        /// <returns>
        /// The next message.
        /// </returns>
        public Message NextMessage(TimeSpan timeout)
        {
            if (this.logger.IsDebugEnabled)
            {
                this.logger.Debug("Retrieving delivery for " + this);
            }
            this.CheckShutdown();
            Delivery delivery;
            this.queue.Poll(timeout, out delivery);
            return this.Handle(delivery);
        }

        public void Start()
        {
            this.channel = ConnectionFactoryUtils.GetTransactionalResourceHolder((IConnectionFactory)connectionFactory, transactional).Channel;
            this.consumer = new InternalConsumer(this.channel, this);
            this.activeObjectCounter.Add(this);
            try
            {
                // Set basicQos before calling basicConsume (it is ignored if we are not transactional and the broker will
                // send blocks of 100 messages)
                this.channel.BasicQos((uint)this.prefetchSize, (ushort)this.prefetchCount, false);
                foreach (var t in this.queues)
                {
                    this.channel.QueueDeclarePassive(t);
                }
            }
            catch (Exception e)
            {
                this.activeObjectCounter.Release(this);
                throw new Exception("Cannot prepare queue for listener. " + "Either the queue doesn't exist or the broker will not allow us to use it.", e);
            }

            try
            {
                foreach (var t in this.queues)
                {
                    this.channel.BasicConsume(t, this.acknowledgeMode.IsAutoAck(), this.consumer);
                    if (this.logger.IsDebugEnabled)
                    {
                        this.logger.Debug("Started " + this);
                    }
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
            if (this.consumer != null && this.consumer.Model != null && this.consumer.ConsumerTag != null)
            {
                RabbitUtils.CloseMessageConsumer(this.consumer.Model, this.consumer.ConsumerTag, this.transactional);
            }

            this.logger.Debug("Closing Rabbit Channel: " + this.channel);

            // This one never throws exceptions...
            RabbitUtils.CloseChannel(this.channel);
        }

        public string ToString() 
        {
            return "Consumer: tag=[" + (this.consumer != null ? this.consumer.ConsumerTag : null) + "], channel=" + this.channel + ", acknowledgeMode=" + this.acknowledgeMode + " local queue size=" + this.queue.Count;
        }
    }

    internal class InternalConsumer : DefaultBasicConsumer
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(InternalConsumer));

        /// <summary>
        /// The outer blocking queue consumer.
        /// </summary>
        private readonly BlockingQueueConsumer outer;

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalConsumer"/> class.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="outer">
        /// The outer.
        /// </param>
        public InternalConsumer(IModel channel, BlockingQueueConsumer outer) : base(channel)
        {
            this.outer = outer;
        }

        /// <summary>
        /// Handle model shutdown, given a consumerTag.
        /// </summary>
        /// <param name="consumerTag">
        /// The consumer tag.
        /// </param>
        /// <param name="sig">
        /// The sig.
        /// </param>
        public void HandleModelShutdown(string consumerTag, ShutdownEventArgs sig)
        {
            if (this.logger.IsDebugEnabled)
            {
                logger.Debug("Received shutdown signal for consumer tag=" + consumerTag + " , cause=" + sig.Cause);
            }
            outer.shutdown = sig;
        }

        /// <summary>
        /// Handle cancel ok.
        /// </summary>
        /// <param name="consumerTag">
        /// The consumer tag.
        /// </param>
        public override void HandleBasicCancelOk(string consumerTag)
        {
            if (this.logger.IsDebugEnabled)
            {
                this.logger.Debug("Received cancellation notice for " + this.outer.ToString());
            }
            // Signal to the container that we have been cancelled
            this.outer.activeObjectCounter.Release(this.outer);
        }

        /// <summary>
        /// Handle basic deliver.
        /// </summary>
        /// <param name="consumerTag">
        /// The consumer tag.
        /// </param>
        /// <param name="envelope">
        /// The envelope.
        /// </param>
        /// <param name="properties">
        /// The properties.
        /// </param>
        /// <param name="body">
        /// The body.
        /// </param>
        public void HandleBasicDeliver(string consumerTag, BasicGetResult envelope, IBasicProperties properties, byte[] body)
        {
            if (this.outer.cancelled)
            {
                if (this.outer.acknowledgeMode.TransactionAllowed())
                {
                    return;
                }
            }
            if (this.logger.IsDebugEnabled)
            {
                this.logger.Debug("Storing delivery for " + this.outer.ToString());
            }
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

        /// <summary>
        /// Handle basic deliver.
        /// </summary>
        /// <param name="consumerTag">
        /// The consumer tag.
        /// </param>
        /// <param name="deliveryTag">
        /// The delivery tag.
        /// </param>
        /// <param name="redelivered">
        /// The redelivered.
        /// </param>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="properties">
        /// The properties.
        /// </param>
        /// <param name="body">
        /// The body.
        /// </param>
        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            // TODO: Validate that 1 is the right message count.
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

        /// <summary>
        /// Initializes a new instance of the <see cref="Delivery"/> class.
        /// </summary>
        /// <param name="envelope">
        /// The envelope.
        /// </param>
        /// <param name="properties">
        /// The properties.
        /// </param>
        /// <param name="body">
        /// The body.
        /// </param>
        public Delivery(BasicGetResult envelope, IBasicProperties properties, byte[] body)
        {
            this.envelope = envelope;
            this.properties = properties;
            this.body = body;
        }

        /// <summary>
        /// Gets Envelope.
        /// </summary>
        public BasicGetResult Envelope
        {
            get { return this.envelope; }
        }

        /// <summary>
        /// Gets Properties.
        /// </summary>
        public IBasicProperties Properties
        {
            get { return this.properties; }
        }

        /// <summary>
        /// Gets Body.
        /// </summary>
        public byte[] Body
        {
            get { return this.body; }
        }
    }
}