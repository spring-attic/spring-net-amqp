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
using System.Collections;
using System.Collections.Generic;
using Common.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class CachedModel : IDecoratorModel
    {
        #region Logging Definition

        private static readonly ILog LOG = LogManager.GetLogger(typeof (CachedModel));

        #endregion

        private IModel target;
        private LinkedList<IModel> modelList;
        private int modelCacheSize;
        private CachingConnectionFactory ccf;

        public CachedModel(IModel targetModel, LinkedList<IModel> modelList, CachingConnectionFactory ccf)
        {
            this.target = targetModel;
            this.modelList = modelList;
            this.modelCacheSize = ccf.ChannelCacheSize;
            this.ccf = ccf;
        }

        #region Implementation of IDecoratorModel

        /// <summary>
        /// Gets the target, for testing purposes.
        /// </summary>
        /// <value>The target.</value>
        public IModel TargetModel
        {
            get { return target; }
        }

        #endregion

        #region Implementation of IModel

        public void Close(ushort replyCode, string replyText)
        {
            if (ccf.Active)
            {
                //don't pass the call to the underlying target.
                lock (modelList)
                {
                    if (modelList.Count < modelCacheSize)
                    {
                        LogicalClose();
                        // Remain open in the session list.
                        return;
                    }
                }
            }
            // If we get here, we're supposed to shut down.
            PhysicalClose(replyCode, replyText);
        }

        public void Close()
        {
            if (ccf.Active)
            {
                //don't pass the call to the underlying target.
                lock (modelList)
                {
                    if (modelList.Count < modelCacheSize)
                    {
                        LogicalClose();
                        // Remain open in the session list.
                        return;
                    }
                }
            }
            // If we get here, we're supposed to shut down.
            PhysicalClose();
        }

        private void LogicalClose()
        {
            // Allow for multiple close calls...
            if (!modelList.Contains(this))
            {
                #region Logging

                if (LOG.IsDebugEnabled)
                {
                    LOG.Debug("Returning cached Session: " + target);
                }

                #endregion

                modelList.AddLast(this); //add to end of linked list. 
            }
        }


        private void PhysicalClose()
        {
            if (LOG.IsDebugEnabled)
            {
                LOG.Debug("Closing cached Model: " + this.target);
            }
            target.Close();
        }

        private void PhysicalClose(ushort replyCode, string replyText)
        {
            if (LOG.IsDebugEnabled)
            {
                LOG.Debug("Closing cached Model: " + this.target);
            }
            target.Close(replyCode, replyText);
        }

        #endregion

        #region Pass through implementations of IModel

        public IBasicProperties CreateBasicProperties()
        {
            return target.CreateBasicProperties();
        }

        public IFileProperties CreateFileProperties()
        {
            return target.CreateFileProperties();
        }

        public IStreamProperties CreateStreamProperties()
        {
            return target.CreateStreamProperties();
        }

        public void ChannelFlow(bool active)
        {
            target.ChannelFlow(active);
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary arguments)
        {
            target.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);
        }

        public void ExchangeDeclare(string exchange, string type, bool durable)
        {
            target.ExchangeDeclare(exchange, type, durable);
        }

        public void ExchangeDeclare(string exchange, string type)
        {
            target.ExchangeDeclare(exchange, type);
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            target.ExchangeDeclarePassive(exchange);
        }

        public void ExchangeDelete(string exchange, bool ifUnused)
        {
            target.ExchangeDelete(exchange, ifUnused);
        }

        public void ExchangeDelete(string exchange)
        {
            target.ExchangeDelete(exchange);
        }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary arguments)
        {
            target.ExchangeBind(destination, source, routingKey, arguments);
        }

        public void ExchangeBind(string destination, string source, string routingKey)
        {
            target.ExchangeBind(destination, source, routingKey);
        }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary arguments)
        {
            target.ExchangeUnbind(destination, source, routingKey, arguments);
        }

        public void ExchangeUnbind(string destination, string source, string routingKey)
        {
            target.ExchangeUnbind(destination, source, routingKey);
        }

        public string QueueDeclare()
        {
            return target.QueueDeclare();
        }

        public string QueueDeclarePassive(string queue)
        {
            return target.QueueDeclarePassive(queue);
        }

        public string QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary arguments)
        {
            return target.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            target.QueueBind(queue, exchange, routingKey, arguments);
        }

        public void QueueBind(string queue, string exchange, string routingKey)
        {
            target.QueueBind(queue, exchange, routingKey);
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            target.QueueUnbind(queue, exchange, routingKey, arguments);
        }

        public uint QueuePurge(string queue)
        {
            return target.QueuePurge(queue);
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
        {
            return target.QueueDelete(queue, ifUnused, ifEmpty);
        }

        public uint QueueDelete(string queue)
        {
            return target.QueueDelete(queue);
        }

        public void ConfirmSelect()
        {
            target.ConfirmSelect();
        }

        public string BasicConsume(string queue, bool noAck, IBasicConsumer consumer)
        {
            return target.BasicConsume(queue, noAck, consumer);
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IBasicConsumer consumer)
        {
            return target.BasicConsume(queue, noAck, consumerTag, consumer);
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary arguments, IBasicConsumer consumer)
        {
            return target.BasicConsume(queue, noAck, consumerTag, arguments, consumer);
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, bool noLocal, bool exclusive, IDictionary arguments, IBasicConsumer consumer)
        {
            return target.BasicConsume(queue, noAck, consumerTag, noLocal, exclusive, arguments, consumer);
        }

        public void BasicCancel(string consumerTag)
        {
            target.BasicCancel(consumerTag);
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            target.BasicQos(prefetchSize, prefetchCount, global);
        }

        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body)
        {
            target.BasicPublish(addr, basicProperties, body);
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            target.BasicPublish(exchange, routingKey, basicProperties, body);
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties, byte[] body)
        {
            target.BasicPublish(exchange, routingKey, mandatory, immediate, basicProperties, body);
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            target.BasicAck(deliveryTag, multiple);
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            target.BasicReject(deliveryTag, requeue);
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            target.BasicNack(deliveryTag, multiple, requeue);
        }

        public void BasicRecover(bool requeue)
        {
            target.BasicRecover(requeue);
        }

        public void BasicRecoverAsync(bool requeue)
        {
            target.BasicRecoverAsync(requeue);
        }

        public BasicGetResult BasicGet(string queue, bool noAck)
        {
            return target.BasicGet(queue, noAck);
        }

        public void TxSelect()
        {
            target.TxSelect();
        }

        public void TxCommit()
        {
            target.TxCommit();
        }

        public void TxRollback()
        {
            target.TxRollback();
        }

        public void DtxSelect()
        {
            target.DtxSelect();
        }

        public void DtxStart(string dtxIdentifier)
        {
            target.DtxStart(dtxIdentifier);
        }

        public void Abort()
        {
            target.Abort();
        }

		public void Abort(ushort replyCode, string replyText)
        {
            target.Abort(replyCode, replyText);
        }

        public IBasicConsumer DefaultConsumer
        {
            get { return target.DefaultConsumer; }
            set { target.DefaultConsumer = value; }
        }

        public ShutdownEventArgs CloseReason
        {
            get { return target.CloseReason; }
        }

        public bool IsOpen
        {
            get { return target.IsOpen; }
        }

        public ulong NextPublishSeqNo
        {
            get { return target.NextPublishSeqNo; }
        }

        public event ModelShutdownEventHandler ModelShutdown
        {
            add
            {
                ModelShutdown += value;
            }
            remove
            {
                ModelShutdown -= value;
            }
        }

        public event BasicReturnEventHandler BasicReturn
        {
            add
            {
                BasicReturn += value;
            }
            remove
            {
                BasicReturn -= value;
            }
        }

        public event BasicAckEventHandler BasicAcks;
        public event BasicNackEventHandler BasicNacks;

        public event CallbackExceptionEventHandler CallbackException
        {
            add
            {
                CallbackException += value;
            }
            remove
            {
                CallbackException -= value;
            }
        }

        public event FlowControlEventHandler FlowControl;
        public event BasicRecoverOkEventHandler BasicRecoverOk;

        #endregion

        public void Dispose()
        {
            target.Dispose();
        }
    }
}