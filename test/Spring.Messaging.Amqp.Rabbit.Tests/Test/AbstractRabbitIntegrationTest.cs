using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Common.Logging;

using NUnit.Framework;
using Spring.Messaging.Amqp.Rabbit.Admin;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Test
{
    /// <summary>
    /// A base class for integration tests, to ensure that the broker is started, and that it is shut down after the test is done.
    /// </summary>
    public abstract class AbstractRabbitIntegrationTest
    {
        public static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        public static EnvironmentAvailable environment = new EnvironmentAvailable("BROKER_INTEGRATION_TEST");

        protected RabbitBrokerAdmin brokerAdmin;

        /// <summary>
        /// Determines if the broker is running.
        /// </summary>
        protected BrokerRunning brokerIsRunning = BrokerRunning.IsRunning();

        /// <summary>
        /// Ensures that RabbitMQ is running.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            this.BeforeFixtureSetUp();
            
            // Eventually add some kind of logic here to start up the broker if it is not running.
            this.AfterFixtureSetUp();

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

        /// <summary>
        /// Fixtures the tear down.
        /// </summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            this.BeforeFixtureTearDown();
            // var brokerAdmin = new RabbitBrokerAdmin();
            // brokerAdmin.StopBrokerApplication();
            // brokerAdmin.StopNode();
            this.AfterFixtureTearDown();
        }

        /// <summary>
        /// Code to execute before fixture setup.
        /// </summary>
        public abstract void BeforeFixtureSetUp();

        /// <summary>
        /// Code to execute before fixture teardown.
        /// </summary>
        public abstract void BeforeFixtureTearDown();

        /// <summary>
        /// Code to execute after fixture setup.
        /// </summary>
        public abstract void AfterFixtureSetUp();

        /// <summary>
        /// Code to execute after fixture teardown.
        /// </summary>
        public abstract void AfterFixtureTearDown();
    }
}
