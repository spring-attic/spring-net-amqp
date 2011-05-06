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
    public class CachedModel : IChannelProxy
    {
        #region Logging Definition

        private static readonly ILog LOG = LogManager.GetLogger(typeof (CachedModel));

        #endregion

        private IModel target;
        private LinkedList<IChannelProxy> modelList;
        private readonly object targetMonitor = new object();
        private readonly bool transactional;

        private int modelCacheSize;
        private CachingConnectionFactory ccf;

        public CachedModel(IModel targetModel, LinkedList<IChannelProxy> channelList, bool transactional, CachingConnectionFactory ccf)
        {
            this.target = targetModel;
            this.modelList = channelList;
            this.transactional = transactional;
            this.modelCacheSize = ccf.ChannelCacheSize;
            this.ccf = ccf;
        }

        #region Implementation of IChannelProxy

        /// <summary>
        /// Gets the target, return underlying channel.
        /// </summary>
        /// <value>The target.</value>
        public IModel GetTargetChannel()
        {
            return target;
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

        public void TxSelect()
        {
            if (!this.transactional)
            {
                throw new InvalidOperationException("Cannot start transaction on non-transactional channel");
            }
            target.TxSelect();
        }

        private void LogicalClose()
        {
            if (!this.target.IsOpen)
            {
                lock (targetMonitor)
                {
                    if (!this.target.IsOpen)
                    {
                        this.target = null;
                        return;
                    }
                }
            }

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
            if (this.target == null)
            {
                return;
            }
            if (this.target.IsOpen)
            {
                lock (targetMonitor)
                {
                    if (this.target.IsOpen)
                    {
                        this.target.Close();
                    }
                    this.target = null;
                }
            }
        }

        private void PhysicalClose(ushort replyCode, string replyText)
        {
            if (LOG.IsDebugEnabled)
            {
                LOG.Debug("Closing cached Model: " + this.target);
            }
            if (this.target == null)
            {
                return;
            }
            if (this.target.IsOpen)
            {
                lock (targetMonitor)
                {
                    if (this.target.IsOpen)
                    {
                        this.target.Close(replyCode, replyText);
                    }
                    this.target = null;
                }
            }
        }

        #endregion

        private T InvokeFunctionWithReconnect<T>(Func<T> func)
        {
            try
            {
                lock (targetMonitor)
                {
                    if (target == null)
                    {
                        target = ccf.CreateBareChannel(transactional);
                    }
                }
                return func.Invoke();
            }
            catch (Exception e)
            {
                if (!this.target.IsOpen)
                {
                    // Basic re-connection logic
                    LOG.Debug("Detected closed channel on exception.  Re-initializing: " + target);
                }
                lock (targetMonitor)
                {
                    if (!this.target.IsOpen)
                    {
                        this.target = ccf.CreateBareChannel(transactional);
                    }
                }
                throw;
            }
        }

        private void InvokeActionWithReconnect(Action action)
        {
            try
            {
                lock (targetMonitor)
                {
                    if (target == null)
                    {
                        target = ccf.CreateBareChannel(transactional);
                    }
                }
                action.Invoke();
            }
            catch (Exception e)
            {
                if (!this.target.IsOpen)
                {
                    // Basic re-connection logic
                    LOG.Debug("Detected closed channel on exception.  Re-initializing: " + target);
                }
                lock (targetMonitor)
                {
                    if (!this.target.IsOpen)
                    {
                        this.target = ccf.CreateBareChannel(transactional);
                    }
                }
                throw;
            }
        }

        #region Wrapped invocations of IModel methods to perform reconnect logic

        public IBasicProperties CreateBasicProperties()
        {
            return InvokeFunctionWithReconnect( () => target.CreateBasicProperties());        
        }

        public IFileProperties CreateFileProperties()
        {
            return InvokeFunctionWithReconnect(() => target.CreateFileProperties());
        }

        public IStreamProperties CreateStreamProperties()
        {
            return InvokeFunctionWithReconnect(() => target.CreateStreamProperties());
        }

        public void ChannelFlow(bool active)
        {
            InvokeActionWithReconnect(() => target.ChannelFlow(active));
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary arguments)
        {
            InvokeActionWithReconnect(() => target.ExchangeDeclare(exchange, type, durable, autoDelete, arguments));
        }


        public void ExchangeDeclare(string exchange, string type, bool durable)
        {
            InvokeActionWithReconnect(() => target.ExchangeDeclare(exchange, type, durable));
        }

        public void ExchangeDeclare(string exchange, string type)
        {
            InvokeActionWithReconnect(() => target.ExchangeDeclare(exchange, type));
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            InvokeActionWithReconnect(() => target.ExchangeDeclarePassive(exchange));
        }

        public void ExchangeDelete(string exchange, bool ifUnused)
        {
            InvokeActionWithReconnect(() => target.ExchangeDelete(exchange, ifUnused));
        }

        public void ExchangeDelete(string exchange)
        {
            InvokeActionWithReconnect(() => target.ExchangeDelete(exchange));
        }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary arguments)
        {
            InvokeActionWithReconnect(() => target.ExchangeBind(destination, source, routingKey, arguments));
        }

        public void ExchangeBind(string destination, string source, string routingKey)
        {
            InvokeActionWithReconnect(() => target.ExchangeBind(destination, source, routingKey));
        }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary arguments)
        {
            InvokeActionWithReconnect(() => target.ExchangeUnbind(destination, source, routingKey, arguments));
        }

        public void ExchangeUnbind(string destination, string source, string routingKey)
        {
            InvokeActionWithReconnect(() => target.ExchangeUnbind(destination, source, routingKey));
        }

        public string QueueDeclare()
        {
            return InvokeFunctionWithReconnect(() => target.QueueDeclare());
        }

        public string QueueDeclarePassive(string queue)
        {
            return InvokeFunctionWithReconnect(() => target.QueueDeclarePassive(queue));
        }

        public string QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary arguments)
        {
            return
                InvokeFunctionWithReconnect(() => target.QueueDeclare(queue, durable, exclusive, autoDelete, arguments));
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            InvokeActionWithReconnect(() => target.QueueBind(queue, exchange, routingKey, arguments));
        }

        public void QueueBind(string queue, string exchange, string routingKey)
        {
            InvokeActionWithReconnect(() => target.QueueBind(queue, exchange, routingKey));
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            InvokeActionWithReconnect(() => target.QueueUnbind(queue, exchange, routingKey, arguments));
        }

        public uint QueuePurge(string queue)
        {
            return InvokeFunctionWithReconnect(() => target.QueuePurge(queue));
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
        {
            return InvokeFunctionWithReconnect(() => target.QueueDelete(queue, ifUnused, ifEmpty));
        }

        public uint QueueDelete(string queue)
        {
            return InvokeFunctionWithReconnect(() => target.QueueDelete(queue));
        }

        public void ConfirmSelect()
        {
            InvokeActionWithReconnect(() => target.ConfirmSelect());
        }

        public string BasicConsume(string queue, bool noAck, IBasicConsumer consumer)
        {
            return InvokeFunctionWithReconnect(() => target.BasicConsume(queue, noAck, consumer));
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IBasicConsumer consumer)
        {
            return InvokeFunctionWithReconnect(() => target.BasicConsume(queue, noAck, consumerTag, consumer));
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary arguments, IBasicConsumer consumer)
        {
            return InvokeFunctionWithReconnect(() => target.BasicConsume(queue, noAck, consumerTag, arguments, consumer));
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, bool noLocal, bool exclusive, IDictionary arguments, IBasicConsumer consumer)
        {
            return
                InvokeFunctionWithReconnect(
                    () => target.BasicConsume(queue, noAck, consumerTag, noLocal, exclusive, arguments, consumer));
        }

        public void BasicCancel(string consumerTag)
        {
            InvokeActionWithReconnect(() => target.BasicCancel(consumerTag));
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            InvokeActionWithReconnect(() => target.BasicQos(prefetchSize, prefetchCount, global));
        }

        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body)
        {
            InvokeActionWithReconnect(() => target.BasicPublish(addr, basicProperties, body));
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            InvokeActionWithReconnect(() => target.BasicPublish(exchange, routingKey, basicProperties, body));
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties, byte[] body)
        {
            InvokeActionWithReconnect(
                () => target.BasicPublish(exchange, routingKey, mandatory, immediate, basicProperties, body));
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            InvokeActionWithReconnect(() => target.BasicAck(deliveryTag, multiple));
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            InvokeActionWithReconnect(() => target.BasicReject(deliveryTag, requeue));
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            InvokeActionWithReconnect(() => target.BasicNack(deliveryTag, multiple, requeue));
        }

        public void BasicRecover(bool requeue)
        {
            InvokeActionWithReconnect(() => target.BasicRecover(requeue));
        }

        public void BasicRecoverAsync(bool requeue)
        {
            InvokeActionWithReconnect(() => target.BasicRecoverAsync(requeue));
        }

        public BasicGetResult BasicGet(string queue, bool noAck)
        {
            return InvokeFunctionWithReconnect(() => target.BasicGet(queue, noAck));
        }

        public void TxCommit()
        {
            InvokeActionWithReconnect(() => target.TxCommit());
        }

        public void TxRollback()
        {
            InvokeActionWithReconnect(() => target.TxRollback());
        }

        public void DtxSelect()
        {
            InvokeActionWithReconnect(() => target.DtxSelect());
        }

        public void DtxStart(string dtxIdentifier)
        {
            InvokeActionWithReconnect(() => target.DtxStart(dtxIdentifier));
        }

        public void Abort()
        {
            InvokeActionWithReconnect(() => target.Abort());
        }

		public void Abort(ushort replyCode, string replyText)
		{
		    InvokeActionWithReconnect(() => target.Abort(replyCode, replyText));
		}

        // TODO - wrap the rest with InvokeAction/Function

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
            InvokeActionWithReconnect(() => target.Dispose());
        }
    }
}