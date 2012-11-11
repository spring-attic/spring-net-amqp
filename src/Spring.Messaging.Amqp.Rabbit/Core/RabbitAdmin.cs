// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitAdmin.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Context;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using Spring.Objects.Factory;
using Spring.Util;
using IConnection = Spring.Messaging.Amqp.Rabbit.Connection.IConnection;
using Queue = Spring.Messaging.Amqp.Core.Queue;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// RabbitMQ implementation of portable AMQP administrative operations for AMQP >= 0.8
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class RabbitAdmin : IAmqpAdmin, IApplicationContextAware, IInitializingObject
    {
        public static readonly string DEFAULT_EXCHANGE_NAME = string.Empty;

        /// <summary>
        /// The Logger.
        /// </summary>
        protected static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The rabbit template.
        /// </summary>
        private readonly RabbitTemplate rabbitTemplate;

        /// <summary>
        /// The running flag.
        /// </summary>
        private volatile bool running;

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
        private readonly IConnectionFactory connectionFactory;

        /// <summary>Initializes a new instance of the <see cref="RabbitAdmin"/> class.</summary>
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
        public bool AutoStartup { get { return this.autoStartup; } set { this.autoStartup = value; } }

        /// <summary>
        /// Sets ApplicationContext.
        /// </summary>
        public IApplicationContext ApplicationContext { set { this.applicationContext = value; } }

        /// <summary>
        /// Gets RabbitTemplate.
        /// </summary>
        public RabbitTemplate RabbitTemplate { get { return this.rabbitTemplate; } }

        #region Implementation of IAmqpAdmin

        /// <summary>Declares the exchange.</summary>
        /// <param name="exchange">The exchange.</param>
        public void DeclareExchange(IExchange exchange)
        {
            this.rabbitTemplate.Execute<object>(
                channel =>
                {
                    this.DeclareExchanges(channel, exchange);
                    return null;
                });
        }

        /// <summary>Deletes the exchange.</summary>
        /// <remarks>Look at implementation specific subclass for implementation specific behavior, for example
        /// for RabbitMQ this will delete the exchange without regard for whether it is in use or not.</remarks>
        /// <param name="exchangeName">Name of the exchange.</param>
        /// <returns>The result of deleting the exchange.</returns>
        public bool DeleteExchange(string exchangeName)
        {
            return this.rabbitTemplate.Execute(
                channel =>
                {
                    if (this.IsDeletingDefaultExchange(exchangeName))
                    {
                        return true;
                    }

                    try
                    {
                        channel.ExchangeDelete(exchangeName, false);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(m => m("Could not delete exchange."), e);
                        return false;
                    }

                    return true;
                });
        }

        /// <summary>Declares the queue.</summary>
        /// <param name="queue">The queue.</param>
        public void DeclareQueue(Queue queue)
        {
            this.rabbitTemplate.Execute<object>(
                channel =>
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
            var queue = new Queue(queueName.QueueName, false, true, true);
            return queue;
        }

        /// <summary>Deletes the queue, without regard for whether it is in use or has messages on it </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <returns>The result of deleting the queue.</returns>
        public bool DeleteQueue(string queueName)
        {
            return this.rabbitTemplate.Execute(
                channel =>
                {
                    try
                    {
                        channel.QueueDelete(queueName);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(m => m("Could not delete queue."), e);
                        return false;
                    }

                    return true;
                });
        }

        /// <summary>Deletes the queue.</summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="unused">if set to <c>true</c> the queue should be deleted only if not in use.</param>
        /// <param name="empty">if set to <c>true</c> the queue should be deleted only if empty.</param>
        public void DeleteQueue(string queueName, bool unused, bool empty)
        {
            this.rabbitTemplate.Execute<object>(
                channel =>
                {
                    channel.QueueDelete(queueName, unused, empty);
                    return null;
                });
        }

        /// <summary>Purges the queue.</summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="noWait">if set to <c>true</c> [no wait].</param>
        public void PurgeQueue(string queueName, bool noWait)
        {
            this.rabbitTemplate.Execute<object>(
                channel =>
                {
                    channel.QueuePurge(queueName);
                    return null;
                });
        }

        /// <summary>Declare the binding.</summary>
        /// <param name="binding">The binding.</param>
        public void DeclareBinding(Binding binding)
        {
            this.rabbitTemplate.Execute<object>(
                channel =>
                {
                    this.DeclareBindings(channel, binding);
                    return null;
                });
        }

        /// <summary>Remove a binding of a queue to an exchange.</summary>
        /// <param name="binding">Binding to remove.</param>
        public void RemoveBinding(Binding binding)
        {
            this.rabbitTemplate.Execute<object>(
                channel =>
                {
                    if (binding.IsDestinationQueue())
                    {
                        if (this.IsRemovingImplicitQueueBinding(binding))
                        {
                            return null;
                        }

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
        public void AfterPropertiesSet()
        {
            lock (this.lifecycleMonitor)
            {
                if (this.running || !this.autoStartup)
                {
                    return;
                }

                this.connectionFactory.AddConnectionListener(new AdminConnectionListener(this));

                this.running = true;
            }
        }

        /// <summary>
        /// Declares all the exchanges, queues and bindings in the enclosing application context, if any. It should be safe
        /// (but unnecessary) to call this method more than once.
        /// </summary>
        public void Initialize()
        {
            if (this.applicationContext == null)
            {
                Logger.Debug(m => m("no ApplicationContext has been set, cannot auto-declare Exchanges, Queues, and Bindings"));
                return;
            }

            Logger.Debug(m => m("Initializing declarations"));
            var exchanges = this.applicationContext.GetObjectsOfType(typeof(IExchange)).Values;
            var queues = this.applicationContext.GetObjectsOfType(typeof(Queue)).Values;
            var bindings = this.applicationContext.GetObjectsOfType(typeof(Binding)).Values;

            foreach (IExchange exchange in exchanges)
            {
                if (!exchange.Durable)
                {
                    Logger.Warn(m => m("Auto-declaring a non-durable Exchange ({0}). It will be deleted by the broker if it shuts down, and can be redeclared by closing and reopening the connection.", exchange.Name));
                }

                if (exchange.AutoDelete)
                {
                    Logger.Warn(
                        m =>
                        m("Auto-declaring an auto-delete Exchange ({0}). It will be deleted by the broker if not in use (if all bindings are deleted), but will only be redeclared if the connection is closed and reopened.", exchange.Name));
                }
            }

            foreach (Queue queue in queues)
            {
                if (!queue.Durable)
                {
                    Logger.Warn(m => m("Auto-declaring a non-durable Queue ({0}). It will be redeclared if the broker stops and is restarted while the connection factory is alive, but all messages will be lost.", queue.Name));
                }

                if (queue.AutoDelete)
                {
                    Logger.Warn(m => m("Auto-declaring an auto-delete Queue ({0}). It will be deleted by the broker if not in use, and all messages will be lost.  Redeclared when the connection is closed and reopened.", queue.Name));
                }

                if (queue.Exclusive)
                {
                    Logger.Warn(m => m("Auto-declaring an exclusive Queue ({0}). It cannot be accessed by consumers on another connection, and will be redeclared if the connection is reopened.", queue.Name));
                }
            }

            this.rabbitTemplate.Execute<object>(
                channel =>
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

            Logger.Debug(m => m("Declarations finished"));
        }

        #endregion

        /// <summary>Declare the exchanges.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="exchanges">The exchanges.</param>
        private void DeclareExchanges(IModel channel, params IExchange[] exchanges)
        {
            foreach (var exchange in exchanges)
            {
                Logger.Debug(m => m("declaring Exchange '{0}'", exchange.Name));
                if (!this.IsDeclaringDefaultExchange(exchange))
                {
                    channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable, exchange.AutoDelete, (IDictionary)exchange.Arguments);
                }
            }
        }

        /// <summary>Declare the queues.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="queues">The queues.</param>
        private void DeclareQueues(IModel channel, params Queue[] queues)
        {
            foreach (var queue in queues)
            {
                if (!queue.Name.StartsWith("amq."))
                {
                    Logger.Debug(m => m("Declaring Queue '{0}'", queue.Name));
                    channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments);
                }
                else if (Logger.IsDebugEnabled)
                {
                    Logger.Debug(m => m("Queue with name that starts with 'amq.' cannot be declared."));
                }
            }
        }

        /// <summary>Declare the bindings.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="bindings">The bindings.</param>
        private void DeclareBindings(IModel channel, params Binding[] bindings)
        {
            foreach (var binding in bindings)
            {
                Logger.Debug(m => m("Binding destination [{0} ({1})] to exchange [{2}] with routing key [{3}]", binding.Destination, binding.BindingDestinationType, binding.Exchange, binding.RoutingKey));

                if (binding.IsDestinationQueue())
                {
                    if (!this.IsDeclaringImplicitQueueBinding(binding))
                    {
                        channel.QueueBind(binding.Destination, binding.Exchange, binding.RoutingKey, binding.Arguments);
                    }
                }
                else
                {
                    channel.ExchangeBind(binding.Destination, binding.Exchange, binding.RoutingKey, binding.Arguments);
                }
            }
        }

        private bool IsDeclaringDefaultExchange(IExchange exchange)
        {
            if (this.IsDefaultExchange(exchange.Name))
            {
                Logger.Debug(m => m("Default exchange is pre-declared by server."));
                return true;
            }

            return false;
        }

        private bool IsDeletingDefaultExchange(string exchangeName)
        {
            if (this.IsDefaultExchange(exchangeName))
            {
                Logger.Debug(m => m("Default exchange cannot be deleted."));
                return true;
            }

            return false;
        }

        private bool IsDefaultExchange(string exchangeName) { return DEFAULT_EXCHANGE_NAME.Equals(exchangeName); }

        private bool IsDeclaringImplicitQueueBinding(Binding binding)
        {
            if (this.IsImplicitQueueBinding(binding))
            {
                Logger.Debug(m => m("The default exchange is implicitly bound to every queue, with a routing key equal to the queue name."));
                return true;
            }

            return false;
        }

        private bool IsRemovingImplicitQueueBinding(Binding binding)
        {
            if (this.IsImplicitQueueBinding(binding))
            {
                Logger.Debug(m => m("Cannot remove implicit default exchange binding to queue."));
                return true;
            }

            return false;
        }

        private bool IsImplicitQueueBinding(Binding binding) { return this.IsDefaultExchange(binding.Exchange) && binding.Destination.Equals(binding.RoutingKey); }
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
        private readonly AtomicBoolean initializing = new AtomicBoolean(false);

        /// <summary>Initializes a new instance of the <see cref="AdminConnectionListener"/> class.</summary>
        /// <param name="outer">The outer.</param>
        public AdminConnectionListener(RabbitAdmin outer) { this.outer = outer; }

        /// <summary>Actions to perform on create.</summary>
        /// <param name="connection">The connection.</param>
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

        /// <summary>Actions to perform on close.</summary>
        /// <param name="connection">The connection.</param>
        public void OnClose(IConnection connection) { }
    }
}
