// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FederatedExchangeParserIntegrationTests.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
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
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// Federated Exchange Parser Integration Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class FederatedExchangeParserIntegrationTests : AbstractDependencyInjectionSpringContextTests
    {
        public static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private static readonly EnvironmentAvailable Environment = new EnvironmentAvailable("BROKER_INTEGRATION_TEST");

        // @Rule
        private readonly BrokerFederated brokerFederated = BrokerFederated.IsRunning();

        // @Autowired
        protected IConnectionFactory connectionFactory;

        // @Autowired
        protected IExchange fanoutTest;

        // @Autowired
        // @Qualifier("bucket")
        protected Queue bucket;

        // @Autowired
        protected RabbitAdmin admin;

        private RabbitBrokerAdmin brokerAdmin;

        /// <summary>Initializes a new instance of the <see cref="FederatedExchangeParserIntegrationTests"/> class.</summary>
        public FederatedExchangeParserIntegrationTests() { this.PopulateProtectedVariables = true; }

        /// <summary>Gets the config locations.</summary>
        protected override string[] ConfigLocations
        {
            get
            {
                var resourceName =
                    @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                    + typeof(FederatedExchangeParserIntegrationTests).Name + "-context.xml";
                return new[] { resourceName };
            }
        }

        /// <summary>
        /// Ensures that RabbitMQ is running.
        /// </summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            try
            {
                if (Environment.IsActive())
                {
                    // Set up broker admin for non-root user
                    this.brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(); // "rabbit@LOCALHOST", 5672);
                    this.brokerAdmin.StartNode();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred during SetUp", ex);
                Assert.Fail("An error occurred during SetUp.");
            }

            if (!this.brokerFederated.Apply())
            {
                Assert.Ignore("Rabbit broker is not running. Ignoring integration test fixture.");
            }
        }

        /// <summary>The test bindings declared.</summary>
        [Test]
        public void TestBindingsDeclared()
        {
            var template = new RabbitTemplate(this.connectionFactory);
            template.ConvertAndSend(this.fanoutTest.Name, string.Empty, "message");
            Thread.Sleep(200);

            // The queue is anonymous so it will be deleted at the end of the test, but it should get the message as long as
            // we use the same connection
            var result = (string)template.ReceiveAndConvert(this.bucket.Name);
            Assert.AreEqual("message", result);
            this.admin.DeleteExchange("fedDirectTest");
            this.admin.DeleteExchange("fedTopicTest");
            this.admin.DeleteExchange("fedFanoutTest");
            this.admin.DeleteExchange("fedHeadersTest");
        }
    }
}
