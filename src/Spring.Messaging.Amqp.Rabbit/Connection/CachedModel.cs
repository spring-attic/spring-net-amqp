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
    /// A cached model.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class CachedModel : IChannelProxy
    {
        #region Logging Definition

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(CachedModel));

        #endregion

        /// <summary>
        /// The target.
        /// </summary>
        private IModel target;

        /// <summary>
        /// The model list.
        /// </summary>
        private LinkedList<IChannelProxy> modelList;

        /// <summary>
        /// The target monitor.
        /// </summary>
        private readonly object targetMonitor = new object();

        /// <summary>
        /// The transactional flag.
        /// </summary>
        private readonly bool transactional;

        /// <summary>
        /// The model cache size.
        /// </summary>
        private int modelCacheSize;

        /// <summary>
        /// The caching connection factory.
        /// </summary>
        private CachingConnectionFactory ccf;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedModel"/> class.
        /// </summary>
        /// <param name="targetModel">
        /// The target model.
        /// </param>
        /// <param name="channelList">
        /// The channel list.
        /// </param>
        /// <param name="transactional">
        /// The transactional.
        /// </param>
        /// <param name="ccf">
        /// The ccf.
        /// </param>
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
        /// <value>
        /// The target.
        /// </value>
        /// <returns>
        /// The get target channel.
        /// </returns>
        public IModel GetTargetChannel()
        {
            return this.target;
        }

        #endregion

        #region Implementation of IModel

        /// <summary>
        /// Close action.
        /// </summary>
        /// <param name="replyCode">
        /// The reply code.
        /// </param>
        /// <param name="replyText">
        /// The reply text.
        /// </param>
        public void Close(ushort replyCode, string replyText)
        {
            if (this.ccf.Active)
            {
                // don't pass the call to the underlying target.
                lock (this.modelList)
                {
                    if (this.modelList.Count < this.modelCacheSize)
                    {
                        this.LogicalClose();

                        // Remain open in the session list.
                        return;
                    }
                }
            }

            // If we get here, we're supposed to shut down.
            this.PhysicalClose(replyCode, replyText);
        }

        /// <summary>
        /// Close action.
        /// </summary>
        public void Close()
        {
            if (this.ccf.Active)
            {
                // don't pass the call to the underlying target.
                lock (this.modelList)
                {
                    if (this.modelList.Count < this.modelCacheSize)
                    {
                        this.LogicalClose();

                        // Remain open in the session list.
                        return;
                    }
                }
            }

            // If we get here, we're supposed to shut down.
            this.PhysicalClose();
        }

        /// <summary>
        /// Select the transaction.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public void TxSelect()
        {
            if (!this.transactional)
            {
                throw new InvalidOperationException("Cannot start transaction on non-transactional channel");
            }

            this.target.TxSelect();
        }

        /// <summary>
        /// Logical close action.
        /// </summary>
        private void LogicalClose()
        {
            if (!this.target.IsOpen)
            {
                lock (this.targetMonitor)
                {
                    if (!this.target.IsOpen)
                    {
                        this.target = null;
                        return;
                    }
                }
            }

            // Allow for multiple close calls...
            if (!this.modelList.Contains(this))
            {
                if (this.logger.IsDebugEnabled)
                {
                    this.logger.Debug("Returning cached Session: " + this.target);
                }

                this.modelList.AddLast(this); // add to end of linked list. 
            }
        }


        /// <summary>
        /// Physical close action.
        /// </summary>
        private void PhysicalClose()
        {
            if (this.logger.IsDebugEnabled)
            {
                this.logger.Debug("Closing cached Model: " + this.target);
            }

            if (this.target == null)
            {
                return;
            }

            if (this.target.IsOpen)
            {
                lock (this.targetMonitor)
                {
                    if (this.target.IsOpen)
                    {
                        this.target.Close();
                    }

                    this.target = null;
                }
            }
        }

        /// <summary>
        /// Physical close action.
        /// </summary>
        /// <param name="replyCode">
        /// The reply code.
        /// </param>
        /// <param name="replyText">
        /// The reply text.
        /// </param>
        private void PhysicalClose(ushort replyCode, string replyText)
        {
            if (this.logger.IsDebugEnabled)
            {
                this.logger.Debug("Closing cached Model: " + this.target);
            }

            if (this.target == null)
            {
                return;
            }

            if (this.target.IsOpen)
            {
                lock (this.targetMonitor)
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

        /// <summary>
        /// Invoke function with reconnect.
        /// </summary>
        /// <param name="func">
        /// The func.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// Type T.
        /// </returns>
        private T InvokeFunctionWithReconnect<T>(Func<T> func)
        {
            try
            {
                lock (this.targetMonitor)
                {
                    if (this.target == null)
                    {
                        this.target = this.ccf.CreateBareChannel(this.transactional);
                    }
                }

                return func.Invoke();
            }
            catch (Exception e)
            {
                if (!this.target.IsOpen)
                {
                    // Basic re-connection logic
                    this.logger.Debug("Detected closed channel on exception.  Re-initializing: " + this.target);
                }

                lock (this.targetMonitor)
                {
                    if (!this.target.IsOpen)
                    {
                        this.target = this.ccf.CreateBareChannel(this.transactional);
                    }
                }

                throw;
            }
        }

        /// <summary>
        /// Invoke action with reconnect.
        /// </summary>
        /// <param name="action">
        /// The action.
        /// </param>
        private void InvokeActionWithReconnect(Action action)
        {
            try
            {
                lock (this.targetMonitor)
                {
                    if (this.target == null)
                    {
                        this.target = this.ccf.CreateBareChannel(this.transactional);
                    }
                }

                action.Invoke();
            }
            catch (Exception e)
            {
                if (!this.target.IsOpen)
                {
                    // Basic re-connection logic
                    this.logger.Debug("Detected closed channel on exception.  Re-initializing: " + this.target);
                }

                lock (this.targetMonitor)
                {
                    if (!this.target.IsOpen)
                    {
                        this.target = this.ccf.CreateBareChannel(this.transactional);
                    }
                }

                throw;
            }
        }

        #region Wrapped invocations of IModel methods to perform reconnect logic

        public IBasicProperties CreateBasicProperties()
        {
            return this.InvokeFunctionWithReconnect(() => this.target.CreateBasicProperties());        
        }

        public IFileProperties CreateFileProperties()
        {
            return this.InvokeFunctionWithReconnect(() => this.target.CreateFileProperties());
        }

        public IStreamProperties CreateStreamProperties()
        {
            return this.InvokeFunctionWithReconnect(() => this.target.CreateStreamProperties());
        }

        public void ChannelFlow(bool active)
        {
            this.InvokeActionWithReconnect(() => this.target.ChannelFlow(active));
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary arguments)
        {
            this.InvokeActionWithReconnect(() => this.target.ExchangeDeclare(exchange, type, durable, autoDelete, arguments));
        }


        public void ExchangeDeclare(string exchange, string type, bool durable)
        {
            this.InvokeActionWithReconnect(() => this.target.ExchangeDeclare(exchange, type, durable));
        }

        public void ExchangeDeclare(string exchange, string type)
        {
            this.InvokeActionWithReconnect(() => this.target.ExchangeDeclare(exchange, type));
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            this.InvokeActionWithReconnect(() => this.target.ExchangeDeclarePassive(exchange));
        }

        public void ExchangeDelete(string exchange, bool ifUnused)
        {
            this.InvokeActionWithReconnect(() => this.target.ExchangeDelete(exchange, ifUnused));
        }

        public void ExchangeDelete(string exchange)
        {
            this.InvokeActionWithReconnect(() => this.target.ExchangeDelete(exchange));
        }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary arguments)
        {
            this.InvokeActionWithReconnect(() => this.target.ExchangeBind(destination, source, routingKey, arguments));
        }

        public void ExchangeBind(string destination, string source, string routingKey)
        {
            this.InvokeActionWithReconnect(() => this.target.ExchangeBind(destination, source, routingKey));
        }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary arguments)
        {
            this.InvokeActionWithReconnect(() => this.target.ExchangeUnbind(destination, source, routingKey, arguments));
        }

        public void ExchangeUnbind(string destination, string source, string routingKey)
        {
            this.InvokeActionWithReconnect(() => this.target.ExchangeUnbind(destination, source, routingKey));
        }

        public string QueueDeclare()
        {
            return this.InvokeFunctionWithReconnect(() => this.target.QueueDeclare());
        }

        public string QueueDeclarePassive(string queue)
        {
            return this.InvokeFunctionWithReconnect(() => this.target.QueueDeclarePassive(queue));
        }

        public string QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary arguments)
        {
            return this.InvokeFunctionWithReconnect(() => this.target.QueueDeclare(queue, durable, exclusive, autoDelete, arguments));
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            this.InvokeActionWithReconnect(() => this.target.QueueBind(queue, exchange, routingKey, arguments));
        }

        public void QueueBind(string queue, string exchange, string routingKey)
        {
            this.InvokeActionWithReconnect(() => this.target.QueueBind(queue, exchange, routingKey));
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            this.InvokeActionWithReconnect(() => this.target.QueueUnbind(queue, exchange, routingKey, arguments));
        }

        public uint QueuePurge(string queue)
        {
            return this.InvokeFunctionWithReconnect(() => this.target.QueuePurge(queue));
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
        {
            return this.InvokeFunctionWithReconnect(() => this.target.QueueDelete(queue, ifUnused, ifEmpty));
        }

        public uint QueueDelete(string queue)
        {
            return this.InvokeFunctionWithReconnect(() => this.target.QueueDelete(queue));
        }

        public void ConfirmSelect()
        {
            this.InvokeActionWithReconnect(() => this.target.ConfirmSelect());
        }

        public string BasicConsume(string queue, bool noAck, IBasicConsumer consumer)
        {
            return this.InvokeFunctionWithReconnect(() => this.target.BasicConsume(queue, noAck, consumer));
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IBasicConsumer consumer)
        {
            return this.InvokeFunctionWithReconnect(() => this.target.BasicConsume(queue, noAck, consumerTag, consumer));
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary arguments, IBasicConsumer consumer)
        {
            return this.InvokeFunctionWithReconnect(() => this.target.BasicConsume(queue, noAck, consumerTag, arguments, consumer));
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, bool noLocal, bool exclusive, IDictionary arguments, IBasicConsumer consumer)
        {
            return this.InvokeFunctionWithReconnect(() => this.target.BasicConsume(queue, noAck, consumerTag, noLocal, exclusive, arguments, consumer));
        }

        public void BasicCancel(string consumerTag)
        {
            this.InvokeActionWithReconnect(() => this.target.BasicCancel(consumerTag));
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            this.InvokeActionWithReconnect(() => this.target.BasicQos(prefetchSize, prefetchCount, global));
        }

        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body)
        {
            this.InvokeActionWithReconnect(() => this.target.BasicPublish(addr, basicProperties, body));
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            this.InvokeActionWithReconnect(() => this.target.BasicPublish(exchange, routingKey, basicProperties, body));
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties, byte[] body)
        {
            this.InvokeActionWithReconnect(() => this.target.BasicPublish(exchange, routingKey, mandatory, immediate, basicProperties, body));
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            this.InvokeActionWithReconnect(() => this.target.BasicAck(deliveryTag, multiple));
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            this.InvokeActionWithReconnect(() => this.target.BasicReject(deliveryTag, requeue));
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            this.InvokeActionWithReconnect(() => this.target.BasicNack(deliveryTag, multiple, requeue));
        }

        public void BasicRecover(bool requeue)
        {
            this.InvokeActionWithReconnect(() => this.target.BasicRecover(requeue));
        }

        public void BasicRecoverAsync(bool requeue)
        {
            this.InvokeActionWithReconnect(() => this.target.BasicRecoverAsync(requeue));
        }

        public BasicGetResult BasicGet(string queue, bool noAck)
        {
            return this.InvokeFunctionWithReconnect(() => this.target.BasicGet(queue, noAck));
        }

        public void TxCommit()
        {
            this.InvokeActionWithReconnect(() => this.target.TxCommit());
        }

        public void TxRollback()
        {
            this.InvokeActionWithReconnect(() => this.target.TxRollback());
        }

        public void DtxSelect()
        {
            this.InvokeActionWithReconnect(() => this.target.DtxSelect());
        }

        public void DtxStart(string dtxIdentifier)
        {
            this.InvokeActionWithReconnect(() => this.target.DtxStart(dtxIdentifier));
        }

        public void Abort()
        {
            this.InvokeActionWithReconnect(() => this.target.Abort());
        }

		public void Abort(ushort replyCode, string replyText)
		{
            this.InvokeActionWithReconnect(() => this.target.Abort(replyCode, replyText));
		}

        // TODO - wrap the rest with InvokeAction/Function

        /// <summary>
        /// Gets or sets DefaultConsumer.
        /// </summary>
        public IBasicConsumer DefaultConsumer
        {
            get { return this.target.DefaultConsumer; }
            set { this.target.DefaultConsumer = value; }
        }

        /// <summary>
        /// Gets CloseReason.
        /// </summary>
        public ShutdownEventArgs CloseReason
        {
            get { return this.target.CloseReason; }
        }

        /// <summary>
        /// Gets a value indicating whether IsOpen.
        /// </summary>
        public bool IsOpen
        {
            get { return this.target.IsOpen; }
        }

        /// <summary>
        /// Gets NextPublishSeqNo.
        /// </summary>
        public ulong NextPublishSeqNo
        {
            get { return this.target.NextPublishSeqNo; }
        }

        /// <summary>
        /// Model shutdown event handler.
        /// </summary>
        public event ModelShutdownEventHandler ModelShutdown
        {
            add
            {
                this.ModelShutdown += value;
            }
            remove
            {
                this.ModelShutdown -= value;
            }
        }

        /// <summary>
        /// Basic return event handler.
        /// </summary>
        public event BasicReturnEventHandler BasicReturn
        {
            add
            {
                this.BasicReturn += value;
            }
            remove
            {
                this.BasicReturn -= value;
            }
        }

        /// <summary>
        /// Basic ack event handler.
        /// </summary>
        public event BasicAckEventHandler BasicAcks;

        /// <summary>
        /// Basic nack event handler.
        /// </summary>
        public event BasicNackEventHandler BasicNacks;

        /// <summary>
        /// Callback exception event handler.
        /// </summary>
        public event CallbackExceptionEventHandler CallbackException
        {
            add
            {
                this.CallbackException += value;
            }
            remove 
            {
                this.CallbackException -= value;
            }
        }

        /// <summary>
        /// Flow control event handler.
        /// </summary>
        public event FlowControlEventHandler FlowControl;

        /// <summary>
        /// Basic recover ok event handler.
        /// </summary>
        public event BasicRecoverOkEventHandler BasicRecoverOk;

        #endregion

        /// <summary>
        /// Dispose action.
        /// </summary>
        public void Dispose()
        {
            this.InvokeActionWithReconnect(() => this.target.Dispose());
        }
    }
}