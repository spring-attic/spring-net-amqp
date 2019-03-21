// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageListenerContainerLifecycleIntegrationTests.cs" company="The original author or authors.">
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
using System.Threading;
using Common.Logging;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>The message listener container lifecycle integration tests.</summary>
    [TestFixture]
    [Category(TestCategory.LifecycleIntegration)]
    public class MessageListenerContainerLifecycleIntegrationTests : AbstractRabbitIntegrationTest
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        private new static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The queue.
        /// </summary>
        private static readonly Queue queue = new Queue("test.queue");

        private enum Concurrency
        {
            /// <summary>The low.</summary>
            LOW = 1, 

            /// <summary>The high.</summary>
            HIGH = 5
        }

        private enum MessageCount
        {
            /// <summary>The low.</summary>
            LOW = 1, 

            /// <summary>The medium.</summary>
            MEDIUM = 20, 

            /// <summary>The high.</summary>
            HIGH = 500
        }

        #region Fixture Setup and Teardown

        /// <summary>
        /// Code to execute before fixture setup.
        /// </summary>
        public override void BeforeFixtureSetUp() { }

        /// <summary>
        /// Code to execute before fixture teardown.
        /// </summary>
        public override void BeforeFixtureTearDown() { }

        /// <summary>
        /// Code to execute after fixture setup.
        /// </summary>
        public override void AfterFixtureSetUp() { }

        /// <summary>
        /// Code to execute after fixture teardown.
        /// </summary>
        public override void AfterFixtureTearDown() { }
        #endregion

        /// <summary>The set up.</summary>
        [SetUp]
        public void SetUp()
        {
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue);
            this.brokerIsRunning.Apply();
        }

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
        [Test]
        public void TestTransactionalLowLevel() { this.DoTest(MessageCount.MEDIUM, Concurrency.LOW, TransactionModeUtils.TransactionMode.ON); }

        /// <summary>
        /// Tests the transactional high level.
        /// </summary>
        [Test]
        public void TestTransactionalHighLevel() { this.DoTest(MessageCount.HIGH, Concurrency.HIGH, TransactionModeUtils.TransactionMode.ON); }

        /// <summary>
        /// Tests the transactional low level with prefetch.
        /// </summary>
        [Test]
        public void TestTransactionalLowLevelWithPrefetch() { this.DoTest(MessageCount.MEDIUM, Concurrency.LOW, TransactionModeUtils.TransactionMode.PREFETCH); }

        /// <summary>
        /// Tests the transactional high level with prefetch.
        /// </summary>
        [Test]
        public void TestTransactionalHighLevelWithPrefetch() { this.DoTest(MessageCount.HIGH, Concurrency.HIGH, TransactionModeUtils.TransactionMode.PREFETCH); }

        /// <summary>
        /// Tests the non transactional low level.
        /// </summary>
        [Test]
        public void TestNonTransactionalLowLevel() { this.DoTest(MessageCount.MEDIUM, Concurrency.LOW, TransactionModeUtils.TransactionMode.OFF); }

        /// <summary>
        /// Tests the non transactional high level.
        /// </summary>
        [Test]
        public void TestNonTransactionalHighLevel() { this.DoTest(MessageCount.HIGH, Concurrency.HIGH, TransactionModeUtils.TransactionMode.OFF); }

        /// <summary>Does the test.</summary>
        /// <param name="level">The level.</param>
        /// <param name="concurrency">The concurrency.</param>
        /// <param name="transactionMode">The transaction mode.</param>
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
            container.ChannelTransacted = transactionMode.IsTransactional();
            container.ConcurrentConsumers = concurrentConsumers;

            if (transactionMode.Prefetch() > 0)
            {
                container.PrefetchCount = transactionMode.Prefetch();
                container.TxSize = transactionMode.TxSize();
            }

            container.QueueNames = new[] { queue.Name };
            container.AfterPropertiesSet();
            container.Start();

            try
            {
                var waited = latch.Wait(50);
                Logger.Info("All messages received before stop: " + waited);
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
                    Logger.Info("All messages received after stop: " + waited);
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

                Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
                waited = latch.Wait(timeout * 1000);
                Logger.Info("All messages received after start: " + waited);
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
    internal class LifecyclePocoListener
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly AtomicInteger count = new AtomicInteger();

        private CountdownEvent latch;

        /// <summary>Initializes a new instance of the <see cref="LifecyclePocoListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        public LifecyclePocoListener(CountdownEvent latch) { this.latch = latch; }

        /// <summary>Resets the specified latch.</summary>
        /// <param name="latch">The latch.</param>
        public void Reset(CountdownEvent latch) { this.latch = latch; }

        /// <summary>Handles the message.</summary>
        /// <param name="value">The value.</param>
        public void HandleMessage(string value)
        {
            try
            {
                Logger.Debug(value + this.count.ReturnValueAndIncrement());
                Thread.Sleep(100);
            }
            finally
            {
                if (this.latch.CurrentCount > 0)
                {
                    this.latch.Signal();
                }
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count { get { return this.count.Value; } }
    }

    /// <summary>
    /// Transaction Mode Utilities
    /// </summary>
    internal static class TransactionModeUtils
    {
        /// <summary>
        /// Transaction Mode.
        /// </summary>
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

        /// <summary>Determines whether the specified mode is transactional.</summary>
        /// <param name="mode">The mode.</param>
        /// <returns><c>true</c> if the specified mode is transactional; otherwise, <c>false</c>.</returns>
        public static bool IsTransactional(this TransactionMode mode) { return mode != TransactionMode.OFF; }

        /// <summary>Acknowledges the mode.</summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The acknowledge mode.</returns>
        public static AcknowledgeModeUtils.AcknowledgeMode AcknowledgeMode(this TransactionMode mode) { return mode == TransactionMode.OFF ? AcknowledgeModeUtils.AcknowledgeMode.None : AcknowledgeModeUtils.AcknowledgeMode.Auto; }

        /// <summary>Prefetches the specified mode.</summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The prefetch size.</returns>
        public static int Prefetch(this TransactionMode mode) { return mode == TransactionMode.PREFETCH ? 10 : -1; }

        /// <summary>Txes the size.</summary>
        /// <param name="mode">The mode.</param>
        /// <returns>The transaction size.</returns>
        public static int TxSize(this TransactionMode mode) { return mode == TransactionMode.PREFETCH ? 5 : -1; }
    }
}
