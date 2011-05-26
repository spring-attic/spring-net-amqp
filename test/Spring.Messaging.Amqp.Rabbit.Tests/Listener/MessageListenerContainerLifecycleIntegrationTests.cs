using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Logging;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Test;
using Spring.Threading.AtomicTypes;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    public class MessageListenerContainerLifecycleIntegrationTests
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private static ILog logger = LogManager.GetLogger(typeof(MessageListenerContainerLifecycleIntegrationTests));

        /// <summary>
        /// The queue.
        /// </summary>
        private static Queue queue = new Queue("test.queue");


        private enum Concurrency
        {
            LOW = 1,
            HIGH = 5
        }

        private enum MessageCount
        {
            LOW = 1,
            MEDIUM = 20,
            HIGH = 500
        }

        //@Rule
        public BrokerRunning brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue);

        //@Rule
        //public Log4jLevelAdjuster logLevels = new Log4jLevelAdjuster(Level.INFO, RabbitTemplate.class,
        //		SimpleMessageListenerContainer.class, BlockingQueueConsumer.class,
        //		MessageListenerContainerLifecycleIntegrationTests.class);

        private RabbitTemplate CreateTemplate(int concurrentConsumers)
        {
            var template = new RabbitTemplate();
            // SingleConnectionFactory connectionFactory = new SingleConnectionFactory();
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.ChannelCacheSize = concurrentConsumers;
            connectionFactory.Port = BrokerTestUtils.GetPort();
            template.ConnectionFactory = connectionFactory;
            return template;
        }

        /// <summary>
        /// Tests the transactional low level.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestTransactionalLowLevel()
        {
            this.DoTest(MessageCount.MEDIUM, Concurrency.LOW, TransactionModeUtils.TransactionMode.ON);
        }

        /// <summary>
        /// Tests the transactional high level.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestTransactionalHighLevel()
        {
            this.DoTest(MessageCount.HIGH, Concurrency.HIGH, TransactionModeUtils.TransactionMode.ON);
        }

        /// <summary>
        /// Tests the transactional low level with prefetch.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestTransactionalLowLevelWithPrefetch()
        {
            this.DoTest(MessageCount.MEDIUM, Concurrency.LOW, TransactionModeUtils.TransactionMode.PREFETCH);
        }

        /// <summary>
        /// Tests the transactional high level with prefetch.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestTransactionalHighLevelWithPrefetch()
        {
            this.DoTest(MessageCount.HIGH, Concurrency.HIGH, TransactionModeUtils.TransactionMode.PREFETCH);
        }

        /// <summary>
        /// Tests the non transactional low level.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestNonTransactionalLowLevel()
        {
            this.DoTest(MessageCount.MEDIUM, Concurrency.LOW, TransactionModeUtils.TransactionMode.OFF);
        }

        /// <summary>
        /// Tests the non transactional high level.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestNonTransactionalHighLevel()
        {
            this.DoTest(MessageCount.HIGH, Concurrency.HIGH, TransactionModeUtils.TransactionMode.OFF);
        }

        /// <summary>
        /// Does the test.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="concurrency">The concurrency.</param>
        /// <param name="transactionMode">The transaction mode.</param>
        /// <remarks></remarks>
        private void DoTest(MessageCount level, Concurrency concurrency, TransactionModeUtils.TransactionMode transactionMode)
        {

            var messageCount = (int)level;
            var concurrentConsumers = (int)concurrency;
            var transactional = transactionMode.IsTransactional();

            var template = this.CreateTemplate(concurrentConsumers);

            var latch = new CountdownEvent(messageCount);
            for (var i = 0; i < messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var container = new SimpleMessageListenerContainer(template.ConnectionFactory);
            var listener = new LifecyclePocoListener(latch);
            container.MessageListener = new MessageListenerAdapter(listener);
            container.AcknowledgeMode = transactionMode.AcknowledgeMode();
            container.IsChannelTransacted = transactionMode.IsTransactional();
            container.ConcurrentConsumers = concurrentConsumers;

            if (transactionMode.Prefetch() > 0)
            {
                container.PrefetchCount = transactionMode.Prefetch();
                container.TxSize = transactionMode.TxSize();
            }

            container.SetQueueNames(new string[] { queue.Name });
            container.AfterPropertiesSet();
            container.Start();

            try
            {
                var waited = latch.Wait(50);
                logger.Info("All messages received before stop: " + waited);
                if (messageCount > 1)
                {
                    Assert.False(waited, "Expected not to receive all messages before stop");
                }

                Assert.AreEqual(concurrentConsumers, container.ActiveConsumerCount);
                container.Stop();
                Thread.Sleep(500);
                Assert.AreEqual(0, container.ActiveConsumerCount);
                if (!transactional)
                {
                    var messagesReceivedAfterStop = listener.Count;
                    waited = latch.Wait(500);
                    logger.Info("All messages received after stop: " + waited);
                    if (messageCount < 100)
                    {
                        Assert.True(waited, "Expected to receive all messages after stop");
                    }

                    Assert.AreEqual(messagesReceivedAfterStop, listener.Count, "Unexpected additional messages received after stop");

                    for (var i = 0; i < messageCount; i++)
                    {
                        template.ConvertAndSend(queue.Name, i + "bar");
                    }

                    latch = new CountdownEvent(messageCount);
                    listener.Reset(latch);
                }

                var messagesReceivedBeforeStart = listener.Count;
                container.Start();
                var timeout = Math.Min(1 + messageCount / (4 * concurrentConsumers), 30);

                logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
                waited = latch.Wait(timeout * 1000);
                logger.Info("All messages received after start: " + waited);
                Assert.AreEqual(concurrentConsumers, container.ActiveConsumerCount);
                if (transactional)
                {
                    Assert.True(waited, "Timed out waiting for message");
                }
                else
                {
                    var count = listener.Count;
                    Assert.True(messagesReceivedBeforeStart < count, "Expected additional messages received after start: " + messagesReceivedBeforeStart + ">=" + count);
                    Assert.Null(template.Receive(queue.Name), "Messages still available");
                }

                Assert.AreEqual(concurrentConsumers, container.ActiveConsumerCount);
            }
            finally
            {
                // Wait for broker communication to finish before trying to stop
                // container
                Thread.Sleep(500);
                container.Shutdown();
                Assert.AreEqual(0, container.ActiveConsumerCount);
            }

            Assert.Null(template.ReceiveAndConvert(queue.Name));
        }
    }

    /// <summary>
    /// A lifecycle Poco Listener.
    /// </summary>
    /// <remarks></remarks>
    internal class LifecyclePocoListener
    {
        private static ILog logger = LogManager.GetLogger(typeof(LifecyclePocoListener));
        private AtomicInteger count = new AtomicInteger();

        private CountdownEvent latch;

        /// <summary>
        /// Initializes a new instance of the <see cref="LifecyclePocoListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public LifecyclePocoListener(CountdownEvent latch)
        {
            this.latch = latch;
        }

        /// <summary>
        /// Resets the specified latch.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public void Reset(CountdownEvent latch)
        {
            this.latch = latch;
        }

        /// <summary>
        /// Handles the message.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks></remarks>
        public void HandleMessage(string value)
        {
            try
            {
                logger.Debug(value + this.count.ReturnValueAndIncrement());
                Thread.Sleep(100);
            }
            finally
            {
                this.latch.Signal();
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <remarks></remarks>
        public int Count
        {
            get { return this.count.Value; }
        }
    }

    /// <summary>
    /// Transaction Mode Utilities
    /// </summary>
    /// <remarks></remarks>
    internal static class TransactionModeUtils
    {
        /// <summary>
        /// Transaction Mode.
        /// </summary>
        /// <remarks></remarks>
        internal enum TransactionMode
        {
            /// <summary>
            /// On
            /// </summary>
            ON,

            /// <summary>
            /// Off
            /// </summary>
            OFF,

            /// <summary>
            /// Prefetch
            /// </summary>
            PREFETCH
        }

        /// <summary>
        /// Determines whether the specified mode is transactional.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns><c>true</c> if the specified mode is transactional; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        public static bool IsTransactional(this TransactionMode mode)
        {
            return mode != TransactionMode.OFF;
        }

        /// <summary>
        /// Acknowledges the mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The acknowledge mode.</returns>
        /// <remarks></remarks>
        public static AcknowledgeModeUtils.AcknowledgeMode AcknowledgeMode(this TransactionMode mode)
        {
            return mode == TransactionMode.OFF ? AcknowledgeModeUtils.AcknowledgeMode.NONE : AcknowledgeModeUtils.AcknowledgeMode.AUTO;
        }

        /// <summary>
        /// Prefetches the specified mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The prefetch size.</returns>
        /// <remarks></remarks>
        public static int Prefetch(this TransactionMode mode)
        {
            return mode == TransactionMode.PREFETCH ? 10 : -1;
        }

        /// <summary>
        /// Txes the size.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The transaction size.</returns>
        /// <remarks></remarks>
        public static int TxSize(this TransactionMode mode)
        {
            return mode == TransactionMode.PREFETCH ? 5 : -1;
        }
    }
}
