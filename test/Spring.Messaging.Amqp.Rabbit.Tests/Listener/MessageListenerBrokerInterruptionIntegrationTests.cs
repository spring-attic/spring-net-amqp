using System;
using System.Collections.Generic;
using System.IO;
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
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Test;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    /// Message listener broker interruption integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class MessageListenerBrokerInterruptionIntegrationTests : AbstractRabbitIntegrationTest
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private static ILog logger = LogManager.GetLogger(typeof(MessageListenerBrokerInterruptionIntegrationTests));

        /// <summary>
        /// The queue. Ensure it is durable or it won't survive the broker restart.
        /// </summary>
        private Queue queue = new Queue("test.queue", true);

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
        private AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.AUTO;

        /// <summary>
        /// The container.
        /// </summary>
        private SimpleMessageListenerContainer container;

        //@Rule
        public static EnvironmentAvailable environment = new EnvironmentAvailable("BROKER_INTEGRATION_TEST");

        /*
         * Ensure broker dies if a test fails (otherwise the erl process might have to be killed manually)
         */
        //@Rule
        //public static BrokerPanic panic = new BrokerPanic();

        private IConnectionFactory connectionFactory;

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

            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(this.queue);
        }

        /// <summary>
        /// Creates the connection factory.
        /// </summary>
        /// <remarks></remarks>
        [SetUp]
        public void CreateConnectionFactory()
        {
            if (environment.IsActive())
            {
                var connectionFactory = new CachingConnectionFactory();
                connectionFactory.ChannelCacheSize = this.concurrentConsumers;
                this.connectionFactory = connectionFactory;
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <remarks></remarks>
        [TearDown]
        public void Clear()
        {
            if (environment.IsActive())
            {
                // Wait for broker communication to finish before trying to stop container
                Thread.Sleep(300);
                logger.Debug("Shutting down at end of test");
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
        /// <remarks></remarks>
        [Test]
        [Ignore("Need to fix")]
        public void TestListenerRecoversFromDeadBroker()
        {
            var queues = this.brokerAdmin.GetQueues();
            logger.Info("Queues: " + queues);
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
            logger.Info("Queues: " + queues);
            container.Start();
            Logger.Info(string.Format("Concurrent Consumers After Container Start: {0}", this.container.ActiveConsumerCount));
            Assert.AreEqual(this.concurrentConsumers, this.container.ActiveConsumerCount);
            Logger.Info(string.Format("Latch.CurrentCount After Container Start: {0}", latch.CurrentCount));
            var timeout = Math.Min(4 + this.messageCount / (4 * this.concurrentConsumers), 30);
            logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
            waited = latch.Wait(timeout * 1000);
            Assert.True(waited, "Timed out waiting for message");

            Assert.IsNull(template.ReceiveAndConvert(this.queue.Name));
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
            container.TxSize = this.txSize;
            container.PrefetchCount = this.txSize;
            container.ConcurrentConsumers = this.concurrentConsumers;
            container.IsChannelTransacted = this.transactional;
            container.AcknowledgeMode = this.acknowledgeMode;
            container.AfterPropertiesSet();
            container.Start();
            return container;
        }


    }

    /// <summary>
    /// A vanilla message listener.
    /// </summary>
    /// <remarks></remarks>
    public class VanillaListener : IChannelAwareMessageListener
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private static ILog logger = LogManager.GetLogger(typeof(VanillaListener));

        /// <summary>
        /// The latch.
        /// </summary>
        private readonly CountdownEvent latch;

        /// <summary>
        /// Initializes a new instance of the <see cref="VanillaListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public VanillaListener(CountdownEvent latch)
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
            Thread.Sleep(75);
            if(this.latch.CurrentCount > 0) this.latch.Signal();
        }
    }
}
