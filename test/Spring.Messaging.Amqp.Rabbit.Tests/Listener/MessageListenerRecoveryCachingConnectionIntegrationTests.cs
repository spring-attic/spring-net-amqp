using System;
using System.Collections.Generic;
using System.Linq;
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
using Spring.Threading.AtomicTypes;
using IConnection = Spring.Messaging.Amqp.Rabbit.Connection.IConnection;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// Message listener recovery caching connection integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    [Ignore("Some state issue (within the test fixture) is causing unreliable execution of these tests")]
    public class MessageListenerRecoveryCachingConnectionIntegrationTests : AbstractRabbitIntegrationTest
    {
        private static ILog logger = LogManager.GetLogger(typeof(MessageListenerRecoveryCachingConnectionIntegrationTests));

        private static Queue queue = new Queue("test.queue");

        private static Queue sendQueue = new Queue("test.send");

        private int concurrentConsumers = 1;

        private int messageCount = 10;

        private bool transactional = false;

        private AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;

        private SimpleMessageListenerContainer container;

        #region Fixture Setup and Teardown
        /// <summary>
        /// Code to execute before fixture setup.
        /// </summary>
        public override void BeforeFixtureSetUp()
        {
        }

        /// <summary>
        /// Code to execute before fixture teardown.
        /// </summary>
        public override void BeforeFixtureTearDown()
        {
        }

        /// <summary>
        /// Code to execute after fixture setup.
        /// </summary>
        public override void AfterFixtureSetUp()
        {
        }

        /// <summary>
        /// Code to execute after fixture teardown.
        /// </summary>
        public override void AfterFixtureTearDown()
        {
        }
        #endregion
        
        /// <summary>
        /// Creates the connection factory.
        /// </summary>
        /// <returns>The connection factory.</returns>
        /// <remarks></remarks>
        protected IConnectionFactory CreateConnectionFactory()
        {
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.ChannelCacheSize = this.concurrentConsumers;
            connectionFactory.Port = BrokerTestUtils.GetPort();
            return connectionFactory;
        }

        [SetUp]
        public void SetUp()
        {
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue, sendQueue);
            this.brokerIsRunning.Apply();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <remarks></remarks>
        [TearDown]
        public void Clear()
        {
            // Wait for broker communication to finish before trying to stop container
            Thread.Sleep(300);
            logger.Debug("Shutting down at end of test");
            if (this.container != null)
            {
                this.container.Shutdown();
            }
        }

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
            logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");

            // All messages committed
            var bytes = (byte[])template.ReceiveAndConvert(sendQueue.Name);
            Assert.NotNull(bytes);
            Assert.AreEqual("bar", Encoding.UTF8.GetString(bytes));
            Assert.AreEqual(null, template.ReceiveAndConvert(queue.Name));
        }

        [Test]
        public void TestListenerSendsMessageAndThenRollback()
        {
            var connectionFactory = CreateConnectionFactory();
            var template = new RabbitTemplate(connectionFactory);
            new RabbitAdmin(connectionFactory).DeclareQueue(sendQueue);

            acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
            transactional = true;

            var latch = new CountdownEvent(1);
            container = CreateContainer(queue.Name, new ChannelSenderListener(sendQueue.Name, latch, true), connectionFactory);
            template.ConvertAndSend(queue.Name, "foo");

            var timeout = GetTimeout();
            logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");

            container.Stop();
            Thread.Sleep(200);

            // Foo message is redelivered
            Assert.AreEqual("foo", template.ReceiveAndConvert(queue.Name));
            // Sending of bar message is also rolled back
            Assert.Null(template.ReceiveAndConvert(sendQueue.Name));
        }

        [Test]
        [Ignore("Need to fix")]
        public void TestListenerRecoversFromBogusDoubleAck()
        {

            var template = new RabbitTemplate(CreateConnectionFactory());

            acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Manual;

            var latch = new CountdownEvent(messageCount);
            container = CreateContainer(queue.Name, new ManualAckListener(latch), CreateConnectionFactory());
            for (var i = 0; i < messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = GetTimeout();
            logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");

            Assert.Null(template.ReceiveAndConvert(queue.Name));

        }

        [Test]
        [Ignore("Need to fix")]
        public void TestListenerRecoversFromClosedChannel()
        {

            var template = new RabbitTemplate(CreateConnectionFactory());

            var latch = new CountdownEvent(messageCount);
            container = CreateContainer(queue.Name, new AbortChannelListener(latch), CreateConnectionFactory());
            for (var i = 0; i < messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = GetTimeout();
            logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");

            Assert.Null(template.ReceiveAndConvert(queue.Name));

        }

        [Test]
        [Ignore("Need to fix")]
        public void TestListenerRecoversFromClosedChannelAndStop()
        {

            var template = new RabbitTemplate(CreateConnectionFactory());

            var latch = new CountdownEvent(messageCount);
            container = CreateContainer(queue.Name, new AbortChannelListener(latch), CreateConnectionFactory());
            Thread.Sleep(500);
            Assert.AreEqual(concurrentConsumers, container.ActiveConsumerCount);

            for (var i = 0; i < messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = GetTimeout();
            logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");

            Assert.Null(template.ReceiveAndConvert(queue.Name));

            Assert.AreEqual(concurrentConsumers, container.ActiveConsumerCount);
            container.Stop();
            Assert.AreEqual(0, container.ActiveConsumerCount);

        }

        [Test]
        [Ignore("Need to fix")]
        public void testListenerRecoversFromClosedConnection()
        {

            var template = new RabbitTemplate(CreateConnectionFactory());

            var latch = new CountdownEvent(messageCount);
            var connectionFactory = CreateConnectionFactory();
            container = CreateContainer(queue.Name, new CloseConnectionListener((IConnectionProxy)connectionFactory.CreateConnection(), latch), connectionFactory);
            for (var i = 0; i < messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = Math.Min(4 + messageCount / (4 * concurrentConsumers), 30);
            logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");

            Assert.Null(template.ReceiveAndConvert(queue.Name));

        }

        [Test]
        [Ignore("Need to fix")]
        public void TestListenerRecoversAndTemplateSharesConnectionFactory()
        {

            var connectionFactory = CreateConnectionFactory();
            var template = new RabbitTemplate(connectionFactory);

            acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Manual;

            var latch = new CountdownEvent(messageCount);
            container = CreateContainer(queue.Name, new ManualAckListener(latch), connectionFactory);
            for (var i = 0; i < messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = GetTimeout();
            logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");

            Assert.Null(template.ReceiveAndConvert(queue.Name));

        }

        [Test]
        public void TestListenerDoesNotRecoverFromMissingQueue()
        {
            try
            {
                concurrentConsumers = 3;
                var latch = new CountdownEvent(messageCount);
                container = CreateContainer("nonexistent", new VanillaListener(latch), CreateConnectionFactory());
            }
            catch (Exception e)
            {
                Assert.True(e is AmqpIllegalStateException);
            }
        }

        [Test]
        public void testSingleListenerDoesNotRecoverFromMissingQueue()
        {
            try
            {
                /*
                 * A single listener sometimes doesn't have time to attempt to start before we ask it if it has failed, so this
                 * is a good test of that potential bug.
                 */
                concurrentConsumers = 1;
                var latch = new CountdownEvent(messageCount);
                container = CreateContainer("nonexistent", new VanillaListener(latch), CreateConnectionFactory());
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
        /// <remarks></remarks>
        private int GetTimeout()
        {
            return Math.Min(15 + this.messageCount / (4 * this.concurrentConsumers), 30);
        }

        /// <summary>
        /// Creates the container.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="listener">The listener.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <returns>The container.</returns>
        /// <remarks></remarks>
        private SimpleMessageListenerContainer CreateContainer(string queueName, object listener, IConnectionFactory connectionFactory)
        {
            var container = new SimpleMessageListenerContainer(connectionFactory);
            container.MessageListener = new MessageListenerAdapter(listener);
            container.QueueNames = new string[] { queueName };
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
    /// <remarks></remarks>
    public class ManualAckListener : IChannelAwareMessageListener
    {
        private static ILog logger = LogManager.GetLogger(typeof(ManualAckListener));

        private AtomicBoolean failed = new AtomicBoolean(false);

        private readonly CountdownEvent latch;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualAckListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public ManualAckListener(CountdownEvent latch)
        {
            this.latch = latch;
        }

        /// <summary>
        /// Called when [message].
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <remarks></remarks>
        public void OnMessage(Message message, IModel channel)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            try
            {
                logger.Debug("Acking: " + value);
                channel.BasicAck((ulong)message.MessageProperties.DeliveryTag, false);
                if (this.failed.CompareAndSet(false, true))
                {
                    // intentional error (causes exception on connection thread):
                    channel.BasicAck((ulong)message.MessageProperties.DeliveryTag, false);
                }
            }
            finally
            {
                if (this.latch.CurrentCount > 0) this.latch.Signal();
            }
        }
    }

    /// <summary>
    /// A channel sender listener.
    /// </summary>
    /// <remarks></remarks>
    public class ChannelSenderListener : IChannelAwareMessageListener
    {
        private static ILog logger = LogManager.GetLogger(typeof(ChannelSenderListener));

        private readonly CountdownEvent latch;

        private readonly bool fail;

        private readonly string queueName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelSenderListener"/> class.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="latch">The latch.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        /// <remarks></remarks>
        public ChannelSenderListener(string queueName, CountdownEvent latch, bool fail)
        {
            this.queueName = queueName;
            this.latch = latch;
            this.fail = fail;
        }

        /// <summary>
        /// Called when [message].
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <remarks></remarks>
        public void OnMessage(Message message, IModel channel)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            try
            {
                logger.Debug("Received: " + value + " Sending: bar");
                channel.BasicPublish(string.Empty, this.queueName, null, Encoding.UTF8.GetBytes("bar"));
                if (this.fail)
                {
                    logger.Debug("Failing (planned)");

                    // intentional error (causes exception on connection thread):
                    throw new Exception("Planned");
                }
            }
            finally
            {
                if (this.latch.CurrentCount > 0) this.latch.Signal();
            }
        }
    }

    /// <summary>
    /// An abort channel listener.
    /// </summary>
    /// <remarks></remarks>
    public class AbortChannelListener : IChannelAwareMessageListener
    {
        private static ILog logger = LogManager.GetLogger(typeof(AbortChannelListener));

        private AtomicBoolean failed = new AtomicBoolean(false);

        private readonly CountdownEvent latch;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbortChannelListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public AbortChannelListener(CountdownEvent latch)
        {
            this.latch = latch;
        }

        /// <summary>
        /// Called when [message].
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <remarks></remarks>
        public void OnMessage(Message message, IModel channel)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            logger.Debug("Receiving: " + value);
            if (this.failed.CompareAndSet(false, true))
            {
                // intentional error (causes exception on connection thread):
                channel.Abort();
            }
            else
            {
                if (this.latch.CurrentCount > 0) this.latch.Signal();
            }
        }
    }

    /// <summary>
    /// A close connection listener.
    /// </summary>
    /// <remarks></remarks>
    public class CloseConnectionListener : IChannelAwareMessageListener
    {
        private static ILog logger = LogManager.GetLogger(typeof(CloseConnectionListener));

        private AtomicBoolean failed = new AtomicBoolean(false);

        private readonly CountdownEvent latch;

        private readonly IConnection connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloseConnectionListener"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public CloseConnectionListener(IConnectionProxy connection, CountdownEvent latch)
        {
            this.connection = connection.GetTargetConnection();
            this.latch = latch;
        }

        /// <summary>
        /// Called when [message].
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <remarks></remarks>
        public void OnMessage(Message message, IModel channel)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            logger.Debug("Receiving: " + value);
            if (this.failed.CompareAndSet(false, true))
            {
                // intentional error (causes exception on connection thread):
                this.connection.Close();
            }
            else
            {
                if (this.latch.CurrentCount > 0) this.latch.Signal();
            }
        }
    }
}