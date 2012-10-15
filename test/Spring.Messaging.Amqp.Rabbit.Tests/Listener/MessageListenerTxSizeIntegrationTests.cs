// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageListenerTxSizeIntegrationTests.cs" company="The original author or authors.">
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
    /// <summary>The message listener tx size integration tests.</summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class MessageListenerTxSizeIntegrationTests : AbstractRabbitIntegrationTest
    {
        private new static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly Queue queue = new Queue("test.queue");

        private readonly RabbitTemplate template = new RabbitTemplate();

        private int concurrentConsumers = 1;

        private int messageCount = 12;

        public int txSize = 4;

        private bool transactional = true;

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

        /// <summary>The create connection factory.</summary>
        [SetUp]
        public void CreateConnectionFactory()
        {
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(this.queue);
            this.brokerIsRunning.Apply();
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.ChannelCacheSize = this.concurrentConsumers;
            connectionFactory.Port = BrokerTestUtils.GetPort();
            this.template.ConnectionFactory = connectionFactory;
        }

        /// <summary>The clear.</summary>
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

        /// <summary>The test listener transactional sunny day.</summary>
        [Test]
        public void TestListenerTransactionalSunnyDay()
        {
            this.transactional = true;
            var latch = new CountdownEvent(this.messageCount);
            this.container = this.CreateContainer(new TxTestListener(latch, false, this));
            for (int i = 0; i < this.messageCount; i++)
            {
                this.template.ConvertAndSend(this.queue.Name, i + "foo");
            }

            int timeout = Math.Min(1 + this.messageCount / (4 * this.concurrentConsumers), 30);
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(new TimeSpan(0, 0, 0, timeout));
            Assert.True(waited, "Timed out waiting for message");
            Assert.Null(this.template.ReceiveAndConvert(this.queue.Name));
        }

        /// <summary>The test listener transactional fails.</summary>
        [Test]
        [Ignore("Need to fix")]
        public void TestListenerTransactionalFails()
        {
            this.transactional = true;
            var latch = new CountdownEvent(this.messageCount);
            this.container = this.CreateContainer(new TxTestListener(latch, true, this));
            for (var i = 0; i < this.txSize; i++)
            {
                this.template.ConvertAndSend(this.queue.Name, i + "foo");
            }

            var timeout = Math.Min(1 + this.messageCount / (4 * this.concurrentConsumers), 30);
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(new TimeSpan(0, 0, 0, timeout));
            Assert.True(waited, "Timed out waiting for message");
            Assert.Null(this.template.ReceiveAndConvert(this.queue.Name));
        }

        private SimpleMessageListenerContainer CreateContainer(object listener)
        {
            var container = new SimpleMessageListenerContainer(this.template.ConnectionFactory);
            container.MessageListener = new MessageListenerAdapter(listener);
            container.QueueNames = new[] { this.queue.Name };
            container.TxSize = this.txSize;
            container.PrefetchCount = this.txSize;
            container.ConcurrentConsumers = this.concurrentConsumers;
            container.ChannelTransacted = this.transactional;
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
            container.AfterPropertiesSet();
            container.Start();
            return container;
        }
    }

    /// <summary>
    /// A Tx Test Listener
    /// </summary>
    public class TxTestListener : IChannelAwareMessageListener
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly ThreadLocal<int> count = new ThreadLocal<int>();
        private readonly MessageListenerTxSizeIntegrationTests outer;

        private readonly CountdownEvent latch;

        private readonly bool fail;

        /// <summary>Initializes a new instance of the <see cref="TxTestListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        /// <param name="outer">The outer.</param>
        public TxTestListener(CountdownEvent latch, bool fail, MessageListenerTxSizeIntegrationTests outer)
        {
            this.latch = latch;
            this.fail = fail;
            this.outer = outer;
        }

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
                Logger.Debug("Received: " + value);
                if (this.count.Value == null)
                {
                    this.count.Value = 1;
                }
                else
                {
                    this.count.Value = this.count.Value + 1;
                }

                if (this.count.Value == this.outer.txSize && this.fail)
                {
                    Logger.Debug("Failing: " + value);
                    this.count.Value = 0;
                    throw new SystemException("Planned");
                }
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
