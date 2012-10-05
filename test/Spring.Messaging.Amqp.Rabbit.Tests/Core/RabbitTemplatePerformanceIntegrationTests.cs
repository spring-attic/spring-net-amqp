// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTemplatePerformanceIntegrationTests.cs" company="The original author or authors.">
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
using System.Threading;
using NUnit.Framework;
using Spring.Messaging.Amqp.Rabbit.Admin;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

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
        private readonly RabbitTemplate template = new RabbitTemplate();

        /// <summary>The fixture set up.</summary>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            var brokerAdmin = new RabbitBrokerAdmin();
            brokerAdmin.StartupTimeout = 10000;
            brokerAdmin.StartBrokerApplication();
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(ROUTE);
        }

        /// <summary>The fixture tear down.</summary>
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
