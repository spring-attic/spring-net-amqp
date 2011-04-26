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
using Common.Logging;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Objects.Factory;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// RabbitMQ implementation of portable AMQP administrative operations for AMQP >= 0.8
    /// </summary>
    /// <author>Mark Pollack</author>
    public class RabbitAdmin : IAmqpAdmin, IInitializingObject
    {
        protected static readonly ILog logger = LogManager.GetLogger(typeof(RabbitAdmin));

        private IConnectionFactory connectionFactory;

        private RabbitTemplate rabbitTemplate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitAdmin"/> class.
        /// </summary>
        public RabbitAdmin()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitAdmin"/> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        public RabbitAdmin(IConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
            AssertUtils.ArgumentNotNull(connectionFactory, "ConnectionFactory is required");
            this.rabbitTemplate = new RabbitTemplate(connectionFactory);

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitAdmin"/> class.
        /// </summary>
        /// <param name="rabbitTemplate">The rabbit template.</param>
        public RabbitAdmin(RabbitTemplate rabbitTemplate)
        {
            this.rabbitTemplate = rabbitTemplate;
        }

        public RabbitTemplate RabbitTemplate
        {
            get { return rabbitTemplate; }
        }

        #region Implementation of IAmqpAdmin

        /// <summary>
        /// Declares the exchange.
        /// </summary>
        /// <param name="exchange">The exchange.</param>
        public void DeclareExchange(IExchange exchange)
        {
            rabbitTemplate.Execute<object>(delegate(IModel channel)
                                               {
                                                   channel.ExchangeDeclare(exchange.Name, exchange.ExchangeType.ToString().ToLower().Trim(), exchange.Durable, exchange.AutoDelete, exchange.Arguments);
                                                   return null;
                                               });
        }

        /// <summary>
        /// Deletes the exchange.
        /// </summary>
        /// <remarks>
        /// Look at implementation specific subclass for implementation specific behavior, for example
        /// for RabbitMQ this will delete the exchange without regard for whether it is in use or not.
        /// </remarks>
        /// <param name="exchangeName">Name of the exchange.</param>
        public void DeleteExchange(string exchangeName)
        {
            rabbitTemplate.Execute<object>(delegate(IModel channel)
            {
                // TODO: verify default settings
                channel.ExchangeDelete(exchangeName, false);
                return null;
            });
        }


        /// <summary>
        /// Declares the queue.
        /// </summary>
        /// <param name="queue">The queue.</param>
        public Queue DeclareQueue()
        {
            string queueName = rabbitTemplate.Execute<string>(delegate(IModel channel)
            {
                return channel.QueueDeclare();                
            });
            Queue q = new Queue(queueName);
            q.Exclusive = true;
            q.AutoDelete = true;
            q.Durable = false;
            return q;
        }

        /// <summary>
        /// Declares the queue.
        /// </summary>
        /// <param name="queue">The queue.</param>
        public void DeclareQueue(Queue queue)
        {
            rabbitTemplate.Execute<object>(delegate(IModel channel)
            {
                channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments);
                return null;
            });
        }

        /// <summary>
        /// Deletes the queue, without regard for whether it is in use or has messages on it 
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        public void DeleteQueue(string queueName)
        {
            rabbitTemplate.Execute<object>(delegate(IModel channel)
            {
                channel.QueueDelete(queueName, false, false);
                return null;
            });
        }

        /// <summary>
        /// Deletes the queue.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="unused">if set to <c>true</c> the queue should be deleted only if not in use.</param>
        /// <param name="empty">if set to <c>true</c> the queue should be deleted only if empty.</param>
        public void DeleteQueue(string queueName, bool unused, bool empty)
        {
            rabbitTemplate.Execute<object>(delegate(IModel channel)
            {
                channel.QueueDelete(queueName, unused, empty);
                return null;
            });
        }

        /// <summary>
        /// Purges the queue.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="noWait">if set to <c>true</c> [no wait].</param>
        public void PurgeQueue(string queueName, bool noWait)
        {
            rabbitTemplate.Execute<object>(delegate(IModel channel)
            {
                channel.QueuePurge(queueName);
                return null;
            });
        }

        public void DeclareBinding(Binding binding)
        {
            rabbitTemplate.Execute<object>(delegate(IModel channel)
            {
                channel.QueueBind(binding.Queue, binding.Exchange, binding.RoutingKey, binding.Arguments);
                return null;
            });
        }

        #endregion

        #region Implementation of IInitializingObject

        public void AfterPropertiesSet()
        {
            if (connectionFactory == null)
            {
                throw new InvalidOperationException("'ConnectionFactory' is required.");
            }
            this.rabbitTemplate = new RabbitTemplate(connectionFactory);           
        }

        #endregion
    }

}