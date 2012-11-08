// -----------------------------------------------------------------------
// <copyright file="StopStartIntegrationTests.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using Common.Logging;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using Spring.Objects.Factory.Xml;
using Spring.Testing.NUnit;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class StopStartIntegrationTests : AbstractDependencyInjectionSpringContextTests
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        // @Rule
        public BrokerRunning brokerIsRunning = BrokerRunning.IsRunning();

        protected AtomicInteger deliveries;

        private static int COUNT = 10000;

        // @Autowired
        // private ApplicationContext ctx;

        // @Autowired
        protected RabbitTemplate amqpTemplate;

        // @Autowired
        protected SimpleMessageListenerContainer container;

        protected RabbitAdmin rabbitAdmin;

        public StopStartIntegrationTests()
        {
            this.PopulateProtectedVariables = true;
        }

        [TestFixtureSetUp]
        public void SetUp()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            this.brokerIsRunning.Apply();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                var queue = this.applicationContext.GetObject<Queue>("stop.start.queue");
                this.rabbitAdmin.DeleteQueue(queue.Name);
                var exchange = this.applicationContext.GetObject<IExchange>("stop.start.exchange");
                this.rabbitAdmin.DeleteExchange(exchange.Name);
            }
            catch (Exception ex)
            {
                Logger.Error(m => m("Error in TearDown"), ex);
                throw;
            }
        }

        [Test]
        public void Test()
        {
            for (var i = 0; i < COUNT; i++)
            {
                this.amqpTemplate.ConvertAndSend("foo" + i);
            }

            var t = DateTime.UtcNow.ToMilliseconds();
            container.Start();
            int n;
            var lastN = 0;
            while ((n = deliveries.Value) < COUNT)
            {
                Thread.Sleep(2000);
                container.Stop();
                Logger.Debug(m => m("######### Current Deliveries Value: {0} #########", deliveries.Value));
                container.Start();
                if (DateTime.UtcNow.ToMilliseconds() - t > 240000 && lastN == n)
                {
                    Assert.Fail("Only received " + deliveries.Value);
                }

                lastN = n;
            }

            Logger.Debug(m => m("######### --------------------------- #########", deliveries.Value));
            Logger.Debug(m => m("######### Final Deliveries Value: {0} #########", deliveries.Value));
            Logger.Debug(m => m("######### --------------------------- #########", deliveries.Value));
        }

        protected override string[] ConfigLocations
        {
            get
            {
                var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Listener/" + typeof(StopStartIntegrationTests).Name + "-context.xml";
                return new[] { resourceName };
            }
        }
    }

    public class StopStartIntegrationTestListner : IMessageListener
    {
        private readonly AtomicInteger deliveries;

        public StopStartIntegrationTestListner(AtomicInteger deliveries) { this.deliveries = deliveries; }

        public void OnMessage(Message message)
        {
            this.deliveries.IncrementValueAndReturn();
        }
    }
}
