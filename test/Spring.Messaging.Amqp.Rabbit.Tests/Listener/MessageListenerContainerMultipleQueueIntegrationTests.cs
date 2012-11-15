// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageListenerContainerMultipleQueueIntegrationTests.cs" company="The original author or authors.">
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
using System.Threading;
using Common.Logging;
using Moq;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using Spring.Messaging.Amqp.Support.Converter;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>The message listener container multiple queue integration tests.</summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class MessageListenerContainerMultipleQueueIntegrationTests : AbstractRabbitIntegrationTest
    {
        private new static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private static readonly Queue queue1 = new Queue("test.queue.1");

        private static readonly Queue queue2 = new Queue("test.queue.2");

        // @Rule
        public BrokerRunning brokerIsRunningAndQueue1Empty;

        // @Rule
        public BrokerRunning brokerIsRunningAndQueue2Empty;

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

        /// <summary>
        /// Sets up.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.brokerIsRunningAndQueue1Empty = BrokerRunning.IsRunningWithEmptyQueues(queue1);
            this.brokerIsRunningAndQueue1Empty.Apply();
            this.brokerIsRunningAndQueue2Empty = BrokerRunning.IsRunningWithEmptyQueues(queue2);
            this.brokerIsRunningAndQueue2Empty.Apply();
        }

        /// <summary>
        /// Tests the multiple queues.
        /// </summary>
        [Test]
        public void TestMultipleQueues()
        {
            var mockConfigurer = new Mock<IContainerConfigurer>();
            mockConfigurer.Setup(c => c.Configure(It.IsAny<SimpleMessageListenerContainer>())).Callback<SimpleMessageListenerContainer>((container) => container.Queues = new[] { queue1, queue2 });

            this.DoTest(1, mockConfigurer.Object);
        }

        /// <summary>
        /// Tests the multiple queue names.
        /// </summary>
        [Test]
        public void TestMultipleQueueNames()
        {
            var mockConfigurer = new Mock<IContainerConfigurer>();
            mockConfigurer.Setup(c => c.Configure(It.IsAny<SimpleMessageListenerContainer>())).Callback<SimpleMessageListenerContainer>((container) => container.QueueNames = new[] { queue1.Name, queue2.Name });

            this.DoTest(1, mockConfigurer.Object);
        }

        /// <summary>
        /// Tests the multiple queues with concurrent consumers.
        /// </summary>
        [Test]
        public void TestMultipleQueuesWithConcurrentConsumers()
        {
            var mockConfigurer = new Mock<IContainerConfigurer>();
            mockConfigurer.Setup(c => c.Configure(It.IsAny<SimpleMessageListenerContainer>())).Callback<SimpleMessageListenerContainer>((container) => container.Queues = new[] { queue1, queue2 });

            this.DoTest(3, mockConfigurer.Object);
        }

        /// <summary>
        /// Tests the multiple queue names with concurrent consumers.
        /// </summary>
        [Test]
        public void TestMultipleQueueNamesWithConcurrentConsumers()
        {
            var mockConfigurer = new Mock<IContainerConfigurer>();
            mockConfigurer.Setup(c => c.Configure(It.IsAny<SimpleMessageListenerContainer>())).Callback<SimpleMessageListenerContainer>((container) => container.QueueNames = new[] { queue1.Name, queue2.Name });

            this.DoTest(3, mockConfigurer.Object);
        }

        /// <summary>Does the test.</summary>
        /// <param name="concurrentConsumers">The concurrent consumers.</param>
        /// <param name="configurer">The configurer.</param>
        private void DoTest(int concurrentConsumers, IContainerConfigurer configurer)
        {
            var messageCount = 10;
            var template = new RabbitTemplate();
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.ChannelCacheSize = concurrentConsumers;
            connectionFactory.Port = BrokerTestUtils.GetPort();
            template.ConnectionFactory = connectionFactory;
            var messageConverter = new SimpleMessageConverter();
            messageConverter.CreateMessageIds = true;
            template.MessageConverter = messageConverter;
            for (var i = 0; i < messageCount; i++)
            {
                template.ConvertAndSend(queue1.Name, i);
                template.ConvertAndSend(queue2.Name, i);
            }

            var container = new SimpleMessageListenerContainer(connectionFactory);
            var latch = new CountdownEvent(messageCount * 2);
            var listener = new MultiplePocoListener(latch);
            container.MessageListener = new MessageListenerAdapter(listener);
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
            container.ChannelTransacted = true;
            container.ConcurrentConsumers = concurrentConsumers;
            configurer.Configure(container);
            container.AfterPropertiesSet();
            container.Start();
            try
            {
                var timeout = Math.Min((1 + messageCount) / concurrentConsumers, 50);
                Logger.Info("Timeout: " + timeout);
                var waited = latch.Wait(timeout * 1000);
                Logger.Info("All messages recovered: " + waited);
                Assert.AreEqual(concurrentConsumers, container.ActiveConsumerCount);
                Assert.True(waited, "Timed out waiting for messages");
            }
            catch (ThreadInterruptedException e)
            {
                Thread.CurrentThread.Interrupt();
                throw new ThreadStateException("unexpected interruption");
            }
            finally
            {
                container.Shutdown();
                Assert.AreEqual(0, container.ActiveConsumerCount);
            }

            Assert.Null(template.ReceiveAndConvert(queue1.Name));
            Assert.Null(template.ReceiveAndConvert(queue2.Name));
        }
    }

    /// <summary>
    /// A container configurer interface.
    /// </summary>
    public interface IContainerConfigurer
    {
        /// <summary>Configures the specified container.</summary>
        /// <param name="container">The container.</param>
        void Configure(SimpleMessageListenerContainer container);
    }

    /// <summary>
    /// A multiple poco listener.
    /// </summary>
    internal class MultiplePocoListener
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly AtomicInteger count = new AtomicInteger();

        private readonly CountdownEvent latch;

        /// <summary>Initializes a new instance of the <see cref="MultiplePocoListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        public MultiplePocoListener(CountdownEvent latch) { this.latch = latch; }

        /// <summary>Handles the message.</summary>
        /// <param name="value">The value.</param>
        public void HandleMessage(int value)
        {
            Logger.Debug(value + ":" + this.count.ReturnValueAndIncrement());
            this.latch.Signal();
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count { get { return this.count.Value; } }
    }
}
