// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeParserIntegrationTests.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections;
using System.Threading;
using Common.Logging;
using NUnit.Framework;
using Spring.Context;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Admin;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;
using Spring.Testing.NUnit;
using Queue = Spring.Messaging.Amqp.Core.Queue;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// ExchangeParserIntegration Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class ExchangeParserIntegrationTests : AbstractDependencyInjectionSpringContextTests
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        public static EnvironmentAvailable environment = new EnvironmentAvailable("BROKER_INTEGRATION_TEST");

        protected RabbitBrokerAdmin brokerAdmin;

        protected IConnectionFactory connectionFactory;

        protected IExchange fanoutTest;

        protected Queue bucket;

        /// <summary>Initializes a new instance of the <see cref="ExchangeParserIntegrationTests"/> class.</summary>
        public ExchangeParserIntegrationTests() { this.PopulateProtectedVariables = true; }

        /// <summary>
        /// Determines if the broker is running.
        /// </summary>
        protected BrokerRunning brokerIsRunning = BrokerRunning.IsRunning();

        /// <summary>Gets the config locations.</summary>
        protected override string[] ConfigLocations
        {
            get
            {
                var resourceName =
                    @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                    + typeof(ExchangeParserIntegrationTests).Name + "-context.xml";
                return new[] { resourceName };
            }
        }

        /// <summary>The fixture set up.</summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));

            try
            {
                if (environment.IsActive())
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

            if (!this.brokerIsRunning.Apply())
            {
                Assert.Ignore("Rabbit broker is not running. Ignoring integration test fixture.");
            }
        }

        /// <summary>The fixture tear down.</summary>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            var admin = this.applicationContext.GetObject<RabbitAdmin>();
            var bindings = this.applicationContext.GetObjects<Binding>();
            var exchanges = this.applicationContext.GetObjects<IExchange>();
            var queues = this.applicationContext.GetObjects<Queue>();

            foreach (DictionaryEntry item in bindings)
            {
                try
                {
                    admin.RemoveBinding(item.Value as Binding);
                }
                catch (Exception ex)
                {
                    Logger.Error(m => m("Could not remove object."), ex);
                    throw;
                }
            }

            foreach (DictionaryEntry item in queues)
            {
                try
                {
                    admin.DeleteQueue(((Queue)item.Value).Name);
                }
                catch (Exception ex)
                {
                    Logger.Error(m => m("Could not remove object."), ex);
                    throw;
                }
            }

            foreach (DictionaryEntry item in exchanges)
            {
                try
                {
                    admin.DeleteExchange(((IExchange)item.Value).Name);
                }
                catch (Exception ex)
                {
                    Logger.Error(m => m("Could not remove object."), ex);
                    throw;
                }
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
        }
    }
}
