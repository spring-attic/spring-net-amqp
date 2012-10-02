
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
using System.Threading;
using AopAlliance.Aop;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Aop;
using Spring.Aop.Framework;
using Spring.Aop.Support;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Transaction;
using Spring.Transaction.Interceptor;
using Spring.Transaction.Support;
using Spring.Util;

#endregion

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    using System.Threading.Tasks;

    using Spring.Messaging.Amqp.Rabbit.Support;

    /// <summary>
    /// A simple message listener container.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class SimpleMessageListenerContainer : AbstractMessageListenerContainer, IContainerDelegate
    {
        #region Logging

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly new ILog logger = LogManager.GetLogger(typeof(SimpleMessageListenerContainer));

        #endregion

        #region Private Fields

        /// <summary>
        /// The default receive timeout.
        /// </summary>
        public static readonly long DEFAULT_RECEIVE_TIMEOUT = 1000;

        /// <summary>
        /// The default prefetch count.
        /// </summary>
        public static readonly int DEFAULT_PREFETCH_COUNT = 1;

        /// <summary>
        /// The default shutdown timeout.
        /// </summary>
        public static readonly long DEFAULT_SHUTDOWN_TIMEOUT = 5000;

        /// <summary>
        /// The default recovery interval. 5000 ms = 5 seconds.
        /// </summary>
        public static readonly long DEFAULT_RECOVERY_INTERVAL = 5000;

        /// <summary>
        /// The prefetch count.
        /// </summary>
        private volatile int prefetchCount = DEFAULT_PREFETCH_COUNT;

        /// <summary>
        /// The transaction size.
        /// </summary>
        private volatile int txSize = 1;

        /// <summary>
        /// The task executor.
        /// </summary>
        // private volatile IExecutorService taskExecutor = Executors.NewCachedThreadPool();

        /// <summary>
        /// The concurrent consumers.
        /// </summary>
        private volatile int concurrentConsumers = 1;

        /// <summary>
        /// The receive timeout.
        /// </summary>
        private long receiveTimeout = DEFAULT_RECEIVE_TIMEOUT;

        /// <summary>
        /// The default shutdown timeout.
        /// </summary>
        private long shutdownTimeout = DEFAULT_SHUTDOWN_TIMEOUT;

        /// <summary>
        /// The default recovery interval.
        /// </summary>
        internal long recoveryInterval = DEFAULT_RECOVERY_INTERVAL;

        /// <summary>
        /// The consumers.
        /// </summary>
        private IList<BlockingQueueConsumer> consumers;

        /// <summary>
        /// The consumers monitor.
        /// </summary>
        private readonly object consumersMonitor = new object();

        /// <summary>
        /// The transaction manager.
        /// </summary>
        private IPlatformTransactionManager transactionManager;

        /// <summary>
        /// The transaction attribute.
        /// </summary>
        private ITransactionAttribute transactionAttribute = new DefaultTransactionAttribute();

        /// <summary>
        /// The advice chain.
        /// </summary>
        private volatile IAdvice[] adviceChain = new IAdvice[0];

        /// <summary>
        /// The cancellation lock.
        /// </summary>
        private ActiveObjectCounter<BlockingQueueConsumer> cancellationLock = new ActiveObjectCounter<BlockingQueueConsumer>();

        /// <summary>
        /// The message properties converter.
        /// </summary>
        private volatile IMessagePropertiesConverter messagePropertiesConverter = new DefaultMessagePropertiesConverter();

        private ContainerDelegate containerDelegate;

        /// <summary>
        /// The proxy.
        /// </summary>
        internal IContainerDelegate proxy;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleMessageListenerContainer"/> class.
        /// </summary>
        public SimpleMessageListenerContainer()
        {
            this.containerDelegate = new ContainerDelegate(this);
            this.proxy = this.containerDelegate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleMessageListenerContainer"/> class.
        /// </summary>
        /// <param name="connectionFactory">
        /// The connection factory.
        /// </param>
        public SimpleMessageListenerContainer(IConnectionFactory connectionFactory)
        {
            this.ConnectionFactory = connectionFactory;
            this.containerDelegate = new ContainerDelegate(this);
            this.proxy = this.containerDelegate;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Sets the advice chain.
        /// </summary>
        /// <value>The advice chain.</value>
        public IAdvice[] AdviceChain
        {
            set { this.adviceChain = value; }
        }

        /// <summary>
        /// Sets RecoveryInterval.
        /// </summary>
        /// <value>The recovery interval.</value>
        public long RecoveryInterval
        {
            set { this.recoveryInterval = value; }
        }

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
                this.concurrentConsumers = value;
            }
        }

        /// <summary>
        /// Sets ReceiveTimeout.
        /// </summary>
        public long ReceiveTimeout
        {
            set { this.receiveTimeout = value; }
        }

        /// <summary>
        /// Sets ShutdownTimeout.
        /// </summary>
        public long ShutdownTimeout
        {
            set { this.shutdownTimeout = value; }
        }

        /*
        /// <summary>
        /// Sets TaskExecutor.
        /// </summary>
        public IExecutorService TaskExecutor
        {
            set
            {
                AssertUtils.ArgumentNotNull(value, "TaskExecutor");
                this.taskExecutor = value;
            }
        }
        */

        /// <summary>
        /// Sets PrefetchCount.
        /// </summary>
        public int PrefetchCount
        {
            set { this.prefetchCount = value; }
        }

        /// <summary>
        /// Sets TxSize.
        /// </summary>
        public int TxSize
        {
            set { this.txSize = value; }
        }

        /// <summary>
        /// Sets TransactionManager.
        /// </summary>
        public IPlatformTransactionManager TransactionManager
        {
            set { this.transactionManager = value; }
        }

        /// <summary>
        /// Sets TransactionAttribute.
        /// </summary>
        public ITransactionAttribute TransactionAttribute
        {
            set { this.transactionAttribute = value; }
        }

        public IMessagePropertiesConverter MessagePropertiesConverter
        {
            set
            {
                AssertUtils.ArgumentNotNull(value, "messagePropertiesConverter must not be null");
                this.messagePropertiesConverter = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether SharedConnectionEnabled.
        /// </summary>
        public bool SharedConnectionEnabled
        {
            get { return true; }
        }

        /// <summary>
        /// Gets ActiveConsumerCount.
        /// </summary>
        public int ActiveConsumerCount
        {
            get { return this.cancellationLock.GetCount(); }
        }

        #endregion

        /// <summary>
        /// Validate the configuration.
        /// </summary>
        protected override void ValidateConfiguration()
        {
            base.ValidateConfiguration();

            AssertUtils.State(
                !(this.AcknowledgeMode.IsAutoAck() && this.transactionManager != null),
                "The acknowledgeMode is None (autoack in Rabbit terms) which is not consistent with having an " + "external transaction manager. Either use a different AcknowledgeMode or make sure the transactionManager is null.");

            if (typeof(CachingConnectionFactory).IsInstanceOfType(this.ConnectionFactory))
            {
                var cf = (CachingConnectionFactory)this.ConnectionFactory;
                if (cf.ChannelCacheSize < this.concurrentConsumers)
                {
                    cf.ChannelCacheSize = this.concurrentConsumers;
                    this.logger.Warn("CachingConnectionFactory's channelCacheSize can not be less than the number of concurrentConsumers so it was reset to match: " + this.concurrentConsumers);
                }
            }
        }

        /// <summary>
        /// Initialize the proxy.
        /// </summary>
        private void InitializeProxy()
        {
            if (this.adviceChain.Length == 0)
            {
                return;
            }

            var factory = new ProxyFactory();
            foreach (var advice in this.adviceChain)
            {
                factory.AddAdvisor(new DefaultPointcutAdvisor(TruePointcut.True, advice));
            }

            factory.ProxyTargetType = false;
            factory.AddInterface(typeof(IContainerDelegate));

            factory.Target = this.containerDelegate;
            this.proxy = (IContainerDelegate)factory.GetProxy();
        }

        /// <summary>
        /// Creates the specified number of concurrent consumers, in the from of a Rabbit Channel
        /// plus associated MessageConsumer
        /// process.
        /// </summary>
        protected override void DoInitialize()
        {
            this.InitializeProxy();
        }

        /// <summary>
        /// Perform start actions.
        /// </summary>
        protected override void DoStart()
        {
            base.DoStart();
            lock (this.consumersMonitor)
            {
                this.InitializeConsumers();

                if (this.consumers == null)
                {
                    this.logger.Info("Consumers were initialized and then cleared (presumably the container was stopped concurrently)");
                    return;
                }

                var processors = new List<AsyncMessageProcessingConsumer>();

                foreach (var consumer in this.consumers)
                {
                    var processor = new AsyncMessageProcessingConsumer(consumer, this);
                    processors.Add(processor);
                    var taskExecutor = new Task(processor.Run);
                    taskExecutor.Start();

                    // Old Spring.Threading way: this.taskExecutor.Execute(processor);
                }

                foreach (var processor in processors)
                {
                    var startupException = processor.GetStartupException();
                    if (startupException != null)
                    {
                        throw new AmqpIllegalStateException("Fatal exception on listener startup", startupException);
                    }
                }
            }
        }

        /// <summary>
        /// Perform stop actions.
        /// </summary>
        protected override void DoStop()
        {
            Shutdown();
            base.DoStop();
        }

        /// <summary>
        /// Perform shutdown actions.
        /// </summary>
        protected override void DoShutdown()
        {
            if (!this.IsRunning)
            {
                return;
            }

            try
            {
                this.logger.Info("Waiting for workers to finish.");
                var finished = this.cancellationLock.Await(new TimeSpan(this.shutdownTimeout*10000));
                this.logger.Info(finished ? "Successfully waited for workers to finish." : "Workers not finished.  Forcing connections to close.");
            }
            catch (ThreadInterruptedException e)
            {
                Thread.CurrentThread.Interrupt();
                this.logger.Warn("Interrupted waiting for workers.  Continuing with shutdown.");
            }
            catch (Exception ex)
            {
                this.logger.Error("Error occurred shutting down workers.", ex);
            }

            lock (this.consumersMonitor)
            {
                this.consumers = null;
            }
        }

        /// <summary>
        /// Initialize the consumers.
        /// </summary>
        private void InitializeConsumers()
        {
            lock (this.consumersMonitor)
            {
                if (this.consumers == null)
                {
                    this.cancellationLock.Dispose();
                    this.consumers = new List<BlockingQueueConsumer>();
                    for (var i = 0; i < this.concurrentConsumers; i++)
                    {
                        var consumer = this.CreateBlockingQueueConsumer();
                        this.consumers.Add(consumer);
                    }
                }
            }
        }

        /// <summary>
        /// Determine if channel is locally transacted.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <returns>
        /// True if locally transacted, else false.
        /// </returns>
        protected override bool IsChannelLocallyTransacted(IModel channel)
        {
            return base.IsChannelLocallyTransacted(channel) && this.transactionManager == null;
        }

        /// <summary>
        /// Create a blocking queue consumer.
        /// </summary>
        /// <returns>
        /// The blocking queue consumer.
        /// </returns>
        protected BlockingQueueConsumer CreateBlockingQueueConsumer()
        {
            // There's no point prefetching less than the tx size, otherwise the consumer will stall because the broker
            // didn't get an ack for delivered messages
            var actualPrefetchCount = this.prefetchCount > this.txSize ? this.prefetchCount : this.txSize;
            return new BlockingQueueConsumer(this.ConnectionFactory, this.messagePropertiesConverter, this.cancellationLock, this.AcknowledgeMode, this.ChannelTransacted, actualPrefetchCount, this.GetRequiredQueueNames());
        }

        /// <summary>
        /// Restart the consumer.
        /// </summary>
        /// <param name="consumer">
        /// The consumer.
        /// </param>
        internal void Restart(BlockingQueueConsumer consumer)
        {
            lock (this.consumersMonitor)
            {
                if (this.consumers != null)
                {
                    try
                    {
                        // Need to recycle the channel in this consumer
                        consumer.Stop();

                        // Ensure consumer counts are correct (another is not going
                        // to start because of the exception, but
                        // we haven't counted down yet)
                        this.cancellationLock.Release(consumer);
                        this.consumers.Remove(consumer);
                        consumer = this.CreateBlockingQueueConsumer();
                        this.consumers.Add(consumer);
                    }
                    catch (Exception e)
                    {
                        this.logger.Warn("Consumer failed irretrievably on restart. " + e.Source + ": " + e.Message);

                        // Re-throw and have it logged properly by the caller.
                        throw e;
                    }

                    var processor = new AsyncMessageProcessingConsumer(consumer, this);
                    var taskExecutor = new Task(processor.Run);

                    // Old way, using Spring.Threading: this.taskExecutor.Execute(new AsyncMessageProcessingConsumer(consumer, this));
                }
            }
        }

        /// <summary>
        /// Receive and execute.
        /// </summary>
        /// <param name="consumer">
        /// The consumer.
        /// </param>
        /// <returns>
        /// True if a message was received.
        /// </returns>
        internal bool ReceiveAndExecute(BlockingQueueConsumer consumer)
        {
            if (this.transactionManager != null)
            {
                try
                {
                    return (bool)new TransactionTemplate(this.transactionManager).Execute(delegate(ITransactionStatus status)
                                                                                     {
                                                                                         ConnectionFactoryUtils.BindResourceToTransaction(new RabbitResourceHolder(consumer.Channel), this.ConnectionFactory, true);
                                                                                         try
                                                                                         {
                                                                                             return this.DoReceiveAndExecute(consumer);
                                                                                         }
                                                                                         catch (Exception e)
                                                                                         {
                                                                                             throw;
                                                                                         }
                                                                                     });
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            return this.DoReceiveAndExecute(consumer);
        }

        /// <summary>
        /// Perform receive and execute actions.
        /// </summary>
        /// <param name="consumer">
        /// The consumer.
        /// </param>
        /// <returns>
        /// True if a message was received.
        /// </returns>
        private bool DoReceiveAndExecute(BlockingQueueConsumer consumer)
        {
            var channel = consumer.Channel;

            for (var i = 0; i < this.txSize; i++)
            {
                this.logger.Trace("Waiting for message from consumer.");
                var message = consumer.NextMessage(new TimeSpan(0, 0, 0, 0, (int)this.receiveTimeout));
                if (message == null)
                {
                    break;
                }

                try
                {
                    this.ExecuteListener(channel, message);
                }
                //TODO: this is caught its never thrown (related to need to port StatefulOperationRetryOperationsInterceptor...!)
                catch (ImmediateAcknowledgeAmqpException e)
                {
                    break;
                }
                catch (Exception ex)
                {
                    consumer.RollbackOnExceptionIfNecessary(channel, message, ex);
                    throw;
                }
            }

            return consumer.CommitIfNecessary(this.IsChannelLocallyTransacted(channel));
        }
    }

    /// <summary>
    /// An asynchronous message processing consumer.
    /// </summary>
    public class AsyncMessageProcessingConsumer
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(AsyncMessageProcessingConsumer));

        /// <summary>
        /// The consumer.
        /// </summary>
        private readonly BlockingQueueConsumer consumer;

        /// <summary>
        /// The start countdown latch.
        /// </summary>
        private readonly CountdownEvent start;

        /// <summary>
        /// The startup exception.
        /// </summary>
        private volatile FatalListenerStartupException startupException;

        /// <summary>
        /// The outer simple message listener container.
        /// </summary>
        private SimpleMessageListenerContainer outer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncMessageProcessingConsumer"/> class.
        /// </summary>
        /// <param name="consumer">
        /// The consumer.
        /// </param>
        /// <param name="outer">
        /// The outer.
        /// </param>
        public AsyncMessageProcessingConsumer(BlockingQueueConsumer consumer, SimpleMessageListenerContainer outer)
        {
            this.consumer = consumer;
            this.outer = outer;
            this.start = new CountdownEvent(1);
        }

        /// <summary>
        /// Retrieve the fatal startup exception if this processor completely failed to locate the broker resources it
        /// needed. Blocks up to 60 seconds waiting (but should always return promptly in normal circumstances).
        /// </summary>
        /// <returns>
        /// A startup exception if there was one.
        /// </returns>
        /// <exception cref="System.TimeoutException">
        /// </exception>
        public FatalListenerStartupException GetStartupException()
        {
            if (!this.start.Wait(new TimeSpan(0, 0, 0, 0, 60000)))
            {
                throw new System.TimeoutException("Timed out waiting for startup");
            }
            return this.startupException;
        }

        #region Implementation of IRunnable

        /// <summary>
        /// Execute Run.
        /// </summary>
        public void Run()
        {
            var aborted = false;

            try
            {
                try
                {
                    this.consumer.Start();
                    this.start.Signal();
                }
                catch (FatalListenerStartupException ex)
                {
                    throw;
                }
                catch (Exception t)
                {
                    if (start.CurrentCount > 0)
                    {
                        this.start.Signal();
                    }
                    this.HandleStartupFailure(t);
                    throw;
                }

                // Always better to stop receiving as soon as possible if
                // transactional
                var continuable = false;
                while (this.outer.IsActive || continuable)
                {
                    try
                    {
                        // Will come back false when the queue is drained
                        continuable = this.outer.ReceiveAndExecute(this.consumer) && !this.outer.ChannelTransacted;
                    }
                    catch (ListenerExecutionFailedException ex)
                    {
                        // Continue to process, otherwise re-throw
                    }
                }
            }
            catch (ThreadInterruptedException e)
            {
                this.logger.Debug("Consumer thread interrupted, processing stopped.");
                Thread.CurrentThread.Interrupt();
                aborted = true;
            }
            catch (FatalListenerStartupException ex)
            {
                this.logger.Error("Consumer received fatal exception on startup", ex);
                this.startupException = ex;

                // Fatal, but no point re-throwing, so just abort.
                aborted = true;
            }
            catch (FatalListenerExecutionException ex)
            {
                this.logger.Error("Consumer received fatal exception during processing", ex);

                // Fatal, but no point re-throwing, so just abort.
                aborted = true;
            }
            catch (Exception t)
            {
                if (this.logger.IsDebugEnabled)
                {
                    this.logger.Warn("Consumer raised exception, processing can restart if the connection factory supports it", t);
                }
                else
                {
                    this.logger.Warn("Consumer raised exception, processing can restart if the connection factory supports it. " + "Exception summary: " + t);
                }
            }

            // In all cases count down to allow container to progress beyond startup
            if (start.CurrentCount > 0)
            {
                this.start.Signal();
            }

            if (!this.outer.IsActive || aborted)
            {
                this.logger.Debug("Cancelling " + this.consumer);
                try
                {
                    this.consumer.Stop();
                }
                catch (AmqpException e)
                {
                    this.logger.Info("Could not cancel message consumer", e);
                }

                if (aborted)
                {
                    this.logger.Info("Stopping container from aborted consumer");
                    this.outer.Stop();
                }
            }
            else
            {
                this.logger.Info("Restarting " + this.consumer);
                this.outer.Restart(this.consumer);
            }
        }

        /// <summary>
        /// Invoke the listener.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        protected void InvokeListener(IModel channel, Message message)
        {
            this.outer.proxy.InvokeListener(channel, message);
        }

        /// <summary>
        /// Handle a startup failure.
        /// Wait for a period determined by the {@link #setRecoveryInterval(long) recoveryInterval} to give the container a
        /// chance to recover from consumer startup failure, e.g. if the broker is down.
        /// </summary>
        /// <param name="t">
        /// The t.
        /// </param>
        /// <exception cref="AmqpException">
        /// </exception>
        protected void HandleStartupFailure(Exception t)
        {
            try
            {
                var timeout = DateTime.Now.AddMilliseconds((double)this.outer.recoveryInterval);
                while (this.outer.IsActive && DateTime.Now < timeout)
                {
                    Thread.Sleep(200);
                }
            }
            catch (ThreadInterruptedException e)
            {
                Thread.CurrentThread.Interrupt();
                throw new AmqpException("Unrecoverable interruption on consumer restart");
            }
        }

        #endregion
    }

    /// <summary>
    /// An IContainerDelegate
    /// </summary>
    public interface IContainerDelegate
    {
        /// <summary>
        /// Invoke the listener.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="message">The message.</param>
        void InvokeListener(IModel channel, Message message);
    }

    /// <summary>
    /// A container delegate.
    /// </summary>
    internal class ContainerDelegate : IContainerDelegate
    {
        /// <summary>
        /// The outer simple message listener container.
        /// </summary>
        private SimpleMessageListenerContainer outer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerDelegate"/> class.
        /// </summary>
        /// <param name="outer">The outer.</param>
        public ContainerDelegate(SimpleMessageListenerContainer outer)
        {
            this.outer = outer;
        }

        /// <summary>
        /// Invoke the listener.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="message">The message.</param>
        public void InvokeListener(IModel channel, Message message)
        {
            this.outer.InvokeListener(channel, message);
        }
    }
}