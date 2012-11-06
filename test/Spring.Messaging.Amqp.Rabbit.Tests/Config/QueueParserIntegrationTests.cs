// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueueParserIntegrationTests.cs" company="The original author or authors.">
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
using NUnit.Framework;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// Queue parser integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class QueueParserIntegrationTests : AbstractRabbitIntegrationTest
    {
        /// <summary>The before fixture set up.</summary>
        public override void BeforeFixtureSetUp() { }

        /// <summary>The before fixture tear down.</summary>
        public override void BeforeFixtureTearDown() { }

        /// <summary>The after fixture set up.</summary>
        public override void AfterFixtureSetUp() { }

        /// <summary>The after fixture tear down.</summary>
        public override void AfterFixtureTearDown() { }

        /// <summary>The set up.</summary>
        [SetUp]
        public void SetUp()
        {
            this.brokerIsRunning = BrokerRunning.IsRunning();
            this.brokerIsRunning.Apply();
        }

        protected XmlObjectFactory objectFactory;

        /// <summary>
        /// Setups this instance.
        /// </summary>
        [TestFixtureSetUp]
        public virtual void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName =
                @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                + typeof(QueueParserIntegrationTests).Name + "-context.xml";
            var resource = new AssemblyResource(resourceName);
            this.objectFactory = new XmlObjectFactory(resource);
        }

        /// <summary>The test arguments queue.</summary>
        [Test]
        public void TestArgumentsQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("arguments");
            Assert.IsNotNull(queue);

            var template = new RabbitTemplate(new CachingConnectionFactory(BrokerTestUtils.GetPort()));
            var rabbitAdmin = new RabbitAdmin(template.ConnectionFactory);
            rabbitAdmin.DeleteQueue(queue.Name);
            rabbitAdmin.DeclareQueue(queue);

            Assert.AreEqual(100L, queue.Arguments["x-message-ttl"]);
            template.ConvertAndSend(queue.Name, "message");

            Thread.Sleep(200);
            var result = (string)template.ReceiveAndConvert(queue.Name);
            Assert.AreEqual(null, result);
        }
    }
}
