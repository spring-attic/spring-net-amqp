using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Common.Logging;

using NUnit.Framework;

using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// SimpleMessageListenerContainerSunnyDayTest Tests
    /// </summary>
    public class SimpleMessageListenerContainerSunnyDayTest
    {
        private static ILog logger = LogManager.GetLogger(typeof(SimpleMessageListenerContainerIntegrationTests));

        private Queue queue = new Queue("test.queue");

        private RabbitTemplate template = new RabbitTemplate();

        [SetUp]
        public void Setup()
        {
            var brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue);

            brokerIsRunning.Apply();
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.ChannelCacheSize = 1;
            connectionFactory.Port = BrokerTestUtils.GetPort();
            this.template.ConnectionFactory = connectionFactory;
        }

        [Test]
        public void TestSingleSunnyDayScenario()
        {
            int concurrentConsumers;
            AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode;
            int messageCount;
            SimpleMessageListenerContainer container;
            int txSize;
            bool externalTransaction;
            bool transactional;


            messageCount = 1;
            concurrentConsumers = 1;
            acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
            transactional = false;
            txSize = 1;
            externalTransaction = false;

            var latch = new CountdownEvent(messageCount);

            container = CreateContainer(new MessageListenerAdapter(new SimplePocoListener(latch)), this.template, queue.Name, txSize, concurrentConsumers, transactional, acknowledgeMode, externalTransaction);
            for (var i = 0; i < messageCount; i++)
            {
                this.template.ConvertAndSend(this.queue.Name, i + "foo");
            }

            var waited = latch.Wait(new TimeSpan(0, 0, 0, Math.Max(2, messageCount / 40)));
            Assert.True(waited, "Timed out waiting for message");
            Assert.Null(this.template.ReceiveAndConvert(this.queue.Name));
        }

        [Test]
        public void TestSingleRainyDayScenario()
        {
            int concurrentConsumers;
            AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode;
            int messageCount;
            SimpleMessageListenerContainer container;
            int txSize;
            bool externalTransaction;
            bool transactional;


            messageCount = 1;
            concurrentConsumers = 1;
            acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
            transactional = false;
            txSize = 1;
            externalTransaction = false;

            var latch = new CountdownEvent(messageCount);

            container = CreateContainer(new MessageListenerAdapter(new SimplePocoListener(latch)), this.template, queue.Name, txSize, concurrentConsumers, transactional, acknowledgeMode, externalTransaction);
            for (var i = 0; i < messageCount; i++)
            {
                this.template.ConvertAndSend(this.queue.Name, i); //guaranteed to fail b/c there's no HandleMessage(int) overload on SimplePocoListener
            }

            var waited = latch.Wait(new TimeSpan(0, 0, 0, Math.Max(2, messageCount / 40)));
            Assert.False(waited, "Should have timed out waiting for message since no handler should match it!");
        }



        /// <summary>
        /// Creates the container.
        /// </summary>
        /// <param name="listener">The listener.</param>
        /// <returns>The container.</returns>
        /// <remarks></remarks>
        private static SimpleMessageListenerContainer CreateContainer(object listener, RabbitTemplate rabbitTemplate, string queueName, int txSize, int concurrentConsumers, bool transactional, AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode, bool externalTransaction)
        {
            var container = new SimpleMessageListenerContainer(rabbitTemplate.ConnectionFactory);
            container.MessageListener = listener;
            container.QueueNames = new string[] { queueName };
            container.TxSize = txSize;
            container.PrefetchCount = txSize;
            container.ConcurrentConsumers = concurrentConsumers;
            container.ChannelTransacted = transactional;
            container.AcknowledgeMode = acknowledgeMode;
            if (externalTransaction)
            {
                container.TransactionManager = new IntegrationTestTransactionManager();
            }

            container.AfterPropertiesSet();
            container.Start();
            return container;
        }
    }
}
