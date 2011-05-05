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
            Execute<object>(delegate(IModel channel)
                                {
                                    DoSend(channel, exchange, routingKey, null, messageCreatorDelegate);
                                    return null;
                                });
        }

        public void ConvertAndSend(object message)
        {
            ConvertAndSend(this.exchange, this.routingKey, message);
        }

        public void ConvertAndSend(string routingKey, object message)
        {
            ConvertAndSend(this.exchange, routingKey, message);
        }

        public void ConvertAndSend(string exchange, string routingKey, object message)
        {
            Send(exchange, routingKey, delegate(IModel channel)
                                           {
                                               return GetRequiredMessageConverter().ToMessage(message, new RabbitMessagePropertiesFactory(channel));
                                           });                
        }

        public void ConvertAndSend(object message, MessagePostProcessorDelegate messagePostProcessorDelegate)
        {
            ConvertAndSend(this.exchange, this.routingKey, message, messagePostProcessorDelegate);
        }

        public void ConvertAndSend(string routingKey, object message, MessagePostProcessorDelegate messagePostProcessorDelegate)
        {
            ConvertAndSend(this.exchange, routingKey, message, messagePostProcessorDelegate);
        }

        public void ConvertAndSend(string exchange, string routingKey, object message, MessagePostProcessorDelegate messagePostProcessorDelegate)
        {
            Send(exchange, routingKey, delegate (IModel channel)
                                        {

                                            Message msg = GetRequiredMessageConverter().ToMessage(message, new RabbitMessagePropertiesFactory(channel));
                                            return messagePostProcessorDelegate(msg);
                                        });
        }

        public Message Receive()
        {
            return Receive(GetRequiredQueue());
        }

        public Message Receive(string queueName)
        {
            return Execute<Message>(delegate(IModel model)
                                         {
                                             BasicGetResult result = model.BasicGet(queueName, !requireAck);                                             
                                             if (result != null)
                                             {
                                                 MessageProperties msgProps =
                                                     new MessageProperties(result.BasicProperties, result.Exchange, result.RoutingKey, result.Redelivered, result.DeliveryTag, result.MessageCount);
                                                 
                                                 //TODO check to copy over other properties such as DeliveryTag...
                                                 Message msg = new Message(result.Body, msgProps);
                                                 return msg;
                                             }
                                             return null;
                                         });
        }

        #region Implementation of IAmqpTemplate

        public object ReceiveAndConvert()
        {
            return ReceiveAndConvert(GetRequiredQueue());
        }

        public object ReceiveAndConvert(string queueName)
        {
            Message response = Receive(queueName);
            if (response != null)
            {
                return GetRequiredMessageConverter().FromMessage(response);
            }
            return null;
        }

        #endregion

        public T Execute<T>(ChannelCallbackDelegate<T> action)
        {
            AssertUtils.ArgumentNotNull(action, "Callback object must not be null");
            IConnection conToClose = null;
            IModel channelToClose = null;
            try
            {
                IModel channelToUse = ConnectionFactoryUtils
                    .DoGetTransactionalChannel(ConnectionFactory,
                                               this.TransactionalResourceFactory);
                if (channelToUse == null)
                {
                    conToClose = CreateConnection();
                    channelToClose = CreateChannel(conToClose);
                    channelToUse = channelToClose;
                }
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Executing callback on RabbitMQ Channel: " + channelToUse);
                }
                return action(channelToUse);
            }
            catch (Exception ex)
            {
                throw;
                //TOOD convertRabbitAccessException(ex) ?
            }
            finally
            {
                RabbitUtils.CloseChannel(channelToClose);
                ConnectionFactoryUtils.ReleaseConnection(conToClose, ConnectionFactory);
            }
        }

        public T Execute<T>(IChannelCallback<T> action)
        {
            return Execute<T>(action.DoInRabbit);
        }

        public MessageProperties CreateMessageProperties()
        {
            IBasicProperties basicProperties = Execute<IBasicProperties>(delegate(IModel model) 
                                                                             {
                                                                                 return model.CreateBasicProperties();
                                                                             });

            return DoCreateMessageProperties(basicProperties);
  
        }

        #endregion

        protected virtual void DoSend(IModel channel, string exchange, string routingKey, IMessageCreator messageCreator,
                                      MessageCreatorDelegate messageCreatorDelegate)
        {
            AssertUtils.IsTrue( (messageCreator == null && messageCreatorDelegate != null) ||
                                (messageCreator != null && messageCreatorDelegate == null) , "Must provide a MessageCreatorDelegate or IMessageCreator instance.");
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

        protected bool ChannelLocallyTransacted(IModel channel)
        {
            return ChannelTransacted
                   && !ConnectionFactoryUtils.IsChannelTransactional(channel, ConnectionFactory);
        }

        private string GetRequiredQueue()
        {
            String name = this.queue;
            if (name == null)
            {
                throw new InvalidOperationException(
                        "No 'queue' specified. Check configuration of RabbitTemplate.");
            }
            return name;
        }

        private IMessageConverter GetRequiredMessageConverter(){
            IMessageConverter converter = MessageConverter;
		    if (converter == null) {
                throw new InvalidOperationException(
					"No 'messageConverter' specified. Check configuration of RabbitTemplate.");
		}
		return converter;
	}
}