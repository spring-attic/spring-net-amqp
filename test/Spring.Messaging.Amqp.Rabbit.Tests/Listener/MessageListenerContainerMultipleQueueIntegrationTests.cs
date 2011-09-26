using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AutoMoq;
using Common.Logging;
using Moq;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Test;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Threading.AtomicTypes;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    public class MessageListenerContainerMultipleQueueIntegrationTests : AbstractRabbitIntegrationTest
    {
        private static ILog logger = LogManager.GetLogger(typeof(MessageListenerContainerMultipleQueueIntegrationTests));

        private static Queue queue1 = new Queue("test.queue.1");

        private static Queue queue2 = new Queue("test.queue.2");

        //@Rule
        public BrokerRunning brokerIsRunningAndQueue1Empty;

        //@Rule
        public BrokerRunning brokerIsRunningAndQueue2Empty;

        //@Rule
        //public Log4jLevelAdjuster logLevels = new Log4jLevelAdjuster(Level.INFO, RabbitTemplate.class,
        //		SimpleMessageListenerContainer.class, BlockingQueueConsumer.class);

        //@Rule
        //public ExpectedException exception = ExpectedException.none();

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
        /// Sets up.
        /// </summary>
        /// <remarks></remarks>
        [SetUp]
        public void SetUp()
        {
            this.brokerIsRunningAndQueue1Empty = BrokerRunning.IsRunningWithEmptyQueues(queue1);
            this.brokerIsRunningAndQueue2Empty = BrokerRunning.IsRunningWithEmptyQueues(queue2);
        }

        /// <summary>
        /// Tests the multiple queues.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestMultipleQueues()
        {
            var mocker = new AutoMoqer();

            var mockConfigurer = mocker.GetMock<IContainerConfigurer>();
            mockConfigurer.Setup(c => c.Configure(It.IsAny<SimpleMessageListenerContainer>())).Callback<SimpleMessageListenerContainer>((container) => container.Queues = new Queue[] { queue1, queue2 });

            this.DoTest(1, mockConfigurer.Object);
        }

        /// <summary>
        /// Tests the multiple queue names.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestMultipleQueueNames()
        {
            var mocker = new AutoMoqer();

            var mockConfigurer = mocker.GetMock<IContainerConfigurer>();
            mockConfigurer.Setup(c => c.Configure(It.IsAny<SimpleMessageListenerContainer>())).Callback<SimpleMessageListenerContainer>((container) => container.QueueNames = new string[] { queue1.Name, queue2.Name });

            this.DoTest(1, mockConfigurer.Object);
        }

        /// <summary>
        /// Tests the multiple queues with concurrent consumers.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestMultipleQueuesWithConcurrentConsumers()
        {
            var mocker = new AutoMoqer();

            var mockConfigurer = mocker.GetMock<IContainerConfigurer>();
            mockConfigurer.Setup(c => c.Configure(It.IsAny<SimpleMessageListenerContainer>())).Callback<SimpleMessageListenerContainer>((container) => container.Queues = new Queue[] { queue1, queue2 });

            this.DoTest(3, mockConfigurer.Object);
        }

        /// <summary>
        /// Tests the multiple queue names with concurrent consumers.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestMultipleQueueNamesWithConcurrentConsumers()
        {
            var mocker = new AutoMoqer();

            var mockConfigurer = mocker.GetMock<IContainerConfigurer>();
            mockConfigurer.Setup(c => c.Configure(It.IsAny<SimpleMessageListenerContainer>())).Callback<SimpleMessageListenerContainer>((container) => container.QueueNames = new string[] { queue1.Name, queue2.Name });

            this.DoTest(3, mockConfigurer.Object);
        }


        /// <summary>
        /// Does the test.
        /// </summary>
        /// <param name="concurrentConsumers">The concurrent consumers.</param>
        /// <param name="configurer">The configurer.</param>
        /// <remarks></remarks>
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
                template.ConvertAndSend(queue1.Name, i.ToString());
                template.ConvertAndSend(queue2.Name, i.ToString());
            }

            var container = new SimpleMessageListenerContainer(connectionFactory);
            var latch = new CountdownEvent(messageCount * 2);
            var listener = new MultiplePocoListener(latch);
            container.MessageListener = new MessageListenerAdapter(listener);
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.AUTO;
            container.IsChannelTransacted = true;
            container.ConcurrentConsumers = concurrentConsumers;
            configurer.Configure(container);
            container.AfterPropertiesSet();
            container.Start();
            try
            {
                var timeout = Math.Min((1 + messageCount) / concurrentConsumers, 30);
                var waited = latch.Wait(timeout * 1000);
                logger.Info("All messages recovered: " + waited);
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
    /// <remarks></remarks>
    public interface IContainerConfigurer
    {
        /// <summary>
        /// Configures the specified container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <remarks></remarks>
        void Configure(SimpleMessageListenerContainer container);
    }

    /// <summary>
    /// A multiple poco listener.
    /// </summary>
    /// <remarks></remarks>
    internal class MultiplePocoListener
    {
        private static ILog logger = LogManager.GetLogger(typeof(MultiplePocoListener));
        private AtomicInteger count = new AtomicInteger();

        private readonly CountdownEvent latch;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplePocoListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public MultiplePocoListener(CountdownEvent latch)
        {
            this.latch = latch;
        }

        /// <summary>
        /// Handles the message.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks></remarks>
        public void HandleMessage(int value)
        {
            logger.Debug(value + ":" + this.count.ReturnValueAndIncrement());
            this.latch.Signal();
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <remarks></remarks>
        public int Count
        {
            get { return this.count.Value; }
        }
    }
}
