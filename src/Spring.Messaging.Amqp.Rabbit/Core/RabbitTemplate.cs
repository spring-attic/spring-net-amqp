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

#region

using System;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Util;
using System.Collections.Generic;

#endregion

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// Helper class that simplifies synchronous RabbitMQ access code. 
    /// </summary>
    /// <author>Mark Pollack</author>
    public class RabbitTemplate : RabbitAccessor, IRabbitOperations
    {
        protected static readonly ILog logger = LogManager.GetLogger(typeof(RabbitTemplate));

        private static readonly string DEFAULT_EXCHANGE = string.Empty; // alias for amq.direct default exchange

        private static readonly string DEFAULT_ROUTING_KEY = string.Empty;

        private static readonly long DEFAULT_REPLY_TIMEOUT = 5000;

        private static readonly string DEFAULT_ENCODING = "UTF-8";

        #region Fields
        private string exchange = DEFAULT_EXCHANGE;

        private string routingKey = DEFAULT_ROUTING_KEY;

        /// <summary>
        /// The default queue name that will be used for synchronous receives.
        /// </summary>
        private string queue;

        private long replyTimeout = DEFAULT_REPLY_TIMEOUT;

        private IMessageConverter messageConverter = new SimpleMessageConverter();

        private string encoding = DEFAULT_ENCODING;
        #endregion

        #region Constructors

        public RabbitTemplate()
        {
            InitDefaultStrategies();
        }

        public RabbitTemplate(IConnectionFactory connectionFactory)
            : this()
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

        protected virtual void InitDefaultStrategies()
        {
            MessageConverter = new SimpleMessageConverter();
        }

        protected virtual IConnection GetConnection(RabbitResourceHolder resourceHolder)
        {
            return resourceHolder.Connection;
        }

        protected virtual IModel GetChannel(RabbitResourceHolder resourceHolder)
        {
            return resourceHolder.Channel;
        }

        #region Implementation of IRabbitOperations

        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="messageCreatorDelegate">
        /// The message creator delegate.
        /// </param>
        public void Send(MessageCreatorDelegate messageCreatorDelegate)
        {
            Send(this.exchange, this.routingKey, messageCreatorDelegate);
        }

        /// <summary>
        /// Send a message, given a routing key.
        /// </summary>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="messageCreatorDelegate">
        /// The message creator delegate.
        /// </param>
        public void Send(string routingKey, MessageCreatorDelegate messageCreatorDelegate)
        {
            Send(this.exchange, routingKey, messageCreatorDelegate);
        }

        public void Send(string exchange, string routingKey, MessageCreatorDelegate messageCreatorDelegate)
        {
            AssertUtils.ArgumentNotNull(messageCreatorDelegate, "MessageCreatorDelegate must not be null");
            Execute<object>(channel =>
                                {
                                    DoSend(channel, exchange, routingKey, null, messageCreatorDelegate);
                                    return null;
                                });
        }

        public void ConvertAndSend(object message)
        {
            this.ConvertAndSend(this.exchange, this.routingKey, message);
        }

        public void ConvertAndSend(string routingKey, object message)
        {
            this.ConvertAndSend(this.exchange, routingKey, message);
        }

        public void ConvertAndSend(string exchange, string routingKey, object message)
        {
            this.Send(exchange, routingKey, channel => GetRequiredMessageConverter().ToMessage(message, new MessageProperties()));
        }

        public void ConvertAndSend(object message, MessagePostProcessorDelegate messagePostProcessorDelegate)
        {
            this.ConvertAndSend(this.exchange, this.routingKey, message, messagePostProcessorDelegate);
        }

        public void ConvertAndSend(string routingKey, object message, MessagePostProcessorDelegate messagePostProcessorDelegate)
        {
            this.ConvertAndSend(this.exchange, routingKey, message, messagePostProcessorDelegate);
        }

        public void ConvertAndSend(string exchange, string routingKey, object message, MessagePostProcessorDelegate messagePostProcessorDelegate)
        {
            this.Send(exchange, routingKey, channel =>
                                        {
                                            Message messageToSend = GetRequiredMessageConverter().ToMessage(message, new MessageProperties());
                                            return messagePostProcessorDelegate(messageToSend);
                                        });
        }

        public Message Receive()
        {
            return this.Receive(this.GetRequiredQueue());
        }

        public Message Receive(string queueName)
        {
            return Execute<Message>(channel =>
                                         {
                                             var response = channel.BasicGet(queueName, !ChannelTransacted);
                                             // Response can be null is the case that there is no message on the queue.
                                             if (response != null)
                                             {
                                                 var deliveryTag = response.DeliveryTag;
                                                 if (ChannelLocallyTransacted(channel))
                                                 {
                                                     channel.BasicAck(deliveryTag, false);
                                                     channel.TxCommit();
                                                 }
                                                 else if (ChannelTransacted)
                                                 {
                                                     // Not locally transacted but it is transacted so it
                                                     // could be synchronized with an external transaction
                                                     ConnectionFactoryUtils.RegisterDeliveryTag(ConnectionFactory, channel, (long)deliveryTag);
                                                 }
                                                 MessageProperties messageProps = RabbitUtils.CreateMessageProperties(response.BasicProperties, response, encoding);
                                                 messageProps.MessageCount = (int)response.MessageCount;
                                                 return new Message(response.Body, messageProps);
                                             }
                                             return null;
                                         });
        }

        #region Implementation of IAmqpTemplate

        public object ReceiveAndConvert()
        {
            return ReceiveAndConvert(this.GetRequiredQueue());
        }

        public object ReceiveAndConvert(string queueName)
        {
            var response = Receive(queueName);
            if (response != null)
            {
                return GetRequiredMessageConverter().FromMessage(response);
            }
            return null;
        }


        public Message SendAndReceive(Message message)
        {
            return this.DoSendAndReceive(this.exchange, this.routingKey, message);
        }

        public Message SendAndReceive(string routingKey, Message message)
        {
            return this.DoSendAndReceive(this.exchange, routingKey, message);
        }

        public Message SendAndReceive(string exchange, string routingKey, Message message)
        {
            return this.DoSendAndReceive(exchange, routingKey, message);
        }

        public object ConvertSendAndReceive(object message)
        {
            return this.ConvertSendAndReceive(this.exchange, this.routingKey, message);
        }

        public object ConvertSendAndReceive(string routingKey, object message)
        {
            return this.ConvertSendAndReceive(this.exchange, routingKey, message);
        }

        public object ConvertSendAndReceive(string exchange, string routingKey, object message)
        {
            var messageProperties = new MessageProperties();
            var requestMessage = GetRequiredMessageConverter().ToMessage(message, messageProperties);
            var replyMessage = this.DoSendAndReceive(exchange, routingKey, requestMessage);
            if (replyMessage == null)
            {
                return null;
            }

            return this.GetRequiredMessageConverter().FromMessage(replyMessage);
        }

        private Message DoSendAndReceive(string exchange, string routingKey, Message message)
        {
            throw new NotImplementedException();
            //Message replyMessage = this.Execute<Message>(channel =>
            //    readonly Queue<Message> replyHandoff = new Queue<Message>();

            //    AssertUtils.IsTrue(message.MessageProperties.ReplyTo == null, "Send-and-receive methods can only be used if the Message does not already have a replyTo property.");
            //    DeclareOk queueDeclaration = channel.QueueDeclare();
            //    Address replyToAddress = new Address(ExchangeTypes.Direct, DEFAULT_EXCHANGE, queueDeclaration.Queue);
            //    message.MessageProperties.ReplyTo = replyToAddress;

            //    var noAck = false;
            //    var consumerTag = Guid.NewGuid().ToString();
            //    var noLocal = true;
            //    var exclusive = true;
            //DefaultConsumer consumer = new DefaultConsumer(channel) {

            //    public void handleDelivery(String consumerTag, Envelope envelope, AMQP.BasicProperties properties,
            //            byte[] body) throws IOException {
            //        MessageProperties messageProperties = RabbitUtils.createMessageProperties(properties, envelope,
            //                encoding);
            //        Message reply = new Message(body, messageProperties);
            //        try {
            //            replyHandoff.put(reply);
            //        } catch (InterruptedException e) {
            //            Thread.currentThread().interrupt();
            //        }
            //    }
            //};
            //channel.basicConsume(replyToAddress.getRoutingKey(), noAck, consumerTag, noLocal, exclusive, null, consumer);
            //DoSend(channel, exchange, routingKey, message);
            //Message reply = (replyTimeout < 0) ? replyHandoff.Take() : replyHandoff.Poll(replyTimeout,TimeUnit.MILLISECONDS);
            //channel.basicCancel(consumerTag);
            //return reply;
            //);

            //return replyMessage;
        }
        #endregion

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
                if (ChannelLocallyTransacted(channel))
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

        public T Execute<T>(IChannelCallback<T> action)
        {
            return Execute<T>(action.DoInRabbit);
        }

        #endregion

        protected virtual void DoSend(IModel channel, string exchange, string routingKey, IMessageCreator messageCreator,
                                      MessageCreatorDelegate messageCreatorDelegate)
        {
            AssertUtils.IsTrue((messageCreator == null && messageCreatorDelegate != null) ||
                                (messageCreator != null && messageCreatorDelegate == null), "Must provide a MessageCreatorDelegate or IMessageCreator instance.");
            Message message;
            if (messageCreator != null)
            {
                message = messageCreator.CreateMessage();
            }
            else
            {
                message = messageCreatorDelegate(channel);
            }
            if (exchange == null)
            {
                // try to send to the configured exchange
                exchange = this.exchange;
            }
            if (routingKey == null)
            {
                // try to send to configured routing key
                routingKey = this.routingKey;
            }

            IBasicProperties bp = RabbitUtils.ExtractBasicProperties(channel, message, this.encoding);
            channel.BasicPublish(exchange, routingKey, bp, message.Body);

            // Check commit - avoid commit call within a JTA transaction.
            // TODO: should we be able to do (via wrapper) something like:
            // channel.getTransacted()?
            if (ChannelTransacted && ChannelLocallyTransacted(channel))
            {
                // Transacted channel created by this template -> commit.
                RabbitUtils.CommitIfNecessary(channel);
            }
        }

        private void DoSend(IModel channel, string exchange, string routingKey, Message message)
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

            channel.BasicPublish(exchange, routingKey, false, false, RabbitUtils.ExtractBasicProperties(channel, message, encoding), message.Body);
            // Check commit - avoid commit call within a JTA transaction.
            if (ChannelLocallyTransacted(channel))
            {
                // Transacted channel created by this template -> commit.
                RabbitUtils.CommitIfNecessary(channel);
            }
        }

        protected bool ChannelLocallyTransacted(IModel channel)
        {
            return ChannelTransacted && !ConnectionFactoryUtils.IsChannelTransactional(channel, ConnectionFactory);
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

        public void Send(Message message)
        {
            throw new NotImplementedException();
        }

        public void Send(string routingKey, Message message)
        {
            throw new NotImplementedException();
        }

        public void Send(string exchange, string routingKey, Message message)
        {
            throw new NotImplementedException();
        }

        public MessageProperties CreateMessageProperties()
        {
            throw new NotImplementedException();
        }
    }
}