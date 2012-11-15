// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StopStartIntegrationTests.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using Spring.Objects.Factory.Xml;
using Spring.Testing.NUnit;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// Stop Start Integration Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
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

        /// <summary>Initializes a new instance of the <see cref="StopStartIntegrationTests"/> class.</summary>
        public StopStartIntegrationTests() { this.PopulateProtectedVariables = true; }

        /// <summary>The set up.</summary>
        [TestFixtureSetUp]
        public void SetUp()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            this.brokerIsRunning.Apply();
        }

        /// <summary>The tear down.</summary>
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

        /// <summary>The test.</summary>
        [Test]
        public void Test()
        {
            for (var i = 0; i < COUNT; i++)
            {
                this.amqpTemplate.ConvertAndSend("foo" + i);
            }

            var t = DateTime.UtcNow.ToMilliseconds();
            this.container.Start();
            int n;
            var lastN = 0;
            while ((n = this.deliveries.Value) < COUNT)
            {
                Thread.Sleep(2000);
                this.container.Stop();
                Logger.Debug(m => m("######### Current Deliveries Value: {0} #########", this.deliveries.Value));
                this.container.Start();
                if (DateTime.UtcNow.ToMilliseconds() - t > 240000 && lastN == n)
                {
                    Assert.Fail("Only received " + this.deliveries.Value);
                }

                lastN = n;
            }

            Logger.Debug(m => m("######### --------------------------- #########", this.deliveries.Value));
            Logger.Debug(m => m("######### Final Deliveries Value: {0} #########", this.deliveries.Value));
            Logger.Debug(m => m("######### --------------------------- #########", this.deliveries.Value));
        }

        /// <summary>Gets the config locations.</summary>
        protected override string[] ConfigLocations
        {
            get
            {
                var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Listener/" + typeof(StopStartIntegrationTests).Name + "-context.xml";
                return new[] { resourceName };
            }
        }
    }

    /// <summary>The stop start integration test listner.</summary>
    public class StopStartIntegrationTestListner : IMessageListener
    {
        private readonly AtomicInteger deliveries;

        /// <summary>Initializes a new instance of the <see cref="StopStartIntegrationTestListner"/> class.</summary>
        /// <param name="deliveries">The deliveries.</param>
        public StopStartIntegrationTestListner(AtomicInteger deliveries) { this.deliveries = deliveries; }

        /// <summary>The on message.</summary>
        /// <param name="message">The message.</param>
        public void OnMessage(Message message) { this.deliveries.IncrementValueAndReturn(); }
    }
}
