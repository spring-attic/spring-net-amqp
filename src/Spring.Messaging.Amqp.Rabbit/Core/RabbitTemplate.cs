
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
using Spring.Collections.Generic;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Threading.Collections.Generic;
using Spring.Util;
using IConnection = Spring.Messaging.Amqp.Rabbit.Connection.IConnection;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// Helper class that simplifies synchronous RabbitMQ access code. 
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald</author>
    public class RabbitTemplate : RabbitAccessor, IRabbitOperations
    {
        /// <summary>
        /// The logger.
        /// </summary>
        protected static readonly ILog logger = LogManager.GetLogger(typeof(RabbitTemplate));

        /// <summary>
        /// The default exchange.
        /// </summary>
        private static readonly string DEFAULT_EXCHANGE = string.Empty; // alias for amq.direct default exchange

        /// <summary>
        /// The default routing key.
        /// </summary>
        private static readonly string DEFAULT_ROUTING_KEY = string.Empty;

        /// <summary>
        /// The default reply timeout.
        /// </summary>
        private static readonly long DEFAULT_REPLY_TIMEOUT = 5000;

        /// <summary>
        /// The default encoding.
        /// </summary>
        private static readonly string DEFAULT_ENCODING = "UTF-8";

        #region Fields

        /// <summary>
        /// The exchange
        /// </summary>
        private string exchange = DEFAULT_EXCHANGE;

        /// <summary>
        /// The routing key.
        /// </summary>
        private string routingKey = DEFAULT_ROUTING_KEY;

        /// <summary>
        /// The default queue name that will be used for synchronous receives.
        /// </summary>
        private string queue;

        /// <summary>
        /// The reply timeout.
        /// </summary>
        private long replyTimeout = DEFAULT_REPLY_TIMEOUT;

        /// <summary>
        /// The message converter.
        /// </summary>
        private IMessageConverter messageConverter = new SimpleMessageConverter();

        /// <summary>
        /// The encoding.
        /// </summary>
        private string encoding = DEFAULT_ENCODING;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitTemplate"/> class.
        /// </summary>
        public RabbitTemplate()
        {
            InitDefaultStrategies();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitTemplate"/> class.
        /// </summary>
        /// <param name="connectionFactory">
        /// The connection factory.
        /// </param>
        public RabbitTemplate(IConnectionFactory connectionFactory) : this()
        {
            ConnectionFactory = connectionFactory;
            AfterPropertiesSet();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Sets Queue.
        /// </summary>
        public string Queue
        {
            set { this.queue = value; }
        }

        /// <summary>
        /// Sets Exchange.
        /// </summary>
        public string Exchange
        {
            set { this.exchange = value; }
        }

        /// <summary>
        /// Sets RoutingKey.
        /// </summary>
        public string RoutingKey
        {
            set { this.routingKey = value; }
        }

        /// <summary>
        /// Sets Encoding.
        /// </summary>
        public string Encoding
        {
            set { this.encoding = value; }
        }

        /// <summary>
        /// Gets or sets MessageConverter.
        /// </summary>
        public IMessageConverter MessageConverter
        {
            get { return this.messageConverter; }
            set { this.messageConverter = value; }
        }

        #endregion

        #region Implementation of IAmqpTemplate

        /// <summary>
        /// Send a message, given the message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void Send(Message message)
        {
            Send(this.exchange, this.routingKey, message);
        }

        /// <summary>
        /// Send a message, given a routing key and the message.
        /// </summary>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public void Send(string routingKey, Message message)
        {
            Send(this.exchange, routingKey, message);
        }

        /// <summary>
        /// Send a message, given an exchange, a routing key, and the message.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public void Send(string exchange, string routingKey, Message message)
        {
            Execute<object>(channel =>
            {
                DoSend(channel, exchange, routingKey, message);
                return null;
            });
        }

        /// <summary>
        /// Convert and send a message, given the message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void ConvertAndSend(object message)
        {
            this.ConvertAndSend(this.exchange, this.routingKey, message);
        }

        /// <summary>
        /// Convert and send a message, given a routing key and the message.
        /// </summary>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public void ConvertAndSend(string routingKey, object message)
        {
            this.ConvertAndSend(this.exchange, routingKey, message);
        }

        /// <summary>
        /// Convert and send a message, given an exchange, a routing key, and the message.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public void ConvertAndSend(string exchange, string routingKey, object message)
        {
            this.Send(exchange, routingKey, Execute<Message>(channel => this.GetRequiredMessageConverter().ToMessage(message, new MessageProperties())));
        }

        /// <summary>
        /// Receive a message.
        /// </summary>
        /// <returns>
        /// The message.
        /// </returns>
        public Message Receive()
        {
            return this.Receive(this.GetRequiredQueue());
        }

        /// <summary>
        /// Receive a message, given the name of a queue.
        /// </summary>
        /// <param name="queueName">
        /// The queue name.
        /// </param>
        /// <returns>
        /// The message.
        /// </returns>
        public Message Receive(string queueName)
        {
            return Execute<Message>(channel =>
                                         {
                                             var response = channel.BasicGet(queueName, !IsChannelTransacted);

                                             // Response can be null is the case that there is no message on the queue.
                                             if (response != null)
                                             {
                                                 var deliveryTag = response.DeliveryTag;
                                                 if (ChannelLocallyTransacted(channel))
                                                 {
                                                     channel.BasicAck(deliveryTag, false);
                                                     channel.TxCommit();
                                                 }
                                                 else if (IsChannelTransacted)
                                                 {
                                                     // Not locally transacted but it is transacted so it
                                                     // could be synchronized with an external transaction
                                                     ConnectionFactoryUtils.RegisterDeliveryTag(ConnectionFactory, channel, (long)deliveryTag);
                                                 }

                                                 var messageProps = RabbitUtils.CreateMessageProperties(response.BasicProperties, response, encoding);
                                                 messageProps.MessageCount = (int)response.MessageCount;
                                                 return new Message(response.Body, messageProps);
                                             }

                                             return null;
                                         });
        }

        /// <summary>
        /// Receive and convert a message.
        /// </summary>
        /// <returns>
        /// The object.
        /// </returns>
        public object ReceiveAndConvert()
        {
            return ReceiveAndConvert(this.GetRequiredQueue());
        }

        /// <summary>
        /// Receive and covert a message, given the name of a queue.
        /// </summary>
        /// <param name="queueName">
        /// The queue name.
        /// </param>
        /// <returns>
        /// The object.
        /// </returns>
        public object ReceiveAndConvert(string queueName)
        {
            var response = Receive(queueName);
            if (response != null)
            {
                return this.GetRequiredMessageConverter().FromMessage(response);
            }

            return null;
        }

        /// <summary>
        /// Send and receive a message, given the message.
        /// </summary>
        /// <param name="message">
        /// The message to send.
        /// </param>
        /// <returns>
        /// The message received.
        /// </returns>
        public Message SendAndReceive(Message message)
        {
            return this.DoSendAndReceive(this.exchange, this.routingKey, message);
        }

        /// <summary>
        /// Send and receive a message, given a routing key and the message.
        /// </summary>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message to send.
        /// </param>
        /// <returns>
        /// The message received.
        /// </returns>
        public Message SendAndReceive(string routingKey, Message message)
        {
            return this.DoSendAndReceive(this.exchange, routingKey, message);
        }

        /// <summary>
        /// Send and receive a message, given an exchange, a routing key, and the message.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message to send.
        /// </param>
        /// <returns>
        /// The message received.
        /// </returns>
        public Message SendAndReceive(string exchange, string routingKey, Message message)
        {
            return this.DoSendAndReceive(exchange, routingKey, message);
        }

        /// <summary>
        /// Convert, send, and receive a message, given the message.
        /// </summary>
        /// <param name="message">
        /// The message to send.
        /// </param>
        /// <returns>
        /// The message received.
        /// </returns>
        public object ConvertSendAndReceive(object message)
        {
            return this.ConvertSendAndReceive(this.exchange, this.routingKey, message);
        }

        /// <summary>
        /// Convert, send, and receive a message, given a routing key and the message.
        /// </summary>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message to send.
        /// </param>
        /// <returns>
        /// The message received.
        /// </returns>
        public object ConvertSendAndReceive(string routingKey, object message)
        {
            return this.ConvertSendAndReceive(this.exchange, routingKey, message);
        }

        /// <summary>
        /// Convert, send, and receive a message, given an exchange, a routing key and the message.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message to send.
        /// </param>
        /// <returns>
        /// The message received.
        /// </returns>
        public object ConvertSendAndReceive(string exchange, string routingKey, object message)
        {
            var messageProperties = new MessageProperties();
            var requestMessage = this.GetRequiredMessageConverter().ToMessage(message, messageProperties);
            var replyMessage = this.DoSendAndReceive(exchange, routingKey, requestMessage);
            if (replyMessage == null)
            {
                return null;
            }

            return this.GetRequiredMessageConverter().FromMessage(replyMessage);
        }

        /// <summary>
        /// Do the send and receive operation, given an exchange, a routing key and the message.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message to send.
        /// </param>
        /// <returns>
        /// The message received.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        private Message DoSendAndReceive(string exchange, string routingKey, Message message)
        {
            var replyMessage = this.Execute<Message>(delegate(IModel channel)
                                                             {
                                                                 var replyHandoff = new SynchronousQueue<Message>();

                                                                 AssertUtils.IsTrue(message.MessageProperties.ReplyTo == null, "Send-and-receive methods can only be used if the Message does not already have a replyTo property.");
                                                                 var queueDeclaration = channel.QueueDeclare();
                                                                 var replyToAddress = new Address(ExchangeTypes.Direct, DEFAULT_EXCHANGE, queueDeclaration);
                                                                 message.MessageProperties.ReplyTo = replyToAddress;

                                                                 var noAck = false;
                                                                 var consumerTag = Guid.NewGuid().ToString();
                                                                 var noLocal = true;
                                                                 var exclusive = true;
                                                                 var consumer = new AdminDefaultBasicConsumer(channel, replyHandoff, this.encoding);
                                                                 channel.BasicConsume(replyToAddress.RoutingKey, noAck, consumerTag, noLocal, exclusive, null, consumer);
                                                                 DoSend(channel, exchange, routingKey, message);
                                                                 Message reply;

                                                                 if (this.replyTimeout < 0)
                                                                 {
                                                                     reply = replyHandoff.Take();
                                                                 }
                                                                 else
                                                                 {
                                                                     replyHandoff.Poll(new TimeSpan(0, 0, 0, 0, (int)this.replyTimeout), out reply);
                                                                 }

                                                                 channel.BasicCancel(consumerTag);
                                                                 return reply;
                                                             });

            return replyMessage;
        }
        #endregion

        #region Implementation of IRabbitOperations
        
        /// <summary>
        /// Execute an action.
        /// </summary>
        /// <param name="action">
        /// The action.
        /// </param>
        /// <typeparam name="T">
        /// Type T
        /// </typeparam>
        /// <returns>
        /// An object of Type T
        /// </returns>
        /// <exception cref="SystemException">
        /// </exception>
        public T Execute<T>(ChannelCallbackDelegate<T> action)
        {
            AssertUtils.ArgumentNotNull(action, "Callback object must not be null");
            var resourceHolder = GetTransactionalResourceHolder();
            var channel = resourceHolder.Channel;

            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Executing callback on RabbitMQ Channel: " + channel);
                }

                return action(channel);
            }
            catch (Exception ex)
            {
                if (this.ChannelLocallyTransacted(channel))
                {
                    resourceHolder.RollbackAll();
                }
                throw ConvertRabbitAccessException(ex);
            }
            finally
            {
                ConnectionFactoryUtils.ReleaseResources(resourceHolder);
            }
        }

        /// <summary>
        /// Execute an action.
        /// </summary>
        /// <param name="action">
        /// The action.
        /// </param>
        /// <typeparam name="T">
        /// Type T
        /// </typeparam>
        /// <returns>
        /// An object of Type T
        /// </returns>
        public T Execute<T>(IChannelCallback<T> action)
        {
            return Execute<T>(action.DoInRabbit);
        }

        #endregion

        /// <summary>
        /// Initialize with default strategies.
        /// </summary>
        protected virtual void InitDefaultStrategies()
        {
            this.MessageConverter = new SimpleMessageConverter();
        }

        /// <summary>
        /// Get the connection from a resource holder.
        /// </summary>
        /// <param name="resourceHolder">
        /// The resource holder.
        /// </param>
        /// <returns>
        /// The connection.
        /// </returns>
        protected new virtual IConnection GetConnection(RabbitResourceHolder resourceHolder)
        {
            return resourceHolder.Connection;
        }

        /// <summary>
        /// Get the channel from a resource holder.
        /// </summary>
        /// <param name="resourceHolder">
        /// The resource holder.
        /// </param>
        /// <returns>
        /// The channel.
        /// </returns>
        protected new virtual RabbitMQ.Client.IModel GetChannel(RabbitResourceHolder resourceHolder)
        {
            return resourceHolder.Channel;
        }

        /// <summary>
        /// Do the send operation.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        private void DoSend(RabbitMQ.Client.IModel channel, string exchange, string routingKey, Message message)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug("Publishing message on exchange [" + exchange + "], routingKey = [" + routingKey + "]");
            }

            if (exchange == null)
            {
                // try to send to configured exchange
                exchange = this.exchange;
            }

            if (routingKey == null)
            {
                // try to send to configured routing key
                routingKey = this.routingKey;
            }

            channel.BasicPublish(exchange, routingKey, false, false, RabbitUtils.ExtractBasicProperties(channel, message, this.encoding), message.Body);

            // Check commit - avoid commit call within a JTA transaction.
            if (this.ChannelLocallyTransacted(channel))
            {
                // Transacted channel created by this template -> commit.
                RabbitUtils.CommitIfNecessary(channel);
            }
        }

        /// <summary>
        /// Flag indicating whether the channel is locally transacted.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <returns>
        /// True if locally transacted, else false.
        /// </returns>
        protected bool ChannelLocallyTransacted(RabbitMQ.Client.IModel channel)
        {
            return IsChannelTransacted && !ConnectionFactoryUtils.IsChannelTransactional(channel, ConnectionFactory);
        }

        /// <summary>
        /// Get the required message converter.
        /// </summary>
        /// <returns>
        /// The message converter.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        private IMessageConverter GetRequiredMessageConverter()
        {
            var converter = this.MessageConverter;
            if (converter == null)
            {
                throw new InvalidOperationException("No 'messageConverter' specified. Check configuration of RabbitTemplate.");
            }

            return converter;
        }

        /// <summary>
        /// Get the required queue.
        /// </summary>
        /// <returns>
        /// The name of the queue.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        private string GetRequiredQueue()
        {
            var name = this.queue;
            if (name == null)
            {
                throw new InvalidOperationException("No 'queue' specified. Check configuration of RabbitTemplate.");
            }

            return name;
        }
    }

    /// <summary>
    /// The admin default basic consumer.
    /// </summary>
    internal class AdminDefaultBasicConsumer : DefaultBasicConsumer
    {
        /// <summary>
        /// The reply handoff.
        /// </summary>
        private readonly SynchronousQueue<Message> replyHandoff;

        /// <summary>
        /// The encoding.
        /// </summary>
        private readonly string encoding;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminDefaultBasicConsumer"/> class.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="replyHandoff">
        /// The reply handoff.
        /// </param>
        /// <param name="encoding">
        /// The encoding.
        /// </param>
        public AdminDefaultBasicConsumer(IModel channel, SynchronousQueue<Message> replyHandoff, string encoding) : base(channel)
        {
            this.replyHandoff = replyHandoff;
            this.encoding = encoding;
        }

        /// <summary>
        /// Handle delivery.
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
        public void HandleDelivery(string consumerTag, BasicGetResult envelope, IBasicProperties properties, byte[] body)
        {
            var messageProperties = RabbitUtils.CreateMessageProperties(properties, envelope, this.encoding);
            var reply = new Message(body, messageProperties);
            try
            {
                this.replyHandoff.Put(reply);
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
            var envelope = new BasicGetResult(deliveryTag, redelivered, exchange, routingKey, 1, properties, body);
            this.HandleDelivery(consumerTag, envelope, properties, body);
        }
    }
}