
using System.Threading;
using NUnit.Framework;
using Spring.Messaging.Amqp.Rabbit.Admin;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core
{
    /// <summary>
    /// Rabbit template performance integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class RabbitTemplatePerformanceIntegrationTests
    {
        /// <summary>
        /// The route.
        /// </summary>
        private const string ROUTE = "test.queue";

        /// <summary>
        /// The template.
        /// </summary>
        private RabbitTemplate template = new RabbitTemplate();

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            var brokerAdmin = new RabbitBrokerAdmin();
            brokerAdmin.StartupTimeout = 10000;
            brokerAdmin.StartBrokerApplication();
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(ROUTE);
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            var brokerAdmin = new RabbitBrokerAdmin();
            brokerAdmin.StopBrokerApplication();
            brokerAdmin.StopNode();
        }

        /*@Rule
        //public RepeatProcessor repeat = new RepeatProcessor(4);

        //@Rule
        // After the repeat processor, so it only runs once
        //public Log4jLevelAdjuster logLevels = new Log4jLevelAdjuster(Level.ERROR, RabbitTemplate.class);

        //@Rule
        // After the repeat processor, so it only runs once*/

        /// <summary>
        /// The broker is running.
        /// </summary>
        public BrokerRunning brokerIsRunning;

        /// <summary>
        /// The connection factory.
        /// </summary>
        private CachingConnectionFactory connectionFactory;

        /// <summary>
        /// Declares the queue.
        /// </summary>
        /// <remarks></remarks>
        [SetUp]
        public void DeclareQueue()
        {
            /*if (repeat.isInitialized()) {
                // Important to prevent concurrent re-initialization
                return;
            }*/
            this.connectionFactory = new CachingConnectionFactory();
            this.connectionFactory.ChannelCacheSize = 4;
            this.connectionFactory.Port = BrokerTestUtils.GetPort();
            this.template.ConnectionFactory = this.connectionFactory;
        }

        /// <summary>
        /// Cleans up.
        /// </summary>
        /// <remarks></remarks>
        [TearDown]
        public void CleanUp()
        {
            /*if (repeat.isInitialized()) {
            //  return;
            //}*/

            if (this.connectionFactory != null)
            {
                this.connectionFactory.Dispose();
            }
        }

        /// <summary>
        /// Tests the send and receive.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        [Repeat(200)]
        public void TestSendAndReceive()
        {
            this.template.ConvertAndSend(ROUTE, "message");
            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            var count = 5;
            while (result == null && count-- > 0)
            {
                /*
                 * Retry for the purpose of non-transacted case because channel operations are async in that case
                 */
                Thread.Sleep(10);
                result = (string)this.template.ReceiveAndConvert(ROUTE);
            }

            Assert.AreEqual("message", result);
        }

        /// <summary>
        /// Tests the send and receive transacted.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        [Repeat(2000)]
        public void TestSendAndReceiveTransacted()
        {
            this.template.ChannelTransacted = true;
            this.template.ConvertAndSend(ROUTE, "message");
            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
        }
    }
}