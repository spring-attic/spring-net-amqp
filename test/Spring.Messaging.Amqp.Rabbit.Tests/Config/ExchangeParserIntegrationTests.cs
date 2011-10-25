
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Common.Logging;

using NUnit.Framework;

using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Admin;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;
using Spring.Testing.NUnit;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// ExchangeParserIntegration Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    [Ignore("Need to fix...")]
    public class ExchangeParserIntegrationTests : AbstractDependencyInjectionSpringContextTests
    {
        public static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        public static EnvironmentAvailable environment = new EnvironmentAvailable("BROKER_INTEGRATION_TEST");

        protected RabbitBrokerAdmin brokerAdmin;

        protected IConnectionFactory connectionFactory;

        protected IExchange fanoutTest;

        protected Queue queue;

        public ExchangeParserIntegrationTests()
        {
            PopulateProtectedVariables = true;
        }

        /// <summary>
        /// Determines if the broker is running.
        /// </summary>
        protected BrokerRunning brokerIsRunning = BrokerRunning.IsRunning();

        protected override string[] ConfigLocations
        {
            get
            {
                var resourceName =
                    @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                    + typeof(ExchangeParserIntegrationTests).Name + "-context.xml";
                return new string[] { resourceName };
            }
        }

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));

            try
            {
                if (environment.IsActive())
                {
                    // Set up broker admin for non-root user
                    this.brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(); //"rabbit@LOCALHOST", 5672);
                    this.brokerAdmin.StartNode();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred during SetUp", ex);
                Assert.Fail("An error occurred during SetUp.");
            }
            if (!this.brokerIsRunning.Apply())
            {
                Assert.Ignore("Rabbit broker is not running. Ignoring integration test fixture.");
            }
        }

        [Test]
        public void testBindingsDeclared()
        {
            var template = new RabbitTemplate(connectionFactory);
            template.ConvertAndSend(fanoutTest.Name, string.Empty, "message");
            Thread.Sleep(200);
            // The queue is anonymous so it will be deleted at the end of the test, but it should get the message as long as
            // we use the same connection
            var result = (String)template.ReceiveAndConvert(queue.Name);
            Assert.AreEqual("message", result);
        }
    }
}
