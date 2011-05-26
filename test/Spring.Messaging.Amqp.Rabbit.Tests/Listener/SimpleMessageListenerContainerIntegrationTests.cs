using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Test;
using Spring.Threading.AtomicTypes;
using Spring.Transaction;
using Spring.Transaction.Support;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    public class SimpleMessageListenerContainerIntegrationTests
    {
        
        private static ILog logger = LogManager.GetLogger(typeof(SimpleMessageListenerContainerIntegrationTests));

        private Queue queue = new Queue("test.queue");

        private RabbitTemplate template = new RabbitTemplate();

        private readonly int concurrentConsumers;

        private readonly AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode;

        // @Rule
        // public Log4jLevelAdjuster logLevels = new Log4jLevelAdjuster(Level.ERROR, RabbitTemplate.class,
        // SimpleMessageListenerContainer.class, BlockingQueueConsumer.class);

        //@Rule
        public BrokerRunning brokerIsRunning;

        private readonly int messageCount;

        private SimpleMessageListenerContainer container;

        private readonly int txSize;

        private readonly bool externalTransaction;

        private readonly bool transactional;

        public SimpleMessageListenerContainerIntegrationTests()
        {
            brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue);
        }

        public SimpleMessageListenerContainerIntegrationTests(int messageCount, int concurrency, AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode, bool transactional, int txSize, bool externalTransaction)
        {
            this.messageCount = messageCount;
            this.concurrentConsumers = concurrency;
            this.acknowledgeMode = acknowledgeMode;
            this.transactional = transactional;
            this.txSize = txSize;
            this.externalTransaction = externalTransaction;
        }

        /*@Parameters
        public static List<Object[]> getParameters() {
            return Arrays.asList( //
                    params(0, 1, 1, AcknowledgeModeUtils.AcknowledgeMode.AUTO), //
                    params(1, 1, 1, AcknowledgeModeUtils.AcknowledgeMode.NONE), //
                    params(2, 4, 1, AcknowledgeModeUtils.AcknowledgeMode.AUTO), //
                    extern(3, 4, 1, AcknowledgeModeUtils.AcknowledgeMode.AUTO), //
                    params(4, 4, 1, AcknowledgeModeUtils.AcknowledgeMode.AUTO, false), //
                    params(5, 2, 2, AcknowledgeModeUtils.AcknowledgeMode.AUTO), //
                    params(6, 2, 2, AcknowledgeModeUtils.AcknowledgeMode.NONE), //
                    params(7, 20, 4, AcknowledgeModeUtils.AcknowledgeMode.AUTO), //
                    params(8, 20, 4, AcknowledgeModeUtils.AcknowledgeMode.NONE), //
                    params(9, 1000, 4, AcknowledgeModeUtils.AcknowledgeMode.AUTO), //
                    params(10, 1000, 4, AcknowledgeModeUtils.AcknowledgeMode.NONE), //
                    params(11, 1000, 4, AcknowledgeModeUtils.AcknowledgeMode.AUTO, 10) //
                    );
        }
        
        private static Object[] params(int i, int messageCount, int concurrency, AcknowledgeMode acknowledgeMode,
                boolean transactional, int txSize) {
            // "i" is just a counter to make it easier to identify the test in the log
            return new Object[] { messageCount, concurrency, acknowledgeMode, transactional, txSize, false };
        }

        private static Object[] params(int i, int messageCount, int concurrency, AcknowledgeMode acknowledgeMode, int txSize) {
            // For this test always us a transaction if it makes sense...
            return params(i, messageCount, concurrency, acknowledgeMode, acknowledgeMode.isTransactionAllowed(), txSize);
        }

        private static Object[] params(int i, int messageCount, int concurrency, AcknowledgeMode acknowledgeMode,
                boolean transactional) {
            return params(i, messageCount, concurrency, acknowledgeMode, transactional, 1);
        }

        private static Object[] params(int i, int messageCount, int concurrency, AcknowledgeMode acknowledgeMode) {
            return params(i, messageCount, concurrency, acknowledgeMode, 1);
        }

        private static Object[] extern(int i, int messageCount, int concurrency, AcknowledgeMode acknowledgeMode) {
            return new Object[] { messageCount, concurrency, acknowledgeMode, true, 1, true };
        }

        @Before
        public void declareQueue() {
            CachingConnectionFactory connectionFactory = new CachingConnectionFactory();
            connectionFactory.setChannelCacheSize(concurrentConsumers);
            connectionFactory.setPort(BrokerTestUtils.getPort());
            template.setConnectionFactory(connectionFactory);
        }

        @After
        public void clear() throws Exception {
            // Wait for broker communication to finish before trying to stop container
            Thread.sleep(300L);
            logger.debug("Shutting down at end of test");
            if (container != null) {
                container.shutdown();
            }
        }

        @Test
        public void testPojoListenerSunnyDay() throws Exception {
            CountDownLatch latch = new CountDownLatch(messageCount);
            doSunnyDayTest(latch, new MessageListenerAdapter(new PojoListener(latch)));
        }

        @Test
        public void testListenerSunnyDay() throws Exception {
            CountDownLatch latch = new CountDownLatch(messageCount);
            doSunnyDayTest(latch, new Listener(latch));
        }

        @Test
        public void testChannelAwareListenerSunnyDay() throws Exception {
            CountDownLatch latch = new CountDownLatch(messageCount);
            doSunnyDayTest(latch, new ChannelAwareListener(latch));
        }

        @Test
        public void testPojoListenerWithException() throws Exception {
            CountDownLatch latch = new CountDownLatch(messageCount);
            doListenerWithExceptionTest(latch, new MessageListenerAdapter(new PojoListener(latch, true)));
        }

        @Test
        public void testListenerWithException() throws Exception {
            CountDownLatch latch = new CountDownLatch(messageCount);
            doListenerWithExceptionTest(latch, new Listener(latch, true));
        }

        @Test
        public void testChannelAwareListenerWithException() throws Exception {
            CountDownLatch latch = new CountDownLatch(messageCount);
            doListenerWithExceptionTest(latch, new ChannelAwareListener(latch, true));
        }

        private void doSunnyDayTest(CountDownLatch latch, Object listener) throws Exception {
            container = createContainer(listener);
            for (int i = 0; i < messageCount; i++) {
                template.convertAndSend(queue.getName(), i + "foo");
            }
            boolean waited = latch.await(Math.max(2, messageCount / 50), TimeUnit.SECONDS);
            assertTrue("Timed out waiting for message", waited);
            assertNull(template.receiveAndConvert(queue.getName()));
        }

        private void doListenerWithExceptionTest(CountDownLatch latch, Object listener) throws Exception {
            container = createContainer(listener);
            if (acknowledgeMode.isTransactionAllowed()) {
                // Should only need one message if it is going to fail
                for (int i = 0; i < concurrentConsumers; i++) {
                    template.convertAndSend(queue.getName(), i + "foo");
                }
            } else {
                for (int i = 0; i < messageCount; i++) {
                    template.convertAndSend(queue.getName(), i + "foo");
                }
            }
            try {
                boolean waited = latch.await(5 + Math.max(1, messageCount / 20), TimeUnit.SECONDS);
                assertTrue("Timed out waiting for message", waited);
            } finally {
                // Wait for broker communication to finish before trying to stop
                // container
                Thread.sleep(300L);
                container.shutdown();
                Thread.sleep(300L);
            }
            if (acknowledgeMode.isTransactionAllowed()) {
                assertNotNull(template.receiveAndConvert(queue.getName()));
            } else {
                assertNull(template.receiveAndConvert(queue.getName()));
            }
        }

        private SimpleMessageListenerContainer createContainer(Object listener) {
            SimpleMessageListenerContainer container = new SimpleMessageListenerContainer(template.getConnectionFactory());
            container.setMessageListener(listener);
            container.setQueueNames(queue.getName());
            container.setTxSize(txSize);
            container.setPrefetchCount(txSize);
            container.setConcurrentConsumers(concurrentConsumers);
            container.setChannelTransacted(transactional);
            container.setAcknowledgeMode(acknowledgeMode);
            if (externalTransaction) {
                container.setTransactionManager(new IntegrationTestTransactionManager());
            }
            container.afterPropertiesSet();
            container.start();
            return container;
        }




    */

    }

    /// <summary>
    /// A Poco Listener.
    /// </summary>
    /// <remarks></remarks>
    public class SimplePocoListener
    {
        private AtomicInteger count = new AtomicInteger();
        private static ILog logger = LogManager.GetLogger(typeof(SimplePocoListener));
        private readonly CountdownEvent latch;

        private readonly bool fail;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimplePocoListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public SimplePocoListener(CountdownEvent latch) : this(latch, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimplePocoListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        /// <remarks></remarks>
        public SimplePocoListener(CountdownEvent latch, bool fail)
        {
            this.latch = latch;
            this.fail = fail;
        }

        /// <summary>
        /// Handles the message.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks></remarks>
        public void HandleMessage(String value)
        {
            try
            {
                int counter = this.count.ReturnValueAndIncrement();
                if (logger.IsDebugEnabled && counter % 500 == 0)
                {
                    logger.Debug("Handling: " + value + ":" + counter + " - " + this.latch);
                }

                if (this.fail)
                {
                    throw new Exception("Planned failure");
                }
            }
            finally
            {
                this.latch.Signal();
            }
        }
    }

    /// <summary>
    /// A listener.
    /// </summary>
    /// <remarks></remarks>
    public class Listener : IMessageListener
    {
        private AtomicInteger count = new AtomicInteger();
        private static ILog logger = LogManager.GetLogger(typeof(Listener));
        private readonly CountdownEvent latch;

        private readonly bool fail;

        /// <summary>
        /// Initializes a new instance of the <see cref="Listener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public Listener(CountdownEvent latch) : this(latch, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Listener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        /// <remarks></remarks>
        public Listener(CountdownEvent latch, bool fail)
        {
            this.latch = latch;
            this.fail = fail;
        }

        /// <summary>
        /// Called when a Message is received.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <remarks></remarks>
        public void OnMessage(Message message)
        {
            var value = Encoding.UTF8.GetString(message.Body);
            try
            {
                int counter = this.count.ReturnValueAndIncrement();
                if (logger.IsDebugEnabled && counter % 500 == 0)
                {
                    logger.Debug(value + counter);
                }

                if (this.fail)
                {
                    throw new Exception("Planned failure");
                }
            }
            finally
            {
                this.latch.Signal();
            }
        }
    }

    /// <summary>
    /// A channel aware listener.
    /// </summary>
    /// <remarks></remarks>
    public class ChannelAwareListener : IChannelAwareMessageListener
    {
        private AtomicInteger count = new AtomicInteger();
        private static ILog logger = LogManager.GetLogger(typeof(ChannelAwareListener));
        private readonly CountdownEvent latch;

        private readonly bool fail;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelAwareListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <remarks></remarks>
        public ChannelAwareListener(CountdownEvent latch) : this(latch, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelAwareListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <param name="fail">if set to <c>true</c> [fail].</param>
        /// <remarks></remarks>
        public ChannelAwareListener(CountdownEvent latch, bool fail)
        {
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
                int counter = this.count.ReturnValueAndIncrement();
                if (logger.IsDebugEnabled && counter % 500 == 0)
                {
                    logger.Debug(value + counter);
                }
                if (this.fail)
                {
                    throw new Exception("Planned failure");
                }
            }
            finally
            {
                this.latch.Signal();
            }
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks></remarks>
    internal class IntegrationTestTransactionManager : AbstractPlatformTransactionManager
    {
        protected override void DoBegin(object transaction, ITransactionDefinition definition)
        {
        }

        protected override void DoCommit(DefaultTransactionStatus status)
        {
        }

        protected override object DoGetTransaction()
        {
            return new object();
        }

        protected override void DoRollback(DefaultTransactionStatus status)
        {
        }
    }
}
