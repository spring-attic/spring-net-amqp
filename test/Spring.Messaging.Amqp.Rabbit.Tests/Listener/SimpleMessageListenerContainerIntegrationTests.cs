// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleMessageListenerContainerIntegrationTests.cs" company="The original author or authors.">
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
using System.Text;
using System.Threading;
using Common.Logging;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Admin;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using Spring.Transaction;
using Spring.Transaction.Support;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// Simple message listener container integration tests.
    /// </summary>
    [TestFixture(1, 1, AcknowledgeModeUtils.AcknowledgeMode.Auto, true, 1, false)]
    [TestFixture(1, 1, AcknowledgeModeUtils.AcknowledgeMode.None, false, 1, false)]
    [TestFixture(4, 1, AcknowledgeModeUtils.AcknowledgeMode.Auto, true, 1, false)]
    [TestFixture(4, 1, AcknowledgeModeUtils.AcknowledgeMode.Auto, true, 1, false)]
    [TestFixture(4, 1, AcknowledgeModeUtils.AcknowledgeMode.Auto, false, 1, false)]
    [TestFixture(2, 2, AcknowledgeModeUtils.AcknowledgeMode.Auto, true, 1, false)]
    [TestFixture(2, 2, AcknowledgeModeUtils.AcknowledgeMode.None, false, 1, false)]
    [TestFixture(20, 4, AcknowledgeModeUtils.AcknowledgeMode.Auto, true, 1, false)]
    [TestFixture(20, 4, AcknowledgeModeUtils.AcknowledgeMode.None, false, 1, false)]
    [TestFixture(300, 4, AcknowledgeModeUtils.AcknowledgeMode.Auto, true, 1, false)]
    [TestFixture(300, 4, AcknowledgeModeUtils.AcknowledgeMode.None, false, 1, false)]
    [TestFixture(300, 4, AcknowledgeModeUtils.AcknowledgeMode.Auto, true, 10, false)]
    [Category(TestCategory.Integration)]
    public class SimpleMessageListenerContainerIntegrationTests
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private static bool hasFixtureSetupBeenRun;

        private readonly Queue queue = new Queue("test.queue");

        private readonly RabbitTemplate template = new RabbitTemplate();

        private readonly int concurrentConsumers;

        private readonly AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode;

        public static EnvironmentAvailable environment = new EnvironmentAvailable("BROKER_INTEGRATION_TEST");

        protected static RabbitBrokerAdmin brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin();

        /// <summary>
        /// Determines if the broker is running.
        /// </summary>
        protected BrokerRunning brokerIsRunning = BrokerRunning.IsRunning();

        #region Fixture Setup and Teardown

        /// <summary>The derived setup.</summary>
        [TestFixtureSetUp]
        public void DerivedSetup()
        {
            if (!hasFixtureSetupBeenRun)
            {
                try
                {
                    if (environment.IsActive())
                    {
                        // Set up broker admin for non-root user
                        // this.brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(); // "rabbit@LOCALHOST", 5672);
                        brokerAdmin.StartNode();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred during SetUp", ex);
                    Assert.Fail("An error occurred during SetUp.");
                }

                if (!this.brokerIsRunning.Apply())
                {
                    Assert.Ignore("Rabbit broker is not running. Ignoring integration test fixture.");
                }

                hasFixtureSetupBeenRun = true;
            }
        }

        /*
        /// <summary>
        /// Code to execute before fixture setup.
        /// </summary>
        public override void BeforeFixtureSetUp() { this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(this.queue); }

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
         * */
        #endregion

        private readonly int messageCount;

        private SimpleMessageListenerContainer container;

        private readonly int txSize;

        private readonly bool externalTransaction;

        private readonly bool transactional;

        /// <summary>Initializes a new instance of the <see cref="SimpleMessageListenerContainerIntegrationTests"/> class.</summary>
        /// <param name="messageCount">The message count.</param>
        /// <param name="concurrency">The concurrency.</param>
        /// <param name="acknowledgeMode">The acknowledge mode.</param>
        /// <param name="transactional">The transactional.</param>
        /// <param name="txSize">The tx size.</param>
        /// <param name="externalTransaction">The external transaction.</param>
        public SimpleMessageListenerContainerIntegrationTests(int messageCount, int concurrency, AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode, bool transactional, int txSize, bool externalTransaction)
        {
            this.messageCount = messageCount;
            this.concurrentConsumers = concurrency;
            this.acknowledgeMode = acknowledgeMode;
            this.transactional = transactional;
            this.txSize = txSize;
            this.externalTransaction = externalTransaction;
        }

        private static object[] GetParams(int i, int messageCount, int concurrency, AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode, bool transactional, int txSize)
        {
            // "i" is just a counter to make it easier to identify the test in the log
            return new object[] { messageCount, concurrency, acknowledgeMode, transactional, txSize, false };
        }

        private static object[] GetParams(int i, int messageCount, int concurrency, AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode, int txSize)
        {
            // For this test always us a transaction if it makes sense...
            return GetParams(i, messageCount, concurrency, acknowledgeMode, acknowledgeMode.TransactionAllowed(), txSize);
        }

        private static object[] GetParams(int i, int messageCount, int concurrency, AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode, bool transactional) { return GetParams(i, messageCount, concurrency, acknowledgeMode, transactional, 1); }

        private static object[] GetParams(int i, int messageCount, int concurrency, AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode) { return GetParams(i, messageCount, concurrency, acknowledgeMode, 1); }

        /// <summary>
        /// Declares the queue.
        /// </summary>
        [SetUp]
        public void DeclareQueue()
        {
            this.brokerIsRunning.Apply();
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.ChannelCacheSize = this.concurrentConsumers;
            connectionFactory.Port = BrokerTestUtils.GetPort();
            this.template.ConnectionFactory = connectionFactory;
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        [TearDown]
        public void Clear()
        {
            // Wait for broker communication to finish before trying to stop container
            Thread.Sleep(300);
            Logger.Debug(m => m("Shutting down at end of test"));

            try
            {
                if (environment.IsActive())
                {
                    // Set up broker admin for non-root user
                    // this.brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(); // "rabbit@LOCALHOST", 5672);
                    brokerAdmin.StartNode();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred during SetUp", ex);
                Assert.Fail("An error occurred during SetUp.");
            }

            if (!this.brokerIsRunning.Apply())
            {
                Assert.Ignore("Rabbit broker is not running. Ignoring integration test fixture.");
            }

            hasFixtureSetupBeenRun = true;
            
            if (this.container != null)
            {
                this.container.Shutdown();
            }
        }

        /// <summary>
        /// Tests the poco listener sunny day.
        /// </summary>
        [Test]
        public void TestPocoListenerSunnyDay()
        {
            var latch = new CountdownEvent(this.messageCount);
            this.DoSunnyDayTest(latch, new MessageListenerAdapter(new SimplePocoListener(latch)));
        }

        /// <summary>
        /// Tests the listener sunny day.
        /// </summary>
        [Test]
        public void TestListenerSunnyDay()
        {
            var latch = new CountdownEvent(this.messageCount);
            this.DoSunnyDayTest(latch, new Listener(latch));
        }

        /// <summary>
        /// Tests the channel aware listener sunny day.
        /// </summary>
        [Test]
        public void TestChannelAwareListenerSunnyDay()
        {
            var latch = new CountdownEvent(this.messageCount);
            this.DoSunnyDayTest(latch, new ChannelAwareListener(latch));
        }

        /// <summary>
        /// Tests the poco listener with exception.
        /// </summary>
        [Test]
        public void TestPocoListenerWithException()
        {
            var latch = new CountdownEvent(this.messageCount);
            this.DoListenerWithExceptionTest(latch, new MessageListenerAdapter(new SimplePocoListener(latch, true)));
        }

        /// <summary>
        /// Tests the listener with exception.
        /// </summary>
        [Test]
        public void TestListenerWithException()
        {
            var latch = new CountdownEvent(this.messageCount);
            this.DoListenerWithExceptionTest(latch, new Listener(latch, true));
        }

        /// <summary>
        /// Tests the channel aware listener with exception.
        /// </summary>
        [Test]
        public void TestChannelAwareListenerWithException()
        {
            var latch = new CountdownEvent(this.messageCount);
            this.DoListenerWithExceptionTest(latch, new ChannelAwareListener(latch, true));
        }

        /// <summary>Does the sunny day test.</summary>
        /// <param name="latch">The latch.</param>
        /// <param name="listener">The listener.</param>
        private void DoSunnyDayTest(CountdownEvent latch, object listener)
        {
            this.container = this.CreateContainer(listener);
            for (var i = 0; i < this.messageCount; i++)
            {
                this.template.ConvertAndSend(this.queue.Name, i + "foo");
            }

            Logger.Debug(m => m("Waiting {0} seconds for messages to be received.", Math.Max(2, this.messageCount / 20)));
            var waited = latch.Wait(new TimeSpan(0, 0, 0, Math.Max(2, this.messageCount / 20)));
            Assert.True(waited, "Timed out waiting for message");
            Assert.Null(this.template.ReceiveAndConvert(this.queue.Name));
        }

        /// <summary>Does the listener with exception test.</summary>
        /// <param name="latch">The latch.</param>
        /// <param name="listener">The listener.</param>
        private void DoListenerWithExceptionTest(CountdownEvent latch, object listener)
        {
            this.container = this.CreateContainer(listener);
            if (this.acknowledgeMode.TransactionAllowed())
            {
                // Should only need one message if it is going to fail
                for (var i = 0; i < this.concurrentConsumers; i++)
                {
                    this.template.ConvertAndSend(this.queue.Name, i + "foo");
                }
            }
            else
            {
                for (var i = 0; i < this.messageCount; i++)
                {
                    this.template.ConvertAndSend(this.queue.Name, i + "foo");
                }
            }

            try
            {
                Logger.Debug(m => m("Waiting {0} seconds for messages to be received.", 5 + Math.Max(1, this.messageCount / 10)));
                var waited = latch.Wait(new TimeSpan(0, 0, 0, 5 + Math.Max(1, this.messageCount / 10)));
                Assert.True(waited, "Timed out waiting for message");
            }
            finally
            {
                // Wait for broker communication to finish before trying to stop
                // container
                Thread.Sleep(300);
                this.container.Shutdown();
                Thread.Sleep(300);
            }

            if (this.acknowledgeMode.TransactionAllowed())
            {
                Assert.NotNull(this.template.ReceiveAndConvert(this.queue.Name));
            }
            else
            {
                Assert.Null(this.template.ReceiveAndConvert(this.queue.Name));
            }
        }

        /// <summary>Creates the container.</summary>
        /// <param name="listener">The listener.</param>
        /// <returns>The container.</returns>
        private SimpleMessageListenerContainer CreateContainer(object listener)
        {
            var container = new SimpleMessageListenerContainer(this.template.ConnectionFactory);
            container.MessageListener = listener;
            container.QueueNames = new[] { this.queue.Name };
            container.TxSize = this.txSize;
            container.PrefetchCount = this.txSize;
            container.ConcurrentConsumers = this.concurrentConsumers;
            container.ChannelTransacted = this.transactional;
            container.AcknowledgeMode = this.acknowledgeMode;
            if (this.externalTransaction)
            {
                container.TransactionManager = new IntegrationTestTransactionManager();
            }

            container.AfterPropertiesSet();
            container.Start();
            return container;
        }
    }

    /// <summary>
    /// A Poco Listener.
    /// </summary>
    public class SimplePocoListener
    {
        private readonly AtomicInteger count = new AtomicInteger();
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly CountdownEvent latch;

        private readonly bool fail;

        /// <summary>Initializes a new instance of the <see cref="SimplePocoListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        public SimplePocoListener(CountdownEvent latch)
            : this(latch, false) { }

        /// <summary>Initializes a new instance of the <see cref="SimplePocoListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        public SimplePocoListener(CountdownEvent latch, bool fail)
        {
            this.latch = latch;
            this.fail = fail;
        }

        /// <summary>Handles the message.</summary>
        /// <param name="value">The value.</param>
        public void HandleMessage(string value)
        {
            try
            {
                var counter = this.count.ReturnValueAndIncrement();
                if (Logger.IsDebugEnabled && counter % 100 == 0)
                {
                    Logger.Debug("Handling: " + value + ":" + counter + " - " + this.latch);
                }

                if (this.fail)
                {
                    throw new Exception("Planned failure");
                }
            }
            finally
            {
                if (this.latch.CurrentCount > 0)
                {
                    Logger.Debug(m => m("Signaling latch. Current count: {0}", this.latch.CurrentCount));
                    this.latch.Signal();
                }
            }
        }

        // this is a bogus overload (TestAtttribute is used since its not ever to be passed) that should never be matched
        // by a message handler method
        // (retain this so that we can test against ambiguity in multi-handler-method-resolution)
        /// <summary>The handle message.</summary>
        /// <param name="value">The value.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void HandleMessage(TestAttribute value) { throw new InvalidOperationException("We should never get here since this overload should never be matched!"); }
    }

    /// <summary>
    /// A listener.
    /// </summary>
    public class Listener : IMessageListener
    {
        private readonly AtomicInteger count = new AtomicInteger();
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly CountdownEvent latch;

        private readonly bool fail;

        /// <summary>Initializes a new instance of the <see cref="Listener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        public Listener(CountdownEvent latch)
            : this(latch, false) { }

        /// <summary>Initializes a new instance of the <see cref="Listener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        public Listener(CountdownEvent latch, bool fail)
        {
            this.latch = latch;
            this.fail = fail;
        }

        /// <summary>Called when a Message is received.</summary>
        /// <param name="message">The message.</param>
        public void OnMessage(Message message)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            try
            {
                var counter = this.count.ReturnValueAndIncrement();
                if (Logger.IsDebugEnabled && counter % 100 == 0)
                {
                    Logger.Debug(value + counter);
                }

                if (this.fail)
                {
                    throw new Exception("Planned failure");
                }
            }
            finally
            {
                if (this.latch.CurrentCount > 0)
                {
                    Logger.Debug(m => m("Signaling latch. Current count: {0}", this.latch.CurrentCount));
                    this.latch.Signal();
                }
            }
        }
    }

    /// <summary>
    /// A channel aware listener.
    /// </summary>
    public class ChannelAwareListener : IChannelAwareMessageListener
    {
        private readonly AtomicInteger count = new AtomicInteger();
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly CountdownEvent latch;

        private readonly bool fail;

        /// <summary>Initializes a new instance of the <see cref="ChannelAwareListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        public ChannelAwareListener(CountdownEvent latch)
            : this(latch, false) { }

        /// <summary>Initializes a new instance of the <see cref="ChannelAwareListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        public ChannelAwareListener(CountdownEvent latch, bool fail)
        {
            this.latch = latch;
            this.fail = fail;
        }

        /// <summary>Called when [message].</summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        public void OnMessage(Message message, IModel channel)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            try
            {
                var counter = this.count.ReturnValueAndIncrement();
                if (Logger.IsDebugEnabled && counter % 100 == 0)
                {
                    Logger.Debug(value + counter);
                }

                if (this.fail)
                {
                    throw new Exception("Planned failure");
                }
            }
            finally
            {
                if (this.latch.CurrentCount > 0)
                {
                    Logger.Debug(m => m("Signaling latch. Current count: {0}", this.latch.CurrentCount));
                    this.latch.Signal();
                }
            }
        }
    }

    /// <summary>
    /// Integration test transaction manager.
    /// </summary>
    internal class IntegrationTestTransactionManager : AbstractPlatformTransactionManager
    {
        /// <summary>Begin a new transaction with the given transaction definition.</summary>
        /// <param name="transaction">Transaction object returned by<see cref="M:Spring.Transaction.Support.AbstractPlatformTransactionManager.DoGetTransaction"/>.</param>
        /// <param name="definition"><see cref="T:Spring.Transaction.ITransactionDefinition"/> instance, describing
        /// propagation behavior, isolation level, timeout etc.</param>
        /// <exception cref="T:Spring.Transaction.TransactionException">In the case of creation or system errors.</exception>
        protected override void DoBegin(object transaction, ITransactionDefinition definition) { }

        /// <summary>Perform an actual commit on the given transaction.</summary>
        /// <param name="status">The status representation of the transaction.</param>
        /// <exception cref="T:Spring.Transaction.TransactionException">In the case of system errors.</exception>
        protected override void DoCommit(DefaultTransactionStatus status) { }

        /// <summary>
        /// Return the current transaction object.
        /// </summary>
        /// <returns>The current transaction object.</returns>
        /// <exception cref="T:Spring.Transaction.CannotCreateTransactionException">
        /// If transaction support is not available.
        /// </exception>
        /// <exception cref="T:Spring.Transaction.TransactionException">
        /// In the case of lookup or system errors.
        /// </exception>
        protected override object DoGetTransaction() { return new object(); }

        /// <summary>Perform an actual rollback on the given transaction.</summary>
        /// <param name="status">The status representation of the transaction.</param>
        /// <exception cref="T:Spring.Transaction.TransactionException">In the case of system errors.</exception>
        protected override void DoRollback(DefaultTransactionStatus status) { }
    }
}
