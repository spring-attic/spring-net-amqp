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
using System.Collections.Generic;
using System.IO;
using Common.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Threading;
using Spring.Threading.Execution;
using Spring.Transaction.Support;
using Spring.Util;

#endregion

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class SimpleMessageListenerContainer : AbstractMessageListenerContainer
    {
        #region Logging

        private readonly ILog logger = LogManager.GetLogger(typeof (SimpleMessageListenerContainer));

        #endregion

        private volatile ushort prefetchCount = 1;

        //TODO compare in more detail to spring 3.0's SimpleAsyncTaskExecutor
        private volatile IExecutorService taskExector = Executors.NewCachedThreadPool();

        private volatile int concurrentConsumers = 1;

        private volatile int blockingQueueConsumerCapacity = -1; //implied unlimited capacity

        //TODO would prefer to use generic ISet, not in Spring.Collections..
        private volatile IList<IModel> channels;

        private volatile IList<QueueingBasicConsumer> consumers;

        private readonly object consumersMonitor = new object();


        /// <summary>
        /// Sets the number of concurrent consumers to create.  Default is 1.
        /// </summary>
        /// <remarks>
        /// Raising the number of concurrent consumers is recommended in order
        /// to scale the consumption of messages coming in from a queue. However,
        /// note that any ordering guarantees are lost once multiple consumers are
        /// registered. In general, stick with 1 consumer for low-volume queues.
        /// </remarks>
        /// <value>The concurrent consumers.</value>
        public int ConcurrentConsumers
        {
            set
            {
                AssertUtils.IsTrue(value > 0, "'concurrentConsumers' value must be at least 1 (one)");
                concurrentConsumers = value;
            }
        }

        public IExecutorService TaskExector
        {
            set
            {
                AssertUtils.ArgumentNotNull(value, "TaskExecutor");
                taskExector = value;
            }
        }

        public ushort PrefetchCount
        {
            get { return prefetchCount; }
            set { prefetchCount = value; }
        }

        public int BlockingQueueConsumerCapacity
        {
            //TODO - need to change from QueueingBasicConsumer to support blocking
            get { return blockingQueueConsumerCapacity; }
            set { blockingQueueConsumerCapacity = value; }
        }

        #region Overrides of AbstractRabbitListeningContainer

        protected override bool SharedConnectionEnabled
        {
            get { return true; }
        }

        /// <summary>
        /// Creates the specified number of concurrent consumers, in the from of a Rabbit Channel
        /// plus associated MessageConsumer
        /// process.
        /// </summary>
        protected override void DoInitialize()
        {
            EstablishSharedConnection();
            InitializeConsumers();
        }

        protected override void DoStart()
        {
            base.DoStart();
            InitializeConsumers();
            foreach (QueueingBasicConsumer consumer in consumers)
            {
                this.taskExector.Execute(new AsyncMessageProcessingConsumer(consumer, this));
            }
        }

        protected override void DoShutdown()
        {        
            taskExector.Shutdown();
            if (!this.IsActive)
            {
                return;
            }
            logger.Debug("Closing Rabbit Consumers");
            foreach (QueueingBasicConsumer consumer in consumers)
            {
                RabbitUtils.CloseMessageConsumer(consumer.Model, consumer.ConsumerTag);
            }
            logger.Debug("Closing Rabbit Channels");
            foreach (IModel channel in channels)
            {
                RabbitUtils.CloseChannel(channel);
            }
        }

        #endregion

        private void InitializeConsumers()
        {
            lock (consumersMonitor)
            {
                if (this.consumers == null)
                {
                    this.channels = new List<IModel>(this.concurrentConsumers);
                    this.consumers = new List<QueueingBasicConsumer>(this.concurrentConsumers);
                    IConnection con = SharedConnection;
                    for (int i = 0; i < this.concurrentConsumers; i++)
                    {
                        IModel channel = this.CreateChannel(con);
                        if (this.IsChannelLocallyTransacted(channel))
                        {
                            channel.TxSelect();
                        }
                        QueueingBasicConsumer consumer = CreateBasicConsumer(channel);
                        this.channels.Add(channel);
                        this.consumers.Add(consumer);
                    }
                }
            }
        }

        private QueueingBasicConsumer CreateBasicConsumer(IModel channel)
        {
            QueueingBasicConsumer consumer;
            if (this.blockingQueueConsumerCapacity <= 0)
            {
                consumer = new QueueingBasicConsumer(channel);
            }
            else
            {
                throw new NotImplementedException("need to implemented BlockingQueue consumer");
            }
            // Set basicQoS before calling basicConsume
            channel.BasicQos(0, this.prefetchCount, false);
            string queueNames = RequiredQueueName();

            string[] queue = StringUtils.CommaDelimitedListToStringArray(queueNames);
            foreach (string name in queue)
            {
                if (!name.StartsWith("amq."))
                {              
                    //TODO look into declare passive
                    channel.QueueDeclare(name);
                }
                string consumerTag = channel.BasicConsume(name, autoAck, null, consumer);
                consumer.ConsumerTag = consumerTag;   
            }          
            return consumer;
        }

        public void ProcessMessage(Message message, IModel channel)
        {
            bool exposeResource = this.ExposeListenerChannel;
            if (exposeResource)
            {
                TransactionSynchronizationManager.BindResource(ConnectionFactory,
                                                               new LocallyExposedRabbitResourceHolder(channel));
            }
            try
            {
                ExecuteListener(channel, message);
            }
            finally
            {
                if (exposeResource)
                {
                    TransactionSynchronizationManager.UnbindResource(ConnectionFactory);
                }
            }
        }
    }

    public class AsyncMessageProcessingConsumer : IRunnable
    {
        #region Logging

        private readonly ILog logger = LogManager.GetLogger(typeof (AsyncMessageProcessingConsumer));

        #endregion

        private QueueingBasicConsumer consumer;
        private SimpleMessageListenerContainer messageListenerContainer;

        public AsyncMessageProcessingConsumer(QueueingBasicConsumer consumer,
                                              SimpleMessageListenerContainer messageListenerContainer)
        {
            this.consumer = consumer;
            this.messageListenerContainer = messageListenerContainer;
        }

        #region Implementation of IRunnable

        public void Run()
        {
            while (true)
            {
                IModel channel = consumer.Model;
                try
                {
                    BasicDeliverEventArgs delivery = (BasicDeliverEventArgs) consumer.Queue.Dequeue();
                    byte[] body = delivery.Body;
                    IMessageProperties messageProperties = new MessageProperties(delivery.BasicProperties,
                                                                                 delivery.Exchange,
                                                                                 delivery.RoutingKey,
                                                                                 delivery.Redelivered,
                                                                                 delivery.DeliveryTag, 0);

                    Message message = new Message(body, messageProperties);

                    messageListenerContainer.ProcessMessage(message, channel);
                }
                //TODO catch any other exception?  What of system signal for shutdown?
                catch (OperationInterruptedException ex)
                {
                    // The consumer was removed, either through
                    // channel or connection closure, or through the
                    // action of IModel.BasicCancel().
                    logger.Debug("Consumer thread interrupted, processing stopped.");
                    break;
                }
                catch (EndOfStreamException ex)
                {
                    // The consumer was cancelled, the model closed, or the
                    // connection went away.
                    logger.Debug("Consumer was cancelled, processing stopped.");
                    break;
                }
            }
        }

        #endregion
    }
}