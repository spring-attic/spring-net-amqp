using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Logging;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Test;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    public class MessageListenerManualAckIntegrationTests : IntegrationTestBase
    {
        private static ILog logger = LogManager.GetLogger(typeof(MessageListenerManualAckIntegrationTests));

        private static Queue queue = new Queue("test.queue");

        private RabbitTemplate template = new RabbitTemplate();

        private int concurrentConsumers = 1;

        private int messageCount = 50;

        private int txSize = 1;

        private bool transactional = false;

        private SimpleMessageListenerContainer container;

        //@Rule
        //public Log4jLevelAdjuster logLevels = new Log4jLevelAdjuster(Level.DEBUG, RabbitTemplate.class,
        //		SimpleMessageListenerContainer.class, BlockingQueueConsumer.class);

        //@Rule
        public BrokerRunning brokerIsRunning;

        /// <summary>
        /// Creates the connection factory.
        /// </summary>
        /// <remarks></remarks>
        [SetUp]
        public void CreateConnectionFactory()
        {
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue);
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.ChannelCacheSize = this.concurrentConsumers;
            connectionFactory.Port = BrokerTestUtils.GetPort();
            this.template.ConnectionFactory = connectionFactory;
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

        /// <summary>
        /// Tests the listener with manual ack non transactional.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestListenerWithManualAckNonTransactional()
        {
            var latch = new CountdownEvent(this.messageCount);
            this.container = this.CreateContainer(new TestListener(latch));
            for (var i = 0; i < this.messageCount; i++)
            {
                this.template.ConvertAndSend(queue.Name, i + "foo");
            }

            var timeout = Math.Min(1 + messageCount / (4 * concurrentConsumers), 30);
            logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");
            Assert.Null(this.template.ReceiveAndConvert(queue.Name));
        }

        /// <summary>
        /// Tests the listener with manual ack transactional.
        /// </summary>
        /// <remarks></remarks>
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
            logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            var waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");
            Assert.Null(this.template.ReceiveAndConvert(queue.Name));
        }

        /// <summary>
        /// Creates the container.
        /// </summary>
        /// <param name="listener">The listener.</param>
        /// <returns>The container.</returns>
        /// <remarks></remarks>
        private SimpleMessageListenerContainer CreateContainer(object listener)
        {
            var container = new SimpleMessageListenerContainer(this.template.ConnectionFactory);
            container.MessageListener = new MessageListenerAdapter(listener);
            container.QueueNames = new string[] { queue.Name };
            container.TxSize = this.txSize;
            container.PrefetchCount = this.txSize;
            container.ConcurrentConsumers = this.concurrentConsumers;
            container.IsChannelTransacted = this.transactional;
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.MANUAL;
            container.AfterPropertiesSet();
            container.Start();
            return container;
        }
    }

    /// <summary>
    /// A test listener.
    /// </summary>
    /// <remarks></remarks>
    public class TestListener : IChannelAwareMessageListener
    {
        private static ILog logger = LogManager.GetLogger(typeof(TestListener));
        private readonly CountdownEvent latch;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public TestListener(CountdownEvent latch)
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
            }
            finally
            {
                latch.Signal();
            }
        }
    }
}