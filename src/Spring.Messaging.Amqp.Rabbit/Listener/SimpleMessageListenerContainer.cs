// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleMessageListenerContainer.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AopAlliance.Aop;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Aop;
using Spring.Aop.Framework;
using Spring.Aop.Support;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Transaction;
using Spring.Transaction.Interceptor;
using Spring.Transaction.Support;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    /// A simple message listener container.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class SimpleMessageListenerContainer : AbstractMessageListenerContainer
    {
        #region Logging

        /// <summary>
        /// The Logger.
        /// </summary>
        private new static readonly ILog Logger = LogManager.GetCurrentClassLogger();
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
        private ISet<BlockingQueueConsumer> consumers;

        /// <summary>
        /// The consumers monitor.
        /// </summary>
        private readonly object consumersMonitor = new object();

        /// <summary>
        /// The transaction manager.
        /// </summary>
        internal IPlatformTransactionManager transactionManager;

        /// <summary>
        /// The transaction attribute.
        /// </summary>
        private ITransactionAttribute transactionAttribute = new DefaultTransactionAttribute { TransactionIsolationLevel = IsolationLevel.Unspecified };

        /// <summary>
        /// The advice chain.
        /// </summary>
        private volatile IAdvice[] adviceChain = new IAdvice[0];

        /// <summary>
        /// The cancellation lock.
        /// </summary>
        private readonly ActiveObjectCounter<BlockingQueueConsumer> cancellationLock = new ActiveObjectCounter<BlockingQueueConsumer>();

        /// <summary>
        /// The message properties converter.
        /// </summary>
        private volatile IMessagePropertiesConverter messagePropertiesConverter = new DefaultMessagePropertiesConverter();

        private volatile bool defaultRequeueRejected = true;

        private readonly ContainerDelegate containerDelegate;

        /// <summary>
        /// The proxy.
        /// </summary>
        internal IContainerDelegate proxy;
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleMessageListenerContainer"/> class.
        /// Default constructor for convenient dependency injection via setters.
        /// </summary>
        public SimpleMessageListenerContainer()
        {
            this.containerDelegate = new ContainerDelegate(this);
            this.proxy = this.containerDelegate;
        }

        /// <summary>Initializes a new instance of the <see cref="SimpleMessageListenerContainer"/> class. Create a listener container from the connection factory (mandatory).</summary>
        /// <param name="connectionFactory">The connection factory.</param>
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
        /// <remarks>
        /// <para>
        /// If <see cref="TxSize"/> is > 1 then multiple listener executions will all be wrapped in the same advice up to that limit.
        /// </para>
        /// <para>
        /// If a <see cref="IPlatformTransactionManager"/> is provided as well, then separate advice is created for the transaction and applied first in the chain. 
        /// In that case the advice chain provided here should not contain a transaction interceptor (otherwise two transactions would be be applied).
        /// </para>
        /// </remarks>
        /// <value>The advice chain.</value>
        public IAdvice[] AdviceChain { set { this.adviceChain = value; } }

        /// <summary>
        /// Sets RecoveryInterval. Specify the interval between recovery attempts, in <b>milliseconds</b>. The default is 5000 ms, that is, 5 seconds.
        /// </summary>
        /// <value>The recovery interval.</value>
        public long RecoveryInterval { set { this.recoveryInterval = value; } }

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
        public long ReceiveTimeout { set { this.receiveTimeout = value; } }

        /// <summary>
        /// Sets ShutdownTimeout.
        /// </summary>
        /// <remarks>
        /// The time to wait for workers in milliseconds after the container is stopped, and before the connection is forced
        /// closed. If any workers are active when the shutdown signal comes they will be allowed to finish processing as
        /// long as they can finish within this timeout. Otherwise the connection is closed and messages remain unacked (if
        /// the channel is transactional). Defaults to 5 seconds.
        /// </remarks>
        public long ShutdownTimeout { set { this.shutdownTimeout = value; } }

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
        /// <remarks>
        /// Tells the broker how many messages to send to each consumer in a single request. Often this can be set quite high
        /// to improve throughput. It should be greater than or equal to the <see cref="TxSize"/>.
        /// </remarks>
        public int PrefetchCount { set { this.prefetchCount = value; } }

        /// <summary>
        /// Sets TxSize.
        /// </summary>
        /// <remarks>
        /// Tells the container how many messages to process in a single transaction (if the channel is transactional). For
        /// best results it should be less than or equal to the <see cref="PrefetchCount"/>.
        /// </remarks>
        public int TxSize { set { this.txSize = value; } }

        /// <summary>
        /// Sets TransactionManager.
        /// </summary>
        public IPlatformTransactionManager TransactionManager { set { this.transactionManager = value; } }

        /// <summary>
        /// Sets TransactionAttribute.
        /// </summary>
        public ITransactionAttribute TransactionAttribute { set { this.transactionAttribute = value; } }

        /// <summary>Sets the message properties converter.</summary>
        public IMessagePropertiesConverter MessagePropertiesConverter
        {
            set
            {
                AssertUtils.ArgumentNotNull(value, "messagePropertiesConverter must not be null");
                this.messagePropertiesConverter = value;
            }
        }

        /// <summary>
        /// Sets DefaultRequeueRejected.
        /// </summary>
        /// <remarks>
        /// Determines the default behavior when a message is rejected, for example because the listener
        /// threw an exception. When true, messages will be requeued, when false, they will not. For
        /// versions of Rabbit that support dead-lettering, the message must not be requeued in order
        /// to be sent to the dead letter exchange. Setting to false causes all rejections to not
        /// be requeued. When true, the default can be overridden by the listener throwing an
        /// <see cref="AmqpRejectAndDontRequeueException"/>. Default true.
        /// </remarks>
        public bool DefaultRequeueRejected { set { this.defaultRequeueRejected = value; } }

        /// <summary>
        /// Gets a value indicating whether SharedConnectionEnabled.
        /// </summary>
        public bool SharedConnectionEnabled { get { return true; } }

        /// <summary>
        /// Gets ActiveConsumerCount.
        /// </summary>
        public int ActiveConsumerCount { get { return this.cancellationLock.GetCount(); } }
        #endregion

        /// <summary>
        /// Validate the configuration.
        /// </summary>
        protected override void ValidateConfiguration()
        {
            base.ValidateConfiguration();

            AssertUtils.State(
                !(this.AcknowledgeMode.IsAutoAck() && this.transactionManager != null), 
                "The acknowledgeMode is None (autoack in Rabbit terms) which is not consistent with having an external transaction manager. Either use a different AcknowledgeMode or make sure the transactionManager is null.");

            if (this.ConnectionFactory is CachingConnectionFactory)
            {
                var cf = (CachingConnectionFactory)this.ConnectionFactory;
                if (cf.ChannelCacheSize < this.concurrentConsumers)
                {
                    cf.ChannelCacheSize = this.concurrentConsumers;
                    Logger.Warn(m => m("CachingConnectionFactory's channelCacheSize can not be less than the number of concurrentConsumers so it was reset to match: {0}", this.concurrentConsumers));
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
            if (!this.ExposeListenerChannel && this.transactionManager != null)
            {
                Logger.Warn(m => m("exposeListenerChannel=false is ignored when using a TransactionManager"));
            }

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
                var newConsumers = this.InitializeConsumers();

                if (this.consumers == null)
                {
                    Logger.Info(m => m("Consumers were initialized and then cleared (presumably the container was stopped concurrently)"));
                    return;
                }

                if (newConsumers <= 0)
                {
                    Logger.Info(m => m("Consumers are already running"));
                    return;
                }

                var processors = new HashSet<AsyncMessageProcessingConsumer>();

                foreach (var consumer in this.consumers)
                {
                    var processor = new AsyncMessageProcessingConsumer(consumer, this);
                    processors.Add(processor);
                    var taskExecutor = new Task(processor.Run);
                    taskExecutor.Start();
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
            this.Shutdown();
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
                Logger.Info(m => m("Waiting for workers to finish."));
                var finished = this.cancellationLock.Await(new TimeSpan(0, 0, 0, 0, (int)this.shutdownTimeout));
                Logger.Info(m => m(finished ? "Successfully waited for workers to finish." : "Workers not finished. Forcing connections to close."));
            }
            catch (ThreadInterruptedException e)
            {
                Thread.CurrentThread.Interrupt();
                Logger.Warn("Interrupted waiting for workers.  Continuing with shutdown.");
            }
            catch (Exception ex)
            {
                Logger.Error("Error occurred shutting down workers.", ex);
            }

            lock (this.consumersMonitor)
            {
                this.consumers = null;
            }
        }

        /// <summary>Initialize the consumers.</summary>
        /// <returns>The number of initialized consumers.</returns>
        protected int InitializeConsumers()
        {
            var count = 0;
            lock (this.consumersMonitor)
            {
                if (this.consumers == null)
                {
                    this.cancellationLock.Dispose();
                    this.consumers = new HashSet<BlockingQueueConsumer>();
                    for (var i = 0; i < this.concurrentConsumers; i++)
                    {
                        var consumer = this.CreateBlockingQueueConsumer();
                        this.consumers.Add(consumer);
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Determine if channel is locally transacted.</summary>
        /// <param name="channel">The channel.</param>
        /// <returns>True if locally transacted, else false.</returns>
        protected override bool IsChannelLocallyTransacted(IModel channel) { return base.IsChannelLocallyTransacted(channel) && this.transactionManager == null; }

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
            return new BlockingQueueConsumer(
                this.ConnectionFactory, this.messagePropertiesConverter, this.cancellationLock, this.AcknowledgeMode, this.ChannelTransacted, actualPrefetchCount, this.defaultRequeueRejected, this.GetRequiredQueueNames());
        }

        /// <summary>Restart the consumer.</summary>
        /// <param name="consumer">The consumer.</param>
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
                        Logger.Warn(m => m("Consumer failed irretrievably on restart. {0}: {1}", e.Source, e.Message));

                        // Re-throw and have it logged properly by the caller.
                        throw;
                    }

                    var processor = new AsyncMessageProcessingConsumer(consumer, this);
                    var taskExecutor = new Task(processor.Run);
                    taskExecutor.Start();
                }
            }
        }

        /// <summary>Receive and execute.</summary>
        /// <param name="consumer">The consumer.</param>
        /// <returns>True if a message was received.</returns>
        internal bool ReceiveAndExecute(BlockingQueueConsumer consumer)
        {
            if (this.transactionManager != null)
            {
                try
                {
                    var transactionTemplate = new TransactionTemplate(this.transactionManager);
                    transactionTemplate.PropagationBehavior = this.transactionAttribute.PropagationBehavior;
                    transactionTemplate.TransactionIsolationLevel = IsolationLevel.Unspecified; // TODO: revert to transactionAttribute once we take dependency on SPRNET 2.0
                    transactionTemplate.TransactionTimeout = this.transactionAttribute.TransactionTimeout;
                    transactionTemplate.ReadOnly = this.transactionAttribute.ReadOnly;
                    transactionTemplate.Name = this.transactionAttribute.Name;

                    return (bool)transactionTemplate.Execute(
                        status =>
                        {
                            ConnectionFactoryUtils.BindResourceToTransaction(new RabbitResourceHolder(consumer.Channel, false), this.ConnectionFactory, true);
                            return this.DoReceiveAndExecute(consumer);
                        });
                }
                catch (Exception ex)
                {
                    Logger.Error(m => m("Error receiving and executing."), ex);
                    throw;
                }
            }

            return this.DoReceiveAndExecute(consumer);
        }

        /// <summary>Perform receive and execute actions.</summary>
        /// <param name="consumer">The consumer.</param>
        /// <returns>True if a message was received.</returns>
        private bool DoReceiveAndExecute(BlockingQueueConsumer consumer)
        {
            var channel = consumer.Channel;

            for (var i = 0; i < this.txSize; i++)
            {
                Logger.Trace(m => m("Waiting for message from consumer."));
                var message = consumer.NextMessage(this.receiveTimeout);
                if (message == null)
                {
                    break;
                }

                try
                {
                    this.ExecuteListener(channel, message);
                }
                catch (ImmediateAcknowledgeAmqpException e)
                {
                    Logger.Trace(m => m("DoReceiveAndExecute => "), e);
                    break;
                }
                catch (Exception ex)
                {
                    consumer.RollbackOnExceptionIfNecessary(ex);
                    throw;
                }
            }

            return consumer.CommitIfNecessary(this.IsChannelLocallyTransacted(channel));
        }

        /// <summary>Invoke the listener.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="message">The message.</param>
        public override void InvokeListener(IModel channel, Message message) { this.proxy.InvokeListener(channel, message); }

        internal void InvokeListenerBase(IModel channel, Message message) { base.InvokeListener(channel, message); }

        /// <summary>Handle a startup failure.
        /// Wait for a period determined by the {@link #setRecoveryInterval(long) recoveryInterval} to give the container a
        /// chance to recover from consumer startup failure, e.g. if the broker is down.</summary>
        /// <param name="t">The t.</param>
        /// <exception cref="AmqpException">The startup exception.</exception>
        internal void HandleStartupFailure(Exception t)
        {
            try
            {
                var timeout = DateTime.UtcNow.AddMilliseconds(this.recoveryInterval);
                while (this.IsActive && DateTime.UtcNow < timeout)
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
    }

    /// <summary>
    /// An asynchronous message processing consumer.
    /// </summary>
    public class AsyncMessageProcessingConsumer
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

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
        private readonly SimpleMessageListenerContainer outer;

        /// <summary>Initializes a new instance of the <see cref="AsyncMessageProcessingConsumer"/> class.</summary>
        /// <param name="consumer">The consumer.</param>
        /// <param name="outer">The outer.</param>
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
        public FatalListenerStartupException GetStartupException()
        {
            if (!this.start.Wait(new TimeSpan(0, 0, 0, 0, 60000)))
            {
                throw new TimeoutException("Timed out waiting for startup");
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
                    if (this.start.CurrentCount > 0)
                    {
                        this.start.Signal();
                    }

                    this.outer.HandleStartupFailure(t);
                    throw;
                }

                if (this.outer.transactionManager != null)
                {
                    // Register the consumer's channel so it will be used by the transaction manager
                    // if it's an instance of RabbitTransactionManager.
                    ConnectionFactoryUtils.RegisterConsumerChannel(this.consumer.Channel);
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
                        Logger.Trace(m => m("Error on ReceiveAndExecute"), ex);

                        // Continue to process, otherwise re-throw
                    }
                }
            }
            catch (ThreadInterruptedException e)
            {
                Logger.Debug(m => m("Consumer thread interrupted, processing stopped."));
                Thread.CurrentThread.Interrupt();
                aborted = true;
            }
            catch (FatalListenerStartupException ex)
            {
                Logger.Error(m => m("Consumer received fatal exception on startup", ex));
                this.startupException = ex;

                // Fatal, but no point re-throwing, so just abort.
                aborted = true;
            }
            catch (FatalListenerExecutionException ex)
            {
                Logger.Error(m => m("Consumer received fatal exception during processing"), ex);

                // Fatal, but no point re-throwing, so just abort.
                aborted = true;
            }
            catch (Exception t)
            {
                Logger.Warn(m => m("Consumer raised exception, processing can restart if the connection factory supports it. " + "Exception summary: " + t));
            }
            finally
            {
                if (this.outer.transactionManager != null)
                {
                    ConnectionFactoryUtils.UnRegisterConsumerChannel();
                }
            }

            // In all cases count down to allow container to progress beyond startup
            if (this.start.CurrentCount > 0)
            {
                this.start.Signal();
            }

            if (!this.outer.IsActive || aborted)
            {
                Logger.Debug(m => m("Cancelling {0}", this.consumer));
                try
                {
                    this.consumer.Stop();
                }
                catch (AmqpException e)
                {
                    Logger.Info(m => m("Could not cancel message consumer"), e);
                }

                if (aborted)
                {
                    Logger.Info(m => m("Stopping container from aborted consumer"));
                    this.outer.Stop();
                }
            }
            else
            {
                Logger.Info(m => m("Restarting {0}", this.consumer));
                this.outer.Restart(this.consumer);
            }
        }

        #endregion
    }

    /// <summary>
    /// An IContainerDelegate
    /// </summary>
    public interface IContainerDelegate
    {
        /// <summary>Invoke the listener.</summary>
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
        private readonly SimpleMessageListenerContainer outer;

        /// <summary>Initializes a new instance of the <see cref="ContainerDelegate"/> class.</summary>
        /// <param name="outer">The outer.</param>
        public ContainerDelegate(SimpleMessageListenerContainer outer) { this.outer = outer; }

        /// <summary>Invoke the listener.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="message">The message.</param>
        public void InvokeListener(IModel channel, Message message) { this.outer.InvokeListenerBase(channel, message); }
    }

    /// <summary>The wrapped transaction exception.</summary>
    public class WrappedTransactionException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="WrappedTransactionException"/> class.</summary>
        /// <param name="cause">The cause.</param>
        public WrappedTransactionException(Exception cause) : base(string.Empty, cause) { }
    }
}
