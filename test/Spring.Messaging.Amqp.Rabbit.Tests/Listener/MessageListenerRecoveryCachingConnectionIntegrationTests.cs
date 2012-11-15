// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageListenerRecoveryCachingConnectionIntegrationTests.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using IConnection = Spring.Messaging.Amqp.Rabbit.Connection.IConnection;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// Message listener recovery caching connection integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class MessageListenerRecoveryCachingConnectionIntegrationTests : AbstractRabbitIntegrationTest
    {
        private new static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private static readonly Queue queue = new Queue("test.queue");

        private static readonly Queue sendQueue = new Queue("test.send");

        private int concurrentConsumers = 1;

        private int messageCount = 10;

        private bool transactional;

        private AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;

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
        /// <returns>The connection factory.</returns>
        protected virtual IConnectionFactory CreateConnectionFactory()
        {
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.ChannelCacheSize = this.concurrentConsumers;
            connectionFactory.Port = BrokerTestUtils.GetPort();
            return connectionFactory;
        }

        /// <summary>The set up.</summary>
        [SetUp]
        public void SetUp()
        {
            this.concurrentConsumers = 1;
            this.messageCount = 10;
            this.transactional = false;
            this.acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;

            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue, sendQueue);
            this.brokerIsRunning.Apply();
            Assert.IsTrue(this.container == null);
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

            if (this.container != null)
            {
                this.container.Dispose();
            }

            this.container = null;
        }

        /// <summary>The test listener sends message and then container commits.</summary>
        [Test]
        public void TestListenerSendsMessageAndThenContainerCommits()
        {
            var connectionFactory = this.CreateConnectionFactory();
            var template = new RabbitTemplate(connectionFactory);
            new RabbitAdmin(connectionFactory).DeclareQueue(sendQueue);

            this.acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
            this.transactional = true;

            var latch = new CountdownEvent(1);
            this.container = this.CreateContainer(queue.Name, new ChannelSenderListener(sendQueue.Name, latch, false), connectionFactory);
            template.ConvertAndSend(queue.Name, "foo");

            var timeout = this.GetTimeout();
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(new TimeSpan(0, 0, timeout));
            Assert.True(waited, "Timed out waiting for message");

            Thread.Sleep(500);

            // All messages committed
            var bytes = (byte[])template.ReceiveAndConvert(sendQueue.Name);
            Assert.NotNull(bytes);
            Assert.AreEqual("bar", Encoding.UTF8.GetString(bytes));
            Assert.AreEqual(null, template.ReceiveAndConvert(queue.Name));
        }

        /// <summary>The test listener sends message and then rollback.</summary>
        [Test]
        public void TestListenerSendsMessageAndThenRollback()
        {
            var connectionFactory = this.CreateConnectionFactory();
            var template = new RabbitTemplate(connectionFactory);
            new RabbitAdmin(connectionFactory).DeclareQueue(sendQueue);

            this.acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
            this.transactional = true;

            var latch = new CountdownEvent(1);
            this.container = this.CreateContainer(queue.Name, new ChannelSenderListener(sendQueue.Name, latch, true), connectionFactory);
            template.ConvertAndSend(queue.Name, "foo");

            var timeout = this.GetTimeout();
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");

            this.container.Stop();
            Thread.Sleep(200);

            // Foo message is redelivered
            Assert.AreEqual("foo", template.ReceiveAndConvert(queue.Name));

            // Sending of bar message is also rolled back
            Assert.Null(template.ReceiveAndConvert(sendQueue.Name));
        }

        /// <summary>The test listener recovers from bogus double ack.</summary>
        [Test]
        public void TestListenerRecoversFromBogusDoubleAck()
        {
            var template = new RabbitTemplate(this.CreateConnectionFactory());

            this.acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Manual;

            var latch = new CountdownEvent(this.messageCount);
            this.container = this.CreateContainer(queue.Name, new ManualAckListener(latch), this.CreateConnectionFactory());
            for (var i = 0; i < this.messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = this.GetTimeout();
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(new TimeSpan(0, 0, timeout));
            Assert.True(waited, "Timed out waiting for message");

            Assert.Null(template.ReceiveAndConvert(queue.Name));
        }

        /// <summary>The test listener recovers from closed channel.</summary>
        [Test]
        public void TestListenerRecoversFromClosedChannel()
        {
            var template = new RabbitTemplate(this.CreateConnectionFactory());

            var latch = new CountdownEvent(this.messageCount);
            this.container = this.CreateContainer(queue.Name, new AbortChannelListener(latch), this.CreateConnectionFactory());
            for (var i = 0; i < this.messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = this.GetTimeout();
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(new TimeSpan(0, 0, timeout));
            Assert.True(waited, "Timed out waiting for message");

            Assert.Null(template.ReceiveAndConvert(queue.Name));
        }

        /// <summary>The test listener recovers from closed channel and stop.</summary>
        [Test]
        public void TestListenerRecoversFromClosedChannelAndStop()
        {
            var template = new RabbitTemplate(this.CreateConnectionFactory());

            var latch = new CountdownEvent(this.messageCount);
            this.container = this.CreateContainer(queue.Name, new AbortChannelListener(latch), this.CreateConnectionFactory());

            var n = 0;
            while (n++ < 100 && this.container.ActiveConsumerCount != this.concurrentConsumers)
            {
                Thread.Sleep(50);
            }

            Assert.AreEqual(this.concurrentConsumers, this.container.ActiveConsumerCount);

            for (var i = 0; i < this.messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = this.GetTimeout();
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(new TimeSpan(0, 0, timeout));
            Assert.True(waited, "Timed out waiting for message");

            Assert.Null(template.ReceiveAndConvert(queue.Name));

            Assert.AreEqual(this.concurrentConsumers, this.container.ActiveConsumerCount);
            this.container.Stop();
            Assert.AreEqual(0, this.container.ActiveConsumerCount);
        }

        /// <summary>The test listener recovers from closed connection.</summary>
        [Test]
        public void TestListenerRecoversFromClosedConnection()
        {
            var template = new RabbitTemplate(this.CreateConnectionFactory());

            var latch = new CountdownEvent(this.messageCount);
            var connectionFactory = this.CreateConnectionFactory();
            this.container = this.CreateContainer(queue.Name, new CloseConnectionListener((IConnectionProxy)connectionFactory.CreateConnection(), latch), connectionFactory);
            for (var i = 0; i < this.messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = Math.Min(4 + (this.messageCount / (4 * this.concurrentConsumers)), 30);
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(new TimeSpan(0, 0, timeout));
            Assert.True(waited, "Timed out waiting for message");

            Assert.Null(template.ReceiveAndConvert(queue.Name));
        }

        /// <summary>The test listener recovers and template shares connection factory.</summary>
        [Test]
        public void TestListenerRecoversAndTemplateSharesConnectionFactory()
        {
            var connectionFactory = this.CreateConnectionFactory();
            var template = new RabbitTemplate(connectionFactory);

            this.acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Manual;

            var latch = new CountdownEvent(this.messageCount);
            this.container = this.CreateContainer(queue.Name, new ManualAckListener(latch), connectionFactory);
            for (var i = 0; i < this.messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = this.GetTimeout();
            Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(new TimeSpan(0, 0, timeout));
            Assert.True(waited, "Timed out waiting for message");

            Assert.Null(template.ReceiveAndConvert(queue.Name));
        }

        /// <summary>The test listener does not recover from missing queue.</summary>
        [Test]
        public void TestListenerDoesNotRecoverFromMissingQueue()
        {
            try
            {
                this.concurrentConsumers = 3;
                var latch = new CountdownEvent(this.messageCount);
                this.container = this.CreateContainer("nonexistent", new VanillaListener(latch), this.CreateConnectionFactory());
            }
            catch (Exception e)
            {
                Assert.True(e is AmqpIllegalStateException);
                this.concurrentConsumers = 1;
            }
        }

        /// <summary>The test single listener does not recover from missing queue.</summary>
        [Test]
        public void TestSingleListenerDoesNotRecoverFromMissingQueue()
        {
            try
            {
                /*
                 * A single listener sometimes doesn't have time to attempt to start before we ask it if it has failed, so this
                 * is a good test of that potential bug.
                 */
                this.concurrentConsumers = 1;
                var latch = new CountdownEvent(this.messageCount);
                this.container = this.CreateContainer("nonexistent", new VanillaListener(latch), this.CreateConnectionFactory());
            }
            catch (Exception e)
            {
                Assert.True(e is AmqpIllegalStateException);
            }
        }

        /// <summary>
        /// Gets the timeout.
        /// </summary>
        /// <returns>The timeout.</returns>
        private int GetTimeout()
        {
            // return 1000;
            return Math.Min(1 + (this.messageCount / (4 * this.concurrentConsumers)), 30);
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
            container.ConcurrentConsumers = this.concurrentConsumers;
            container.ChannelTransacted = this.transactional;
            container.AcknowledgeMode = this.acknowledgeMode;
            container.AfterPropertiesSet();
            container.Start();
            return container;
        }
    }

    /// <summary>
    /// A manual ack listener.
    /// </summary>
    public class ManualAckListener : IChannelAwareMessageListener
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly AtomicBoolean failed = new AtomicBoolean(false);

        private readonly CountdownEvent latch;

        /// <summary>Initializes a new instance of the <see cref="ManualAckListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        public ManualAckListener(CountdownEvent latch) { this.latch = latch; }

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
                if (this.failed.CompareAndSet(false, true))
                {
                    // intentional error (causes exception on connection thread):
                    channel.BasicAck((ulong)message.MessageProperties.DeliveryTag, false);
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

    /// <summary>
    /// A channel sender listener.
    /// </summary>
    public class ChannelSenderListener : IChannelAwareMessageListener
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly CountdownEvent latch;

        private readonly bool fail;

        private readonly string queueName;

        /// <summary>Initializes a new instance of the <see cref="ChannelSenderListener"/> class.</summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="latch">The latch.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        public ChannelSenderListener(string queueName, CountdownEvent latch, bool fail)
        {
            this.queueName = queueName;
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
                Logger.Debug(m => m("Received: {0} Sending: bar", value));
                channel.BasicPublish(string.Empty, this.queueName, null, Encoding.UTF8.GetBytes("bar"));
                if (this.fail)
                {
                    Logger.Debug(m => m("Failing (planned)"));

                    // intentional error (causes exception on connection thread):
                    throw new Exception("Planned");
                }
            }
            finally
            {
                if (this.latch.CurrentCount > 0)
                {
                    this.latch.Signal();
                    Logger.Debug(m => m("Latch Count: {0}", this.latch.CurrentCount));
                }
            }
        }
    }

    /// <summary>
    /// An abort channel listener.
    /// </summary>
    public class AbortChannelListener : IChannelAwareMessageListener
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly AtomicBoolean failed = new AtomicBoolean(false);

        private readonly CountdownEvent latch;

        /// <summary>Initializes a new instance of the <see cref="AbortChannelListener"/> class.</summary>
        /// <param name="latch">The latch.</param>
        public AbortChannelListener(CountdownEvent latch) { this.latch = latch; }

        /// <summary>Called when [message].</summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        public void OnMessage(Message message, IModel channel)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            Logger.Debug(m => m("Receiving: {0}", value));
            if (this.failed.CompareAndSet(false, true))
            {
                // intentional error (causes exception on connection thread):
                channel.Abort();
            }
            else
            {
                if (this.latch.CurrentCount > 0)
                {
                    this.latch.Signal();
                    Logger.Debug(m => m("Latch Count: {0}", this.latch.CurrentCount));
                }
            }
        }
    }

    /// <summary>
    /// A close connection listener.
    /// </summary>
    public class CloseConnectionListener : IChannelAwareMessageListener
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly AtomicBoolean failed = new AtomicBoolean(false);

        private readonly CountdownEvent latch;

        private readonly IConnection connection;

        /// <summary>Initializes a new instance of the <see cref="CloseConnectionListener"/> class.</summary>
        /// <param name="connection">The connection.</param>
        /// <param name="latch">The latch.</param>
        public CloseConnectionListener(IConnectionProxy connection, CountdownEvent latch)
        {
            this.connection = connection.GetTargetConnection();
            this.latch = latch;
        }

        /// <summary>Called when [message].</summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        public void OnMessage(Message message, IModel channel)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            Logger.Debug(m => m("Receiving: {0}", value));
            if (this.failed.CompareAndSet(false, true))
            {
                // intentional error (causes exception on connection thread):
                this.connection.Close();
            }
            else
            {
                if (this.latch.CurrentCount > 0)
                {
                    this.latch.Signal();
                    Logger.Debug(m => m("Latch Count: {0}", this.latch.CurrentCount));
                }
            }
        }
    }
}
