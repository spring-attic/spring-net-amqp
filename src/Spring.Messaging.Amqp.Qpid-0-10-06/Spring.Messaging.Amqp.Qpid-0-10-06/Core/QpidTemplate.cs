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
using log4net;
using org.apache.qpid.client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Qpid.Client;
using Spring.Messaging.Amqp.Qpid.Support;
using Spring.Util;
using Message=Spring.Messaging.Amqp.Core.Message;

namespace Spring.Messaging.Amqp.Qpid.Core
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class QpidTemplate : QpidAccessor, IQpidOperations
    {
        private static readonly string DEFAULT_EXCHANGE = "";

        private static readonly string DEFAULT_ROUTING_KEY = "";

        #region Fields

        protected static readonly ILog logger = LogManager.GetLogger(typeof(QpidTemplate));

        private volatile string defaultExchange = DEFAULT_EXCHANGE;

  
        private volatile string defaultRoutingKey = DEFAULT_ROUTING_KEY;

        private volatile String defaultQueueName;

        private volatile bool mandatoryPublish;

        private volatile bool immediatePublish;

        private volatile bool requireAckOnReceive;


        private readonly QpidTemplateResourceFactory transactionalResourceFactory;

        #endregion



        public string DefaultQueueName
        {
            get { return defaultQueueName; }
            set { defaultQueueName = value; }
        }

        public string DefaultRoutingKey
        {
            get { return defaultRoutingKey; }
            set { defaultRoutingKey = value; }
        }


        protected virtual IClient GetConnection(QpidResourceHolder resourceHolder)
        {
            return resourceHolder.Connection;
        }

        protected virtual IClientSession GetChannel(QpidResourceHolder resourceHolder)
        {
            return resourceHolder.Channel;
        }

        #region Implementation of IAmqpTemplate

        public void Send(MessageCreatorDelegate messageCreator)
        {
            throw new NotImplementedException();
        }

        public void Send(string routingkey, MessageCreatorDelegate messageCreator)
        {
            throw new NotImplementedException();
        }

        public void Send(string exchange, string routingKey, MessageCreatorDelegate messageCreatorDelegate)
        {
            AssertUtils.ArgumentNotNull(messageCreatorDelegate, "MessageCreatorDelegate must not be null");
            Execute<object>(delegate(IClientSession model)
            {
                DoSend(model, exchange, routingKey, null, messageCreatorDelegate);
                return null;
            });
        }

        protected virtual void DoSend(IClientSession channel, string exchange, string routingKey, IMessageCreator messageCreator,
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
                message = messageCreatorDelegate();
            }
            if (exchange == null)
            {
                // try to send to default exchange
                exchange = this.defaultExchange;
            }
            if (routingKey == null)
            {
                // try to send to default routing key
                routingKey = this.defaultRoutingKey;
            }

            /*   org.apache.qpid.client.IMessage message = new org.apache.qpid.client.Message();
             *   message.ClearData();
                message.AppendData(Encoding.UTF8.GetBytes("That's all, folks!"));
                session.MessageTransfer("amq.direct", "routing_key", message);
                session.Sync();
             * 
             */
            org.apache.qpid.client.IMessage qpidMessage = new org.apache.qpid.client.Message();
            qpidMessage.AppendData(message.Body);

            qpidMessage.DeliveryProperties = QpidUtils.ExtractDeliveryProperties(message);

            qpidMessage.DeliveryProperties.SetRoutingKey(routingKey);
            qpidMessage.DeliveryProperties.SetExchange(exchange);

            qpidMessage.DeliveryProperties.SetImmediate(this.immediatePublish);
            //TODO where to set mandetoryPublish?

            channel.MessageTransfer(exchange, routingKey, qpidMessage);
            channel.Sync();            
            
           
            // TODO: should we be able to do (via wrapper) something like:
            // channel.getTransacted()?
            if (ChannelTransacted && ChannelLocallyTransacted(channel))
            {
                // Transacted channel created by this template -> commit.
                QpidUtils.CommitIfNecessary(channel);
            }
        }



        public void ConvertAndSend(object message)
        {
            throw new NotImplementedException();
        }

        public void ConvertAndSend(string routingKey, object message)
        {
            throw new NotImplementedException();
        }

        public void ConvertAndSend(string exchange, string routingKey, object message)
        {
            throw new NotImplementedException();
        }

        public void ConvertAndSend(object message, MessagePostProcessorDelegate messagePostProcessorDelegate)
        {
            throw new NotImplementedException();
        }

        public void ConvertAndSend(string routingKey, object message, MessagePostProcessorDelegate messagePostProcessorDelegate)
        {
            throw new NotImplementedException();
        }

        public void ConvertAndSend(string exchange, string routingKey, object message, MessagePostProcessorDelegate messagePostProcessorDelegate)
        {
            throw new NotImplementedException();
        }

        public Message Receive()
        {
            throw new NotImplementedException();
        }

        public Message Receive(string queueName)
        {
            throw new NotImplementedException();
        }

        public object ReceiveAndConvert()
        {
            throw new NotImplementedException();
        }

        public object ReceiveAndConvert(string queueName)
        {
            throw new NotImplementedException();
        }

        public IMessageProperties CreateMessageProperties()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation of IQpidOperations

        public T Execute<T>(IClientSessionCallback<T> action)
        {
            return Execute<T>(action.DoInSession);
        }

        public T Execute<T>(ClientSessionCallbackDelegate<T> action)
        {
            AssertUtils.ArgumentNotNull(action, "Callback object must not be null");
            IClient clientToClose = null;
            IClientSession sessionToClose = null;
            try
            {
                IClientSession channelToUse = ClientFactoryUtils
                    .DoGetTransactionalSession(ClientFactory,
                                               this.transactionalResourceFactory);
                if (channelToUse == null)
                {
                    clientToClose = CreateClient();
                    sessionToClose = CreateChannel(clientToClose);
                    channelToUse = sessionToClose;
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
                QpidUtils.CloseChannel(sessionToClose);
                ClientFactoryUtils.ReleaseConnection(clientToClose, ClientFactory);
            }
        }

        #endregion

        #region ResourceFactory Helper Class

        private class QpidTemplateResourceFactory : IResourceFactory
        {
            private QpidTemplate outer;

            public QpidTemplateResourceFactory(QpidTemplate outer)
            {
                this.outer = outer;
            }

            #region Implementation of IResourceFactory

            public IClientSession GetChannel(QpidResourceHolder holder)
            {
                return outer.GetChannel(holder);
            }

            public IClient GetConnection(QpidResourceHolder rabbitResourceHolder)
            {
                return outer.GetConnection(rabbitResourceHolder);
            }

            public IClient CreateConnection()
            {
                return outer.CreateClient();
            }

            public IClientSession CreateChannel(IClient connection)
            {
                return outer.CreateChannel(connection);
            }

            public bool IsSynchedLocalTransactionAllowed
            {
                get { return outer.ChannelTransacted; }
            }

            #endregion
        }

        #endregion

        protected bool ChannelLocallyTransacted(IClientSession channel)
        {
            return ChannelTransacted
                   && !ClientFactoryUtils.IsChannelTransactional(channel, ClientFactory);
        }
    }



}