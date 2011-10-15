using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Common.Logging;
using Erlang.NET;
using NUnit.Framework;
using Spring.Messaging.Amqp.Rabbit.Test;

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    [TestFixture]
    [Category(TestCategory.LifecycleIntegration)]
    public class RabbitBrokerAdminLifecycleIntegrationTests
    {
        private static ILog logger = LogManager.GetLogger(typeof(RabbitBrokerAdminLifecycleIntegrationTests));

        private static readonly string NODE_NAME = "spring@" + Dns.GetHostName().ToUpper();

        public static EnvironmentAvailable environment = new EnvironmentAvailable("BROKER_INTEGRATION_TEST");

        [TestFixtureSetUp]
        public void SetUp()
        {
            environment.Apply();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            environment.Apply();
            if (environment.IsActive())
            {
                var brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(NODE_NAME);
                brokerAdmin.StopNode();
            }
        }

        /// <summary>
        /// Inits this instance.
        /// </summary>
        /// <remarks></remarks>
        [SetUp]
        public void Init()
        {
            var directory = new DirectoryInfo("target/rabbitmq");
            if (directory.Exists)
            {
                directory.Delete(true);
            }
        }

        [TearDown]
        public void End()
        {
            var brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(NODE_NAME);
            brokerAdmin.StopNode();
        }

        /// <summary>
        /// Tests the start node.
        /// </summary>
        [Test]
        public void TestStartNode()
        {
            // Set up broker admin for non-root user
            var brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(NODE_NAME);
            var status = brokerAdmin.GetStatus();
            try
            {
                // Stop it if it is already running
                if (status.IsReady)
                {
                    brokerAdmin.StopBrokerApplication();
                    Thread.Sleep(1000);
                }
            }
            catch (OtpException e)
            {
                // Not useful for test.
            }

            status = brokerAdmin.GetStatus();
            if (!status.IsRunning)
            {
                brokerAdmin.StartBrokerApplication();
            }

            status = brokerAdmin.GetStatus();

            try
            {
                Assert.False(status.Nodes == null || status.Nodes.Count < 1, "Broker node did not start. Check logs for hints.");
                Assert.True(status.IsRunning, "Broker node not running.  Check logs for hints.");
                Assert.True(status.IsReady, "Broker application not running.  Check logs for hints.");

                Thread.Sleep(1000);
                brokerAdmin.StopBrokerApplication();
                Thread.Sleep(1000);
            }
            finally
            {
                brokerAdmin.StopNode();
            }
        }

        /// <summary>
        /// Tests the stop and start broker.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestStopAndStartBroker()
        {
            // Set up broker admin for non-root user
            var brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(NODE_NAME);
            var status = brokerAdmin.GetStatus();

            status = brokerAdmin.GetStatus();
            if (!status.IsRunning)
            {
                brokerAdmin.StartBrokerApplication();
            }

            brokerAdmin.StopBrokerApplication();

            status = brokerAdmin.GetStatus();
            Assert.AreEqual(0, status.RunningNodes.Count);

            brokerAdmin.StartBrokerApplication();
            status = brokerAdmin.GetStatus();
            this.AssertBrokerAppRunning(status);
        }

        /// <summary>
        /// Repeats the lifecycle.
        /// </summary>
        [Test]
        public void RepeatLifecycle()
        {
            for (var i = 1; i <= 20; i++)
            {
                this.TestStopAndStartBroker();
                Thread.Sleep(200);
                // if (i % 5 == 0)
                // {
                    logger.Debug("i = " + i);
                // }
            }

            var brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(NODE_NAME);
            brokerAdmin.StopNode();
        }

        /// <summary>
        /// Asserts the broker app running. Asserts that the named-node is running.
        /// </summary>
        /// <param name="status">The status.</param>
        private void AssertBrokerAppRunning(RabbitStatus status)
        {
            Assert.AreEqual(1, status.RunningNodes.Count);
            Assert.True(status.RunningNodes[0].Name.Contains(NODE_NAME));
        }
    }
}
