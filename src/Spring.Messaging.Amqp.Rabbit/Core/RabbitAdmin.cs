
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
using Spring.Context;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Objects.Factory;
using Spring.Threading.AtomicTypes;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// RabbitMQ implementation of portable AMQP administrative operations for AMQP >= 0.8
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald</author>
    public class RabbitAdmin : IAmqpAdmin, IInitializingObject
    {
        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILog logger = LogManager.GetLogger(typeof(RabbitAdmin));

        /// <summary>
        /// The rabbit template.
        /// </summary>
        private RabbitTemplate rabbitTemplate;

        /// <summary>
        /// The running flag.
        /// </summary>
        private volatile bool running = false;

        /// <summary>
        /// The auto startup flag.
        /// </summary>
        private volatile bool autoStartup = true;

        /// <summary>
        /// The application context.
        /// </summary>
        private volatile IApplicationContext applicationContext;

        /// <summary>
        /// The lifecycle monitor.
        /// </summary>
        private readonly object lifecycleMonitor = new object();

        /// <summary>
        /// The connection factory.
        /// </summary>
        private IConnectionFactory connectionFactory;

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
        /// Sets a value indicating whether AutoStartup.
        /// </summary>
        public bool AutoStartup
        {
            get { return this.autoStartup; }
            set { this.autoStartup = value; }
        }

        /// <summary>
        /// Sets ApplicationContext.
        /// </summary>
        public IApplicationContext ApplicationContext
        {
            set { this.applicationContext = value; }
        }

        /// <summary>
        /// Gets RabbitTemplate.
        /// </summary>
        public RabbitTemplate RabbitTemplate
        {
            get { return this.rabbitTemplate; }
        }

        #region Implementation of IAmqpAdmin

        /// <summary>
        /// Declares the exchange.
        /// </summary>
        /// <param name="exchange">The exchange.</param>
        public void DeclareExchange(IExchange exchange)
        {
            this.rabbitTemplate.Execute<object>(delegate(RabbitMQ.Client.IModel channel)
            {
                this.DeclareExchanges(channel, exchange);
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
        /// <param name="exchangeName">
        /// Name of the exchange.
        /// </param>
        /// <returns>
        /// The result of deleting the exchange.
        /// </returns>
        public bool DeleteExchange(string exchangeName)
        {
            return this.rabbitTemplate.Execute(delegate(RabbitMQ.Client.IModel channel)
            {
                try
                {
                    channel.ExchangeDelete(exchangeName, false);
                }
                catch (Exception e)
                {
                    return false;
                }

                return true;
            });
        }

        /// <summary>
        /// Declares the queue.
        /// </summary>
        /// <param name="queue">The queue.</param>
        public void DeclareQueue(Queue queue)
        {
            this.rabbitTemplate.Execute<object>(delegate(RabbitMQ.Client.IModel channel)
            {
                this.DeclareQueues(channel, queue);
                return null;
            });
        }

        /// <summary>
        /// Declares a queue whose name is automatically named by the server.  It is created with
        /// exclusive = true, autoDelete=true, and durable = false.
        /// </summary>
        /// <returns>The queue.</returns>
        public Queue DeclareQueue()
        {
            var queueName = this.rabbitTemplate.Execute(channel => channel.QueueDeclare());
            var q = new Queue(queueName, true, true, false);
            return q;
        }


        /// <summary>
        /// Deletes the queue, without regard for whether it is in use or has messages on it 
        /// </summary>
        /// <param name="queueName">
        /// Name of the queue.
        /// </param>
        /// <returns>
        /// The result of deleting the queue.
        /// </returns>
        public bool DeleteQueue(string queueName)
        {
            return this.rabbitTemplate.Execute(delegate(RabbitMQ.Client.IModel channel)
            {
                try
                {
                    channel.QueueDelete(queueName);
                }
                catch (Exception e)
                {
                    return false;
                }

                return true;
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
            this.rabbitTemplate.Execute<object>(delegate(RabbitMQ.Client.IModel channel)
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
            this.rabbitTemplate.Execute<object>(delegate(RabbitMQ.Client.IModel channel)
            {
                channel.QueuePurge(queueName);
                return null;
            });
        }

        /// <summary>
        /// Declare the binding.
        /// </summary>
        /// <param name="binding">
        /// The binding.
        /// </param>
        public void DeclareBinding(Binding binding)
        {
            this.rabbitTemplate.Execute<object>(delegate(RabbitMQ.Client.IModel channel)
            {
                this.DeclareBindings(channel, binding);
                return null;
            });
        }

        /// <summary>
        /// Remove a binding of a queue to an exchange.
        /// </summary>
        /// <param name="binding">Binding to remove.</param>
        public void RemoveBinding(Binding binding)
        {
            this.rabbitTemplate.Execute<object>(delegate(RabbitMQ.Client.IModel channel)
            {
                if (binding.IsDestinationQueue())
                {
                    channel.QueueUnbind(binding.Destination, binding.Exchange, binding.RoutingKey, binding.Arguments);
                }
                else
                {
                    channel.ExchangeUnbind(binding.Destination, binding.Exchange, binding.RoutingKey, binding.Arguments);
                }

                return null;
            });
        }

        #endregion

        #region Implementation of IInitializingObject

        /// <summary>
        /// Actions to perform after properties are set.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public void AfterPropertiesSet()
        {
            lock (this.lifecycleMonitor)
            {
                if (this.running || !this.autoStartup)
                {
                    return;
                }

                connectionFactory.AddConnectionListener(new AdminConnectionListener(this));

                this.running = true;
            }
            if (this.connectionFactory == null)
            {
                throw new InvalidOperationException("'ConnectionFactory' is required.");
            }
            this.rabbitTemplate = new RabbitTemplate(connectionFactory);
        }

        /// <summary>
        /// Declares all the exchanges, queues and bindings in the enclosing application context, if any. It should be safe
        /// (but unnecessary) to call this method more than once.
        /// </summary>
        public void Initialize()
        {
            if (this.applicationContext == null)
            {
                if (this.logger.IsDebugEnabled)
                {
                    this.logger.Debug("no ApplicationContext has been set, cannot auto-declare Exchanges, Queues, and Bindings");
                }
                return;
            }

            this.logger.Debug("Initializing declarations");
            var exchanges = this.applicationContext.GetObjectsOfType(typeof(IExchange)).Values;
            var queues = this.applicationContext.GetObjectsOfType(typeof(Queue)).Values;
            var bindings = this.applicationContext.GetObjectsOfType(typeof(Binding)).Values;

            foreach (IExchange exchange in exchanges)
            {
                if (!exchange.Durable)
                {
                    this.logger.Warn("Auto-declaring a non-durable Exchange (" + exchange.Name +
                                     "). It will be deleted by the broker if it shuts down, and can be redeclared by closing and reopening the connection.");
                }
                if (exchange.AutoDelete)
                {
                    this.logger.Warn("Auto-declaring an auto-delete Exchange ("
                                     + exchange.Name
                                     +
                                     "). It will be deleted by the broker if not in use (if all bindings are deleted), but will only be redeclared if the connection is closed and reopened.");
                }
            }

            foreach (Queue queue in queues)
            {
                if (!queue.Durable)
                {
                    this.logger.Warn("Auto-declaring a non-durable Queue ("
                            + queue.Name
                            + "). It will be redeclared if the broker stops and is restarted while the connection factory is alive, but all messages will be lost.");
                }

                if (queue.AutoDelete)
                {
                    this.logger.Warn("Auto-declaring an auto-delete Queue ("
                            + queue.Name
                            + "). It will be deleted deleted by the broker if not in use, and all messages will be lost.  Redeclared when the connection is closed and reopened.");
                }

                if (queue.Exclusive)
                {
                    this.logger.Warn("Auto-declaring an exclusive Queue ("
                            + queue.Name
                            + "). It cannot be accessed by consumers on another connection, and will be redeclared if the connection is reopened.");
                }
            }

            this.rabbitTemplate.Execute<object>(delegate(RabbitMQ.Client.IModel channel)
            {
                var exchangeArray = new IExchange[exchanges.Count];
                var queueArray = new Queue[queues.Count];
                var bindingArray = new Binding[bindings.Count];

                exchanges.CopyTo(exchangeArray, 0);
                queues.CopyTo(queueArray, 0);
                bindings.CopyTo(bindingArray, 0);

                this.DeclareExchanges(channel, exchangeArray);
                this.DeclareQueues(channel, queueArray);
                this.DeclareBindings(channel, bindingArray);
                return null;
            });

            this.logger.Debug("Declarations finished");
        }
        #endregion

        /// <summary>
        /// Declare the exchanges.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="exchanges">
        /// The exchanges.
        /// </param>
        private void DeclareExchanges(RabbitMQ.Client.IModel channel, params IExchange[] exchanges)
        {
            foreach (var exchange in exchanges)
            {
                if (this.logger.IsDebugEnabled)
                {
                    this.logger.Debug("declaring Exchange '" + exchange.Name + "'");
                }
                channel.ExchangeDeclare(exchange.Name, exchange.ExchangeType, exchange.Durable, exchange.AutoDelete, exchange.Arguments);
            }
        }

        /// <summary>
        /// Declare the queues.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="queues">
        /// The queues.
        /// </param>
        private void DeclareQueues(RabbitMQ.Client.IModel channel, params Queue[] queues)
        {
            foreach (var queue in queues)
            {
                if (!queue.Name.StartsWith("amq."))
                {
                    if (this.logger.IsDebugEnabled)
                    {
                        this.logger.Debug("Declaring Queue '" + queue.Name + "'");
                    }

                    channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments);
                }
                else if (this.logger.IsDebugEnabled)
                {
                    this.logger.Debug("Queue with name that starts with 'amq.' cannot be declared.");
                }
            }
        }

        /// <summary>
        /// Declare the bindings.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="bindings">
        /// The bindings.
        /// </param>
        private void DeclareBindings(RabbitMQ.Client.IModel channel, params Binding[] bindings)
        {
            foreach (var binding in bindings)
            {
                if (this.logger.IsDebugEnabled)
                {
                    this.logger.Debug("Binding destination [" + binding.Destination + " (" + binding.BindingDestinationType
                        + ")] to exchange [" + binding.Exchange + "] with routing key [" + binding.RoutingKey
                        + "]");
                }

                if (binding.IsDestinationQueue())
                {
                    channel.QueueBind(binding.Destination, binding.Exchange, binding.RoutingKey, binding.Arguments);
                }
                else
                {
                    channel.ExchangeBind(binding.Destination, binding.Exchange, binding.RoutingKey, binding.Arguments);
                }
            }
        }
    }

    /// <summary>
    /// An admin connection listener.
    /// </summary>
    internal class AdminConnectionListener : IConnectionListener
    {
        /// <summary>
        /// The outer rabbitadmin.
        /// </summary>
        private readonly RabbitAdmin outer;

        /// <summary>
        /// Prevent stack overflow...
        /// </summary>
        private AtomicBoolean initializing = new AtomicBoolean(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminConnectionListener"/> class.
        /// </summary>
        /// <param name="outer">
        /// The outer.
        /// </param>
        public AdminConnectionListener(RabbitAdmin outer)
        {
            this.outer = outer;
        }

        /// <summary>
        /// Actions to perform on create.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        public void OnCreate(IConnection connection)
        {
            if (!this.initializing.CompareAndSet(false, true))
            {
                // If we are already initializing, we don't need to do it again...
                return;
            }

            try
            {
                /*
                 * ...but it is possible for this to happen twice in the same ConnectionFactory (if more than
                 * one concurrent Connection is allowed). It's idempotent, so no big deal (a bit of network
                 * chatter). In fact it might even be a good thing: exclusive queues only make sense if they are
                 * declared for every connection. If anyone has a problem with it: use auto-startup="false".
                 */
                this.outer.Initialize();
            }
            finally
            {
                this.initializing.CompareAndSet(true, false);
            }
        }

        /// <summary>
        /// Actions to perform on close.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        public void OnClose(IConnection connection)
        {
        }
    }
}