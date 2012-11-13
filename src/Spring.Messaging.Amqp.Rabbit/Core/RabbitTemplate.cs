// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTemplate.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// <para>
    /// Helper class that simplifies synchronous RabbitMQ access (sending and receiving messages).
    /// </para>
    /// <para>
    /// The default settings are for non-transactional messaging, which reduces the amount of data exchanged with the broker.
    /// To use a new transaction for every send or receive set the {@link #setChannelTransacted(boolean) channelTransacted}
    /// flag. To extend the transaction over multiple invocations (more efficient), you can use a Spring transaction to
    /// bracket the calls (with <code>channelTransacted=true</code> as well).
    /// </para>
    /// <para>
    /// The only mandatory property is the {@link #setConnectionFactory(ConnectionFactory) ConnectionFactory}. There are
    /// strategies available for converting messages to and from Java objects (
    /// {@link #setMessageConverter(MessageConverter) MessageConverter}) and for converting message headers (known as message
    /// properties in AMQP, see {@link #setMessagePropertiesConverter(MessagePropertiesConverter) MessagePropertiesConverter}
    /// ). The defaults probably do something sensible for typical use cases, as long as the message content-type is set
    /// appropriately.
    /// </para>
    /// <para>
    /// The "send" methods all have overloaded versions that allow you to explicitly target an exchange and a routing key, or
    /// you can set default values to be used in all send operations. The plain "receive" methods allow you to explicitly
    /// target a queue to receive from, or you can set a default value for the template that applies to all explicit
    /// receives. The convenience methods for send <b>and</b> receive use the sender defaults if no exchange or routing key
    /// is specified, but they always use a temporary queue for the receive leg, so the default queue is ignored.
    /// </para>
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Mark Fisher</author>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class RabbitTemplate : RabbitAccessor, IRabbitOperations, IMessageListener, IPublisherCallbackChannelListener
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        protected new static readonly ILog Logger = LogManager.GetCurrentClassLogger();

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
        private volatile string exchange = DEFAULT_EXCHANGE;

        /// <summary>
        /// The routing key.
        /// </summary>
        private volatile string routingKey = DEFAULT_ROUTING_KEY;

        /// <summary>
        /// The default queue name that will be used for synchronous receives.
        /// </summary>
        private volatile string queue;

        /// <summary>
        /// The reply timeout.
        /// </summary>
        private long replyTimeout = DEFAULT_REPLY_TIMEOUT;

        /// <summary>
        /// The message converter.
        /// </summary>
        private volatile IMessageConverter messageConverter = new SimpleMessageConverter();

        /// <summary>
        /// The message properties converter.
        /// </summary>
        private volatile IMessagePropertiesConverter messagePropertiesConverter = new DefaultMessagePropertiesConverter();

        /// <summary>
        /// The encoding.
        /// </summary>
        private volatile string encoding = DEFAULT_ENCODING;

        private volatile Queue replyQueue;

        private readonly IDictionary<string, BlockingCollection<Message>> replyHolder = new ConcurrentDictionary<string, BlockingCollection<Message>>();

        private volatile IConfirmCallback confirmCallback;

        private volatile IReturnCallback returnCallback;

        private readonly IDictionary<object, SortedDictionary<long, PendingConfirm>> pendingConfirms = new ConcurrentDictionary<object, SortedDictionary<long, PendingConfirm>>();

        private volatile bool mandatory;

        private volatile bool immediate;

        private readonly string uuid = Guid.NewGuid().ToString();

        public static readonly string STACKED_CORRELATION_HEADER = "spring_reply_correlation";

        public static readonly string STACKED_REPLY_TO_HEADER = "spring_reply_to";
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitTemplate"/> class. 
        /// Convenient constructor for use with setter injection. Don't forget to set the connection factory.
        /// </summary>
        public RabbitTemplate() { this.InitDefaultStrategies(); }

        /// <summary>Initializes a new instance of the <see cref="RabbitTemplate"/> class.
        /// Create a rabbit template with default strategies and settings.</summary>
        /// <param name="connectionFactory">The connection factory to use.</param>
        public RabbitTemplate(IConnectionFactory connectionFactory)
            : this()
        {
            this.ConnectionFactory = connectionFactory;
            this.AfterPropertiesSet();
        }

        #endregion

        /// <summary>
        /// Set up the default strategies. Subclasses can override if necessary.
        /// </summary>
        protected virtual void InitDefaultStrategies() { this.MessageConverter = new SimpleMessageConverter(); }

        #region Properties

        /// <summary>
        /// Sets Exchange. The name of the default exchange to use for send operations when none is specified. Defaults to <code>""</code>
        /// which is the default exchange in the broker (per the AMQP specification).
        /// </summary>
        /// <value>The exchange.</value>
        public string Exchange { set { this.exchange = value; } }

        /// <summary>
        /// Sets RoutingKey. The value of a default routing key to use for send operations when none is specified. Default is empty which is
        /// not helpful when using the default (or any direct) exchange, but fine if the exchange is a headers exchange for
        /// instance.
        /// </summary>
        /// <value>The routing key.</value>
        public string RoutingKey { set { this.routingKey = value; } }

        /// <summary>
        /// Sets Queue. The name of the default queue to receive messages from when none is specified explicitly.
        /// </summary>
        /// <value>The queue.</value>
        public string Queue { set { this.queue = value; } }

        /// <summary>
        /// Sets Encoding. The encoding to use when inter-converting between byte arrays and Strings in message properties.
        /// </summary>
        /// <value>The encoding.</value>
        public string Encoding { set { this.encoding = value; } }

        /// <summary>
        /// A queue for replies; if not provided, a temporary exclusive, auto-delete queue will be used for each reply.
        /// </summary>
        public Queue ReplyQueue { set { this.replyQueue = value; } }

        /// <summary>
        /// Sets the reply timeout. Specify the timeout in milliseconds to be used when waiting for a reply Message when using one of the
        /// sendAndReceive methods. The default value is defined as {@link #DEFAULT_REPLY_TIMEOUT}. A negative value
        /// indicates an indefinite timeout. Not used in the plain receive methods because there is no blocking receive
        /// operation defined in the protocol.
        /// </summary>
        /// <value>The reply timeout.</value>
        public long ReplyTimeout { set { this.replyTimeout = value; } }

        /// <summary>Gets or sets the message converter.</summary>
        public IMessageConverter MessageConverter { get { return this.messageConverter; } set { this.messageConverter = value; } }

        /// <summary>
        /// Sets the message properties converter.
        /// </summary>
        /// <value>The message properties converter.</value>
        public IMessagePropertiesConverter MessagePropertiesConverter
        {
            set
            {
                AssertUtils.ArgumentNotNull(value, "messagePropertiesConverter must not be null");
                this.messagePropertiesConverter = value;
            }
        }

        /// <summary>Sets the confirm callback.</summary>
        public IConfirmCallback ConfirmCallback { set { this.confirmCallback = value; } }

        /// <summary>Sets the return callback.</summary>
        public IReturnCallback ReturnCallback { set { this.returnCallback = value; } }

        /// <summary>Sets a value indicating whether mandatory.</summary>
        public bool Mandatory { set { this.mandatory = value; } }

        /// <summary>Sets a value indicating whether immediate.</summary>
        public bool Immediate { set { this.immediate = value; } }

        /// <summary>Gets unconfirmed correlation data older than age and removes them.</summary>
        /// <param name="age">Age in millseconds</param>
        /// <returns>The collection of correlation data for which confirms have not been received.</returns>
        public HashSet<CorrelationData> GetUnconfirmed(long age)
        {
            var unconfirmed = new HashSet<CorrelationData>();
            lock (this.pendingConfirms)
            {
                var threshold = DateTime.UtcNow.ToMilliseconds() - age;
                foreach (var channelPendingConfirmEntry in this.pendingConfirms)
                {
                    var channelPendingConfirms = channelPendingConfirmEntry.Value;

                    PendingConfirm pendingConfirm;
                    var itemsToRemove = new Dictionary<long, PendingConfirm>();
                    foreach (var item in channelPendingConfirms)
                    {
                        pendingConfirm = item.Value;
                        if (pendingConfirm.Timestamp < threshold)
                        {
                            unconfirmed.Add(pendingConfirm.CorrelationData);
                            itemsToRemove.Add(item.Key, item.Value);
                        }
                        else
                        {
                            break;
                        }
                    }

                    foreach (var item in itemsToRemove)
                    {
                        channelPendingConfirms.Remove(item.Key);
                    }
                }
            }

            return unconfirmed.Count > 0 ? unconfirmed : null;
        }

        #endregion

        #region Implementation of IAmqpTemplate

        /// <summary>Send a message, given the message.</summary>
        /// <param name="message">The message.</param>
        public void Send(Message message) { this.Send(this.exchange, this.routingKey, message); }

        /// <summary>Send a message, given a routing key and the message.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        public void Send(string routingKey, Message message) { this.Send(this.exchange, routingKey, message); }

        /// <summary>The send.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        public void Send(string exchange, string routingKey, Message message) { this.Send(exchange, routingKey, message, null); }

        /// <summary>Send a message, given an exchange, a routing key, and the message.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="correlationData">The correlation Data.</param>
        public void Send(string exchange, string routingKey, Message message, CorrelationData correlationData)
        {
            this.Execute<object>(
                channel =>
                {
                    this.DoSend(channel, exchange, routingKey, message, correlationData);
                    return null;
                });
        }

        /// <summary>Convert and send a message, given the message.</summary>
        /// <param name="message">The message.</param>
        public void ConvertAndSend(object message) { this.ConvertAndSend(this.exchange, this.routingKey, message, (CorrelationData)null); }

        /// <summary>The convert and send.</summary>
        /// <param name="message">The message.</param>
        /// <param name="correlationData">The correlation data.</param>
        public void ConvertAndSend(object message, CorrelationData correlationData) { this.ConvertAndSend(this.exchange, this.routingKey, message, correlationData); }

        /// <summary>Convert and send a message, given a routing key and the message.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        public void ConvertAndSend(string routingKey, object message) { this.ConvertAndSend(this.exchange, routingKey, message, (CorrelationData)null); }

        /// <summary>The convert and send.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="correlationData">The correlation data.</param>
        public void ConvertAndSend(string routingKey, object message, CorrelationData correlationData) { this.ConvertAndSend(this.exchange, routingKey, message, correlationData); }

        /// <summary>Convert and send a message, given an exchange, a routing key, and the message.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        public void ConvertAndSend(string exchange, string routingKey, object message) { this.ConvertAndSend(exchange, routingKey, message, (CorrelationData)null); }

        /// <summary>The convert and send.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="correlationData">The correlation data.</param>
        public void ConvertAndSend(string exchange, string routingKey, object message, CorrelationData correlationData) { this.Send(exchange, routingKey, this.ConvertMessageIfNecessary(message), correlationData); }

        /// <summary>Convert and send a message, given the message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        public void ConvertAndSend(object message, Func<Message, Message> messagePostProcessor) { this.ConvertAndSend(this.exchange, this.routingKey, message, messagePostProcessor, null); }

        /// <summary>The convert and send.</summary>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <param name="correlationData">The correlation data.</param>
        public void ConvertAndSend(object message, Func<Message, Message> messagePostProcessor, CorrelationData correlationData) { this.ConvertAndSend(this.exchange, this.routingKey, message, messagePostProcessor, correlationData); }

        /// <summary>Convert and send a message, given the message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        public void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor) { this.ConvertAndSend(this.exchange, this.routingKey, message, messagePostProcessor, null); }

        /// <summary>The convert and send.</summary>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <param name="correlationData">The correlation data.</param>
        public void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData) { this.ConvertAndSend(this.exchange, this.routingKey, message, messagePostProcessor, correlationData); }

        /// <summary>Convert and send a message, given a routing key and the message.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        public void ConvertAndSend(string routingKey, object message, Func<Message, Message> messagePostProcessor) { this.ConvertAndSend(this.exchange, routingKey, message, messagePostProcessor, null); }

        /// <summary>The convert and send.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <param name="correlationData">The correlation data.</param>
        public void ConvertAndSend(string routingKey, object message, Func<Message, Message> messagePostProcessor, CorrelationData correlationData) { this.ConvertAndSend(this.exchange, routingKey, message, messagePostProcessor, correlationData); }

        /// <summary>Convert and send a message, given a routing key and the message.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        public void ConvertAndSend(string routingKey, object message, IMessagePostProcessor messagePostProcessor) { this.ConvertAndSend(this.exchange, routingKey, message, messagePostProcessor, null); }

        /// <summary>The convert and send.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <param name="correlationData">The correlation data.</param>
        public void ConvertAndSend(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData) { this.ConvertAndSend(this.exchange, routingKey, message, messagePostProcessor, correlationData); }

        /// <summary>The convert and send.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        public void ConvertAndSend(string exchange, string routingKey, object message, Func<Message, Message> messagePostProcessor) { this.ConvertAndSend(exchange, routingKey, message, messagePostProcessor, null); }

        /// <summary>Convert and send a message, given an exchange, a routing key, and the message.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <param name="correlationData">The correlation Data.</param>
        public void ConvertAndSend(string exchange, string routingKey, object message, Func<Message, Message> messagePostProcessor, CorrelationData correlationData)
        {
            var messageToSend = this.ConvertMessageIfNecessary(message);
            messageToSend = messagePostProcessor.Invoke(messageToSend);
            this.Send(exchange, routingKey, this.Execute(channel => messageToSend), correlationData);
        }

        /// <summary>The convert and send.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        public void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor) { this.ConvertAndSend(exchange, routingKey, message, messagePostProcessor, null); }

        /// <summary>Convert and send a message, given an exchange, a routing key, and the message.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <param name="correlationData">The correlation Data.</param>
        public void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            var messageToSend = this.ConvertMessageIfNecessary(message);
            messageToSend = messagePostProcessor.PostProcessMessage(messageToSend);
            this.Send(exchange, routingKey, this.Execute(channel => messageToSend), correlationData);
        }

        /// <summary>
        /// Receive a message.
        /// </summary>
        /// <returns>The message.</returns>
        public Message Receive() { return this.Receive(this.GetRequiredQueue()); }

        /// <summary>Receive a message, given the name of a queue.</summary>
        /// <param name="queueName">The queue name.</param>
        /// <returns>The message.</returns>
        public Message Receive(string queueName)
        {
            return this.Execute(
                channel =>
                {
                    var response = channel.BasicGet(queueName, !this.ChannelTransacted);

                    // Response can be null is the case that there is no message on the queue.
                    if (response != null)
                    {
                        var deliveryTag = response.DeliveryTag;
                        if (this.ChannelLocallyTransacted(channel))
                        {
                            channel.BasicAck(deliveryTag, false);
                            channel.TxCommit();
                        }
                        else if (this.ChannelTransacted)
                        {
                            // Not locally transacted but it is transacted so it
                            // could be synchronized with an external transaction
                            ConnectionFactoryUtils.RegisterDeliveryTag(
                                this.ConnectionFactory, channel, (long)deliveryTag);
                        }

                        var messageProps =
                            this.messagePropertiesConverter.ToMessageProperties(
                                response.BasicProperties, response, this.encoding);
                        messageProps.MessageCount = (int)response.MessageCount;
                        return new Message(response.Body, messageProps);
                    }

                    return null;
                });
        }

        /// <summary>
        /// Receive and convert a message.
        /// </summary>
        /// <returns>The object.</returns>
        public object ReceiveAndConvert() { return this.ReceiveAndConvert(this.GetRequiredQueue()); }

        /// <summary>Receive and covert a message, given the name of a queue.</summary>
        /// <param name="queueName">The queue name.</param>
        /// <returns>The object.</returns>
        public object ReceiveAndConvert(string queueName)
        {
            var response = this.Receive(queueName);
            if (response != null)
            {
                return this.GetRequiredMessageConverter().FromMessage(response);
            }

            return null;
        }

        /// <summary>Send and receive a message, given the message.</summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The message received.</returns>
        public Message SendAndReceive(Message message) { return this.DoSendAndReceive(this.exchange, this.routingKey, message); }

        /// <summary>Send and receive a message, given a routing key and the message.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The message received.</returns>
        public Message SendAndReceive(string routingKey, Message message) { return this.DoSendAndReceive(this.exchange, routingKey, message); }

        /// <summary>Send and receive a message, given an exchange, a routing key, and the message.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The message received.</returns>
        public Message SendAndReceive(string exchange, string routingKey, Message message) { return this.DoSendAndReceive(exchange, routingKey, message); }

        /// <summary>Convert, send, and receive a message, given the message.</summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The message received.</returns>
        public object ConvertSendAndReceive(object message) { return this.ConvertSendAndReceive(this.exchange, this.routingKey, message, default(IMessagePostProcessor)); }

        /// <summary>Convert, send, and receive a message, given a routing key and the message.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The message received.</returns>
        public object ConvertSendAndReceive(string routingKey, object message) { return this.ConvertSendAndReceive(this.exchange, routingKey, message, default(IMessagePostProcessor)); }

        /// <summary>Convert, send, and receive a message, given an exchange, a routing key and the message.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The message received.</returns>
        public object ConvertSendAndReceive(string exchange, string routingKey, object message) { return this.ConvertSendAndReceive(exchange, routingKey, message, default(IMessagePostProcessor)); }

        /// <summary>Convert, send, and receive a message, given the message.</summary>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The message received.</returns>
        public object ConvertSendAndReceive(object message, Func<Message, Message> messagePostProcessor) { return this.ConvertSendAndReceive(this.exchange, this.routingKey, message, messagePostProcessor); }

        /// <summary>Convert, send, and receive a message, given the message.</summary>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The message received.</returns>
        public object ConvertSendAndReceive(object message, IMessagePostProcessor messagePostProcessor) { return this.ConvertSendAndReceive(this.exchange, this.routingKey, message, messagePostProcessor); }

        /// <summary>Convert, send, and receive a message, given a routing key and the message.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The message received.</returns>
        public object ConvertSendAndReceive(string routingKey, object message, Func<Message, Message> messagePostProcessor) { return this.ConvertSendAndReceive(this.exchange, routingKey, message, messagePostProcessor); }

        /// <summary>Convert, send, and receive a message, given a routing key and the message.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The message received.</returns>
        public object ConvertSendAndReceive(string routingKey, object message, IMessagePostProcessor messagePostProcessor) { return this.ConvertSendAndReceive(this.exchange, routingKey, message, messagePostProcessor); }

        /// <summary>Convert, send, and receive a message, given an exchange, a routing key and the message.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The message received.</returns>
        public object ConvertSendAndReceive(string exchange, string routingKey, object message, Func<Message, Message> messagePostProcessor)
        {
            var requestMessage = this.ConvertMessageIfNecessary(message);
            if (messagePostProcessor != null)
            {
                requestMessage = messagePostProcessor.Invoke(requestMessage);
            }

            var replyMessage = this.DoSendAndReceive(exchange, routingKey, requestMessage);
            if (replyMessage == null)
            {
                return null;
            }

            return this.GetRequiredMessageConverter().FromMessage(replyMessage);
        }

        /// <summary>Convert, send, and receive a message, given an exchange, a routing key and the message.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The message received.</returns>
        public object ConvertSendAndReceive(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor)
        {
            var requestMessage = this.ConvertMessageIfNecessary(message);
            if (messagePostProcessor != null)
            {
                requestMessage = messagePostProcessor.PostProcessMessage(requestMessage);
            }

            var replyMessage = this.DoSendAndReceive(exchange, routingKey, requestMessage);
            if (replyMessage == null)
            {
                return null;
            }

            return this.GetRequiredMessageConverter().FromMessage(replyMessage);
        }

        /// <summary>The convert message if necessary.</summary>
        /// <param name="message">The message.</param>
        /// <returns>The Spring.Messaging.Amqp.Core.Message.</returns>
        protected Message ConvertMessageIfNecessary(object message)
        {
            if (message is Message)
            {
                return (Message)message;
            }

            return this.GetRequiredMessageConverter().ToMessage(message, new MessageProperties());
        }

        /// <summary>The do send and receive.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <returns>The Spring.Messaging.Amqp.Core.Message.</returns>
        protected Message DoSendAndReceive(string exchange, string routingKey, Message message)
        {
            if (this.replyQueue == null)
            {
                return this.DoSendAndReceiveWithTemporary(exchange, routingKey, message);
            }
            else
            {
                return this.DoSendAndReceiveWithFixed(exchange, routingKey, message);
            }
        }

        /// <summary>Do the send and receive operation, given an exchange, a routing key and the message.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The message received.</returns>
        protected Message DoSendAndReceiveWithTemporary(string exchange, string routingKey, Message message)
        {
            var replyMessage = this.Execute(
                delegate(IModel channel)
                {
                    var replyHandoff = new BlockingCollection<Message>();

                    AssertUtils.IsTrue(message.MessageProperties.ReplyTo == null, "Send-and-receive methods can only be used if the Message does not already have a replyTo property.");
                    var queueDeclaration = channel.QueueDeclare();
                    var replyTo = queueDeclaration.QueueName;
                    message.MessageProperties.ReplyTo = replyTo;

                    var noAck = true;
                    var consumerTag = Guid.NewGuid().ToString();
                    var noLocal = true;
                    var exclusive = true;
                    var consumer = new SendAndReceiveDefaultConsumer(channel, replyHandoff, this.encoding, this.messagePropertiesConverter);
                    channel.BasicConsume(replyTo, noAck, consumerTag, noLocal, exclusive, null, consumer);
                    this.DoSend(channel, exchange, routingKey, message, null);

                    var reply = (this.replyTimeout < 0) ? replyHandoff.Take() : replyHandoff.Poll((int)this.replyTimeout);
                    channel.BasicCancel(consumerTag);
                    return reply;
                });

            return replyMessage;
        }

        /// <summary>The do send and receive with fixed.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <returns>The Spring.Messaging.Amqp.Core.Message.</returns>
        protected Message DoSendAndReceiveWithFixed(string exchange, string routingKey, Message message)
        {
            var replyMessage = this.Execute(
                delegate(IModel channel)
                {
                    var replyHandoff = new BlockingCollection<Message>();
                    var messageTag = Guid.NewGuid().ToString();
                    this.replyHolder.Add(messageTag, replyHandoff);

                    var replyTo = message.MessageProperties.ReplyTo;
                    if (!string.IsNullOrWhiteSpace(replyTo))
                    {
                        Logger.Debug(m => m("Dropping replyTo header:{0} in favor of template's configured reply-queue:{1}", replyTo, this.replyQueue.Name));
                    }

                    var springReplyTo = (string)message.MessageProperties.Headers.Get(STACKED_REPLY_TO_HEADER);
                    message.MessageProperties.SetHeader(STACKED_REPLY_TO_HEADER, this.PushHeaderValue(replyTo, springReplyTo));

                    message.MessageProperties.ReplyTo = this.replyQueue.Name;
                    var correlation = (string)message.MessageProperties.Headers.Get(STACKED_CORRELATION_HEADER);
                    if (!string.IsNullOrWhiteSpace(correlation))
                    {
                        message.MessageProperties.SetHeader(STACKED_CORRELATION_HEADER, this.PushHeaderValue(messageTag, correlation));
                    }
                    else
                    {
                        message.MessageProperties.SetHeader("spring_reply_correlation", messageTag);
                    }

                    Logger.Debug("Sending message with tag " + messageTag);
                    this.DoSend(channel, exchange, routingKey, message, null);
                    var reply = (this.replyTimeout < 0) ? replyHandoff.Take() : replyHandoff.Poll((int)this.replyTimeout);
                    this.replyHolder.Remove(messageTag);
                    return reply;
                });

            return replyMessage;
        }

        #endregion

        #region Implementation of IRabbitOperations

        /// <summary>Execute an action.</summary>
        /// <typeparam name="T">Type T</typeparam>
        /// <param name="action">The action.</param>
        /// <returns>An object of Type T</returns>
        public T Execute<T>(ChannelCallbackDelegate<T> action)
        {
            AssertUtils.ArgumentNotNull(action, "Callback object must not be null");
            var resourceHolder = this.GetTransactionalResourceHolder();
            var channel = resourceHolder.Channel;

            if (this.confirmCallback != null || this.returnCallback != null)
            {
                this.AddListener(channel);
            }

            try
            {
                Logger.Debug(m => m("Executing callback on RabbitMQ Channel: {0}", channel));
                return action(channel);
            }
            catch (Exception ex)
            {
                if (this.ChannelLocallyTransacted(channel))
                {
                    resourceHolder.RollbackAll();
                }

                throw this.ConvertRabbitAccessException(ex);
            }
            finally
            {
                ConnectionFactoryUtils.ReleaseResources(resourceHolder);
            }
        }

        /// <summary>Execute an action.</summary>
        /// <typeparam name="T">Type T</typeparam>
        /// <param name="action">The action.</param>
        /// <returns>An object of Type T</returns>
        public T Execute<T>(IChannelCallback<T> action) { return Execute(action.DoInRabbit); }
        #endregion

        /// <summary>Do the send operation.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        /// <param name="correlationData">The correlation Data.</param>
        protected void DoSend(IModel channel, string exchange, string routingKey, Message message, CorrelationData correlationData)
        {
            Logger.Debug(m => m("Publishing message on exchange [{0}], routingKey = [{1}]", exchange, routingKey));

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

            if (this.confirmCallback != null && channel is IPublisherCallbackChannel)
            {
                var publisherCallbackChannel = (IPublisherCallbackChannel)channel;
                publisherCallbackChannel.AddPendingConfirm(this, (long)channel.NextPublishSeqNo, new PendingConfirm(correlationData, DateTime.UtcNow.ToMilliseconds()));
            }

            var mandatory = this.returnCallback == null ? false : this.mandatory;
            var immediate = this.returnCallback == null ? false : this.immediate;

            var messageProperties = message.MessageProperties;
            if (mandatory || immediate)
            {
                messageProperties.Headers.Add("spring_return_correlation", this.uuid);
            }

            var convertedMessageProperties = this.messagePropertiesConverter.FromMessageProperties(channel, message.MessageProperties, this.encoding);

            channel.BasicPublish(exchange, routingKey, mandatory, immediate, convertedMessageProperties, message.Body);

            // Check commit is needed.
            if (this.ChannelLocallyTransacted(channel))
            {
                // Transacted channel created by this template -> commit.
                RabbitUtils.CommitIfNecessary(channel);
            }
        }

        /// <summary>Flag indicating whether the channel is locally transacted.</summary>
        /// <param name="channel">The channel.</param>
        /// <returns>True if locally transacted, else false.</returns>
        protected bool ChannelLocallyTransacted(IModel channel) { return this.ChannelTransacted && !ConnectionFactoryUtils.IsChannelTransactional(channel, this.ConnectionFactory); }

        /// <summary>
        /// Get the required message converter.
        /// </summary>
        /// <returns>The message converter.</returns>
        private IMessageConverter GetRequiredMessageConverter()
        {
            var converter = this.MessageConverter;
            if (converter == null)
            {
                throw new AmqpIllegalStateException("No 'messageConverter' specified. Check configuration of RabbitTemplate.");
            }

            return converter;
        }

        /// <summary>
        /// Get the required queue.
        /// </summary>
        /// <returns>The name of the queue.</returns>
        private string GetRequiredQueue()
        {
            var name = this.queue;
            if (name == null)
            {
                throw new AmqpIllegalStateException("No 'queue' specified. Check configuration of RabbitTemplate.");
            }

            return name;
        }

        private void AddListener(IModel channel)
        {
            if (channel is IPublisherCallbackChannel)
            {
                var publisherCallbackChannel = (IPublisherCallbackChannel)channel;
                var pendingConfirm = publisherCallbackChannel.AddListener(this);
                if (!this.pendingConfirms.ContainsKey(channel))
                {
                    this.pendingConfirms.Add(channel, pendingConfirm);
                    Logger.Debug(m => m("Added pending confirms for {0} to map, size now {1}", channel, this.pendingConfirms.Count));
                }
            }
            else
            {
                throw new AmqpIllegalStateException("Channel does not support confirms or returns; is the connection factory configured for confirms or returns?");
            }
        }

        /// <summary>The handle confirm.</summary>
        /// <param name="pendingConfirm">The pending confirm.</param>
        /// <param name="ack">The ack.</param>
        public void HandleConfirm(PendingConfirm pendingConfirm, bool ack)
        {
            if (this.confirmCallback != null)
            {
                this.confirmCallback.Confirm(pendingConfirm.CorrelationData, ack);
            }
            else
            {
                Logger.Warn(m => m("Confirm received but no callback available"));
            }
        }

        /// <summary>The handle return.</summary>
        /// <param name="replyCode">The reply code.</param>
        /// <param name="replyText">The reply text.</param>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="body">The body.</param>
        public void HandleReturn(int replyCode, string replyText, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            if (this.returnCallback == null)
            {
                Logger.Warn(m => m("Returned message but no callback available"));
            }
            else
            {
                properties.Headers.Remove("spring_return_correlation");
                var messageProperties = this.messagePropertiesConverter.ToMessageProperties(properties, null, this.encoding);
                var returnedMessage = new Message(body, messageProperties);
                this.returnCallback.ReturnedMessage(returnedMessage, replyCode, replyText, exchange, routingKey);
            }
        }

        /// <summary>The is confirm listener.</summary>
        /// <returns>The System.Boolean.</returns>
        public bool IsConfirmListener { get { return this.confirmCallback != null; } }

        /// <summary>The is return listener.</summary>
        /// <returns>The System.Boolean.</returns>
        public bool IsReturnListener { get { return this.returnCallback != null; } }

        /// <summary>The remove pending confirms reference.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="unconfirmed">The unconfirmed.</param>
        public void RemovePendingConfirmsReference(IModel channel, SortedDictionary<long, PendingConfirm> unconfirmed)
        {
            this.pendingConfirms.Remove(channel);
            Logger.Debug(m => m("Removed pending confirms for {0} from map, size now {1}", channel, this.pendingConfirms.Count));
        }

        /// <summary>Gets the uuid.</summary>
        public string Uuid { get { return this.uuid; } }

        /// <summary>The on message.</summary>
        /// <param name="message">The message.</param>
        public void OnMessage(Message message)
        {
            var messageTag = (string)message.MessageProperties.Headers.Get(STACKED_CORRELATION_HEADER);
            if (messageTag == null)
            {
                Logger.Error("No correlation header in reply");
                return;
            }

            var poppedHeaderValue = this.PopHeaderValue(messageTag);
            messageTag = poppedHeaderValue.PoppedValue;
            message.MessageProperties.SetHeader(STACKED_CORRELATION_HEADER, poppedHeaderValue.NewValue);
            var springReplyTo = (string)message.MessageProperties.Headers.Get(STACKED_REPLY_TO_HEADER);
            if (springReplyTo != null)
            {
                poppedHeaderValue = this.PopHeaderValue(springReplyTo);
                springReplyTo = poppedHeaderValue.NewValue;
                message.MessageProperties.SetHeader(STACKED_REPLY_TO_HEADER, springReplyTo);
                message.MessageProperties.ReplyTo = null;
            }

            var queue = this.replyHolder.Get(messageTag);

            if (queue == null)
            {
                Logger.Warn(m => m("Reply received after timeout for {0}", messageTag));

                return;
            }

            queue.Add(message);
            Logger.Debug(m => m("Reply received for {0}", messageTag));
        }

        private string PushHeaderValue(string newValue, string oldValue)
        {
            if (oldValue == null)
            {
                return newValue;
            }
            else
            {
                return newValue + ":" + oldValue;
            }
        }

        private PoppedHeader PopHeaderValue(string value)
        {
            int index = value.IndexOf(":", StringComparison.Ordinal);
            if (index < 0)
            {
                return new PoppedHeader(value, null);
            }
            else
            {
                return new PoppedHeader(value.Substring(0, index), value.Substring(index + 1));
            }
        }
    }

    internal class PoppedHeader
    {
        private readonly string poppedValue;

        private readonly string newValue;

        /// <summary>Initializes a new instance of the <see cref="PoppedHeader"/> class.</summary>
        /// <param name="poppedValue">The popped value.</param>
        /// <param name="newValue">The new value.</param>
        public PoppedHeader(string poppedValue, string newValue)
        {
            this.poppedValue = poppedValue;
            if (!string.IsNullOrWhiteSpace(newValue))
            {
                this.newValue = newValue;
            }
            else
            {
                this.newValue = null;
            }
        }

        /// <summary>Gets the popped value.</summary>
        public string PoppedValue { get { return this.poppedValue; } }

        /// <summary>Gets the new value.</summary>
        public string NewValue { get { return this.newValue; } }
    }

    /// <summary>
    /// The admin default basic consumer.
    /// </summary>
    internal class SendAndReceiveDefaultConsumer : DefaultBasicConsumer
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The reply handoff.
        /// </summary>
        private readonly BlockingCollection<Message> replyHandoff;

        /// <summary>
        /// The encoding.
        /// </summary>
        private readonly string encoding;

        /// <summary>
        /// The message properties converter.
        /// </summary>
        private readonly IMessagePropertiesConverter messagePropertiesConverter;

        /// <summary>Initializes a new instance of the <see cref="SendAndReceiveDefaultConsumer"/> class.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="replyHandoff">The reply handoff.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="messagePropertiesConverter">The message properties converter.</param>
        public SendAndReceiveDefaultConsumer(IModel channel, BlockingCollection<Message> replyHandoff, string encoding, IMessagePropertiesConverter messagePropertiesConverter) : base(channel)
        {
            this.replyHandoff = replyHandoff;
            this.encoding = encoding;
            this.messagePropertiesConverter = messagePropertiesConverter;
        }

        /// <summary>Handle delivery.</summary>
        /// <param name="consumerTag">The consumer tag.</param>
        /// <param name="envelope">The envelope.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="body">The body.</param>
        public void HandleDelivery(string consumerTag, BasicGetResult envelope, IBasicProperties properties, byte[] body)
        {
            var messageProperties = this.messagePropertiesConverter.ToMessageProperties(properties, envelope, this.encoding);
            var reply = new Message(body, messageProperties);
            Logger.Trace(m => m("Message received {0}", reply));
            try
            {
                this.replyHandoff.Add(reply);
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
            this.HandleDelivery(consumerTag, envelope, properties, body);
        }
    }
}
