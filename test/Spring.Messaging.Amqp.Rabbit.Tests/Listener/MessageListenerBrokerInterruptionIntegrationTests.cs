// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageListenerBrokerInterruptionIntegrationTests.cs" company="The original author or authors.">
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
using System.IO;
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
    /// <summary>
    /// Message listener broker interruption integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class MessageListenerBrokerInterruptionIntegrationTests : AbstractRabbitIntegrationTest
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        private new static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The queue. Ensure it is durable or it won't survive the broker restart.
        /// </summary>
        private readonly Queue queue = new Queue("test.queue", true);

        /// <summary>
        /// Concurrent consumers.
        /// </summary>
        private int concurrentConsumers = 2;

        /// <summary>
        /// The message count.
        /// </summary>
        private int messageCount = 60;

        /// <summary>
        /// The transaction size.
        /// </summary>
        private int txSize = 1;

        /// <summary>
        /// The transactional flag.
        /// </summary>
        private bool transactional = false;

        /// <summary>
        /// The acknowledge mode.
        /// </summary>
        private AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;

        /// <summary>
        /// The container.
        /// </summary>
        private SimpleMessageListenerContainer container;

        // @Rule
        public new static EnvironmentAvailable environment = new EnvironmentAvailable("BROKER_INTEGRATION_TEST");

        /*
         * Ensure broker dies if a test fails (otherwise the erl process might have to be killed manually)
         */
        // @Rule
        // public static BrokerPanic panic = new BrokerPanic();
        private IConnectionFactory connectionFactory;

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
        /// Initializes a new instance of the <see cref="MessageListenerBrokerInterruptionIntegrationTests"/> class. 
        /// </summary>
        /// <remarks>
        /// </remarks>
        public MessageListenerBrokerInterruptionIntegrationTests()
        {
            try
            {
                var directory = new DirectoryInfo("target/rabbitmq");
                if (directory.Exists)
                {
                    directory.Delete(true);
                }
            }
            catch (Exception)
            {
                Logger.Error("Could not delete directory. Assuming broker is running.");
            }

            // this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(this.queue);
            // this.brokerIsRunning.Apply();
        }

        /// <summary>
        /// Creates the connection factory.
        /// </summary>
        [SetUp]
        public void CreateConnectionFactory()
        {
            if (environment.IsActive())
            {
                var connectionFactory = new CachingConnectionFactory();
                connectionFactory.ChannelCacheSize = this.concurrentConsumers;
                connectionFactory.Port = BrokerTestUtils.GetAdminPort();
                this.connectionFactory = connectionFactory;
                this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(this.queue);
                this.brokerIsRunning.Apply();
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        [TearDown]
        public void Clear()
        {
            if (environment.IsActive())
            {
                // Wait for broker communication to finish before trying to stop container
                Thread.Sleep(300);
                Logger.Debug("Shutting down at end of test");
                if (this.container != null)
                {
                    this.container.Shutdown();
                }

                this.brokerAdmin.StopNode();

                // Remove all trace of the durable queue...
                var directory = new DirectoryInfo("target/rabbitmq");
                if (directory.Exists)
                {
                    directory.Delete(true);
                }
            }
        }

        /// <summary>
        /// Tests the listener recovers from dead broker.
        /// </summary>
        [Test]
        public void TestListenerRecoversFromDeadBroker()
        {
            var queues = this.brokerAdmin.GetQueues();
            Logger.Info("Queues: " + queues);
            Assert.AreEqual(1, queues.Count);
            Assert.True(queues[0].Durable);

            var template = new RabbitTemplate(this.connectionFactory);

            var latch = new CountdownEvent(this.messageCount);
            Assert.AreEqual(this.messageCount, latch.CurrentCount, "No more messages to receive before even sent!");
            this.container = this.CreateContainer(this.queue.Name, new VanillaListener(latch), this.connectionFactory);
            for (var i = 0; i < this.messageCount; i++)
            {
                template.ConvertAndSend(this.queue.Name, i + "foo");
            }

            Assert.True(latch.CurrentCount > 0, "No more messages to receive before broker stopped");
            Logger.Info(string.Format("Latch.CurrentCount Before Shutdown: {0}", latch.CurrentCount));
            this.brokerAdmin.StopBrokerApplication();
            Assert.True(latch.CurrentCount > 0, "No more messages to receive after broker stopped");
            Logger.Info(string.Format("Latch.CurrentCount After Shutdown: {0}", latch.CurrentCount));
            var waited = latch.Wait(500);
            Assert.False(waited, "Did not time out waiting for message");

            this.container.Stop();
            Assert.AreEqual(0, this.container.ActiveConsumerCount);
            Logger.Info(string.Format("Latch.CurrentCount After Container Stop: {0}", latch.CurrentCount));
            this.brokerAdmin.StartBrokerApplication();
            queues = this.brokerAdmin.GetQueues();
            Logger.Info("Queues: " + queues);
            this.container.Start();
            Logger.Info(string.Format("Concurrent Consumers After Container Start: {0}", this.container.ActiveConsumerCount));
            Assert.AreEqual(this.concurrentConsumers, this.container.ActiveConsumerCount);
            Logger.Info(string.Format("Latch.CurrentCount After Container Start: {0}", latch.CurrentCount));
            var timeout = Math.Min((4 + this.messageCount) / (4 * this.concurrentConsumers), 30);
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");

            Assert.IsNull(template.ReceiveAndConvert(this.queue.Name));
        }

        /// <summary>Creates the container.</summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="listener">The listener.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <returns>The container.</returns>
        private SimpleMessageListenerContainer CreateContainer(string queueName, object listener, IConnectionFactory connectionFactory)
        {
            var container = new SimpleMessageListenerContainer(connectionFactory);
            container.MessageListener = new MessageListenerAdapter(listener);
            container.QueueNames = new[] { queueName };
            container.TxSize = this.txSize;
            container.PrefetchCount = this.txSize;
            container.ConcurrentConsumers = this.concurrentConsumers;
            container.ChannelTransacted = this.transactional;
            container.AcknowledgeMode = this.acknowledgeMode;
            container.AfterPropertiesSet();
            container.Start();
            return container;
        }
    }

    /// <summary>
    /// A vanilla message listener.
    /// </summary>
    public class VanillaListener : IChannelAwareMessageListener
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The latch.
        /// </summary>
        private readonly CountdownEvent latch;

        /// <summary>Initializes a new instance of the <see cref="VanillaListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        public VanillaListener(CountdownEvent latch) { this.latch = latch; }

        /// <summary>Called when [message].</summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        public void OnMessage(Message message, IModel channel)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            Logger.Debug("Receiving: " + value);
            if (this.latch.CurrentCount > 0)
            {
                this.latch.Signal();
            }
        }
    }
}
