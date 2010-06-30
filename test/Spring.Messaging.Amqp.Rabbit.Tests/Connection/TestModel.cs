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
using System.Collections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class TestModel : IModel
    {
        private int closeCount;
        private int createdCount;


        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public TestModel()
        {
            createdCount++;
        }

        public int CloseCount
        {
            get { return closeCount; }
        }

        public int CreatedCount
        {
            get { return createdCount; }
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation of IModel

        public IBasicProperties CreateBasicProperties()
        {
            throw new NotImplementedException();
        }

        public IFileProperties CreateFileProperties()
        {
            throw new NotImplementedException();
        }

        public IStreamProperties CreateStreamProperties()
        {
            throw new NotImplementedException();
        }

        public void ChannelFlow(bool active)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDeclare(string exchange, string type, bool durable)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDeclare(string exchange, string type)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDeclare(string exchange, string type, bool passive, bool durable, bool autoDelete, bool @internal, bool nowait, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDelete(string exchange, bool ifUnused, bool nowait)
        {
            throw new NotImplementedException();
        }

        public string QueueDeclare()
        {
            throw new NotImplementedException();
        }

        public string QueueDeclare(string queue)
        {
            throw new NotImplementedException();
        }

        public string QueueDeclare(string queue, bool durable)
        {
            throw new NotImplementedException();
        }

        public string QueueDeclare(string queue, bool passive, bool durable, bool exclusive, bool autoDelete, bool nowait, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public void QueueBind(string queue, string exchange, string routingKey, bool nowait, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public uint QueuePurge(string queue, bool nowait)
        {
            throw new NotImplementedException();
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty, bool nowait)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, IDictionary filter, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool noAck, IDictionary filter, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary filter, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, bool noLocal, bool exclusive, IDictionary filter, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }

        public void BasicCancel(string consumerTag)
        {
            throw new NotImplementedException();
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            throw new NotImplementedException();
        }

        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body)
        {
            throw new NotImplementedException();
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            throw new NotImplementedException();
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties, byte[] body)
        {
            throw new NotImplementedException();
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            throw new NotImplementedException();
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            throw new NotImplementedException();
        }

        public void BasicRecover(bool requeue)
        {
            throw new NotImplementedException();
        }

        public BasicGetResult BasicGet(string queue, bool noAck)
        {
            throw new NotImplementedException();
        }

        public void TxSelect()
        {
            throw new NotImplementedException();
        }

        public void TxCommit()
        {
            throw new NotImplementedException();
        }

        public void TxRollback()
        {
            throw new NotImplementedException();
        }

        public void DtxSelect()
        {
            throw new NotImplementedException();
        }

        public void DtxStart(string dtxIdentifier)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            this.closeCount++;
        }

        public void Close(ushort replyCode, string replyText)
        {
            this.closeCount++;
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public void Abort(ushort replyCode, string replyText)
        {
            throw new NotImplementedException();
        }

        public ShutdownEventArgs CloseReason
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsOpen
        {
            get { throw new NotImplementedException(); }
        }

        public event ModelShutdownEventHandler ModelShutdown;
        public event BasicReturnEventHandler BasicReturn;
        public event CallbackExceptionEventHandler CallbackException;

        #endregion
    }

}