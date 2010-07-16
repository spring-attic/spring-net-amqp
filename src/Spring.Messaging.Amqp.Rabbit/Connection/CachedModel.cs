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
            target = targetModel;
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

        public void ExchangeDeclare(string exchange, string type, bool durable)
        {
            target.ExchangeDeclare(exchange, type, durable);
        }

        public void ExchangeDeclare(string exchange, string type)
        {
            target.ExchangeDeclare(exchange, type);
        }

        public void ExchangeDeclare(string exchange, string type, bool passive,
                                    bool durable, bool autoDelete, bool isInternal,
                                    bool nowait, IDictionary arguments)
        {
            target.ExchangeDeclare(exchange, type, passive,
                                   durable, autoDelete, isInternal,
                                   nowait, arguments);
        }

        public void ExchangeDelete(string exchange, bool ifUnused, bool nowait)
        {
            target.ExchangeDelete(exchange, ifUnused, nowait);
        }

        public string QueueDeclare()
        {
            return target.QueueDeclare();
        }

        public string QueueDeclare(string queue)
        {
            return target.QueueDeclare(queue);
        }

        public string QueueDeclare(string queue, bool durable)
        {
            return target.QueueDeclare(queue, durable);
        }

        public string QueueDeclare(string queue, bool passive, bool durable, bool exclusive, bool autoDelete,
                                   bool nowait, IDictionary arguments)
        {
            return target.QueueDeclare(queue, passive, durable, exclusive, autoDelete, nowait, arguments);
        }

        public void QueueBind(string queue, string exchange, string routingKey, bool nowait, IDictionary arguments)
        {
            target.QueueBind(queue, exchange, routingKey, nowait, arguments);
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            target.QueueUnbind(queue, exchange, routingKey, arguments);
        }

        public uint QueuePurge(string queue, bool nowait)
        {
            return target.QueuePurge(queue, nowait);
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty, bool nowait)
        {
            return target.QueueDelete(queue, ifUnused, ifEmpty, nowait);
        }

        public string BasicConsume(string queue, IDictionary filter, IBasicConsumer consumer)
        {
            return target.BasicConsume(queue, filter, consumer);
        }

        public string BasicConsume(string queue, bool noAck, IDictionary filter, IBasicConsumer consumer)
        {
            return target.BasicConsume(queue, noAck, filter, consumer);
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary filter,
                                   IBasicConsumer consumer)
        {
            return target.BasicConsume(queue, noAck, consumerTag, filter, consumer);
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, bool noLocal, bool exclusive,
                                   IDictionary filter, IBasicConsumer consumer)
        {
            return target.BasicConsume(queue, noAck, consumerTag, noLocal, exclusive, filter, consumer);
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

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate,
                                 IBasicProperties basicProperties, byte[] body)
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

        public void BasicRecover(bool requeue)
        {
            target.BasicRecover(requeue);
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

        #endregion

        public void Dispose()
        {
            target.Dispose();
        }
    }
}