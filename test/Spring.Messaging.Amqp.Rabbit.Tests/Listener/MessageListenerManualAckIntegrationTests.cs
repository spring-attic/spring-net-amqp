// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageListenerManualAckIntegrationTests.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>The message listener manual ack integration tests.</summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class MessageListenerManualAckIntegrationTests : AbstractRabbitIntegrationTest
    {
        private static readonly new ILog Logger = LogManager.GetCurrentClassLogger();

        private static readonly Queue queue = new Queue("test.queue");

        private readonly RabbitTemplate template = new RabbitTemplate();

        private int concurrentConsumers = 1;

        private int messageCount = 50;

        private int txSize = 1;

        private bool transactional;

        private SimpleMessageListenerContainer container;

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
        /// Creates the connection factory.
        /// </summary>
        [SetUp]
        public void CreateConnectionFactory()
        {
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue);
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
            Logger.Debug("Shutting down at end of test");
            if (this.container != null)
            {
                this.container.Shutdown();
            }
        }

        /// <summary>
        /// Tests the listener with manual ack non transactional.
        /// </summary>
        [Test]
        public void TestListenerWithManualAckNonTransactional()
        {
            var latch = new CountdownEvent(this.messageCount);
            this.container = this.CreateContainer(new TestListener(latch));
            for (var i = 0; i < this.messageCount; i++)
            {
                this.template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = Math.Min(1 + this.messageCount / (4 * this.concurrentConsumers), 30);
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");
            Assert.Null(this.template.ReceiveAndConvert(queue.Name));
        }

        /// <summary>
        /// Tests the listener with manual ack transactional.
        /// </summary>
        [Test]
        public void TestListenerWithManualAckTransactional()
        {
            this.transactional = true;
            var latch = new CountdownEvent(this.messageCount);
            this.container = this.CreateContainer(new TestListener(latch));
            for (var i = 0; i < this.messageCount; i++)
            {
                this.template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = Math.Min(1 + this.messageCount / (4 * this.concurrentConsumers), 30);
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");
            Assert.Null(this.template.ReceiveAndConvert(queue.Name));
        }

        /// <summary>Creates the container.</summary>
        /// <param name="listener">The listener.</param>
        /// <returns>The container.</returns>
        private SimpleMessageListenerContainer CreateContainer(object listener)
        {
            var container = new SimpleMessageListenerContainer(this.template.ConnectionFactory);
            container.MessageListener = new MessageListenerAdapter(listener);
            container.QueueNames = new[] { queue.Name };
            container.TxSize = this.txSize;
            container.PrefetchCount = this.txSize;
            container.ConcurrentConsumers = this.concurrentConsumers;
            container.ChannelTransacted = this.transactional;
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Manual;
            container.AfterPropertiesSet();
            container.Start();
            return container;
        }
    }

    /// <summary>
    /// A test listener.
    /// </summary>
    public class TestListener : IChannelAwareMessageListener
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly CountdownEvent latch;

        /// <summary>Initializes a new instance of the <see cref="TestListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        public TestListener(CountdownEvent latch) { this.latch = latch; }

        /// <summary>Handles the message.</summary>
        /// <param name="value">The value.</param>
        public void HandleMessage(string value) { }

        /// <summary>Called when [message].</summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        public void OnMessage(Message message, IModel channel)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            try
            {
                Logger.Debug("Acking: " + value);
                channel.BasicAck((ulong)message.MessageProperties.DeliveryTag, false);
            }
            finally
            {
                if (this.latch.CurrentCount > 0)
                {
                    this.latch.Signal();
                }
            }
        }
    }
}
