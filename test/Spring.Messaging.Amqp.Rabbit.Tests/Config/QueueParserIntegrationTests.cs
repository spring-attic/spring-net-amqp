
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using NUnit.Framework;

using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// Queue parser integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class QueueParserIntegrationTests : AbstractRabbitIntegrationTest
    {
        

        public override void BeforeFixtureSetUp()
        {
        }

        public override void BeforeFixtureTearDown()
        {
        }

        public override void AfterFixtureSetUp()
        {
        }

        public override void AfterFixtureTearDown()
        {
        }

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
            objectFactory = new XmlObjectFactory(resource);
        }

        [Test]
        public void testArgumentsQueue()
        {
            var queue = objectFactory.GetObject<Queue>("arguments");
            Assert.IsNotNull(queue);

            var template = new RabbitTemplate(new CachingConnectionFactory(BrokerTestUtils.GetPort()));
            var rabbitAdmin = new RabbitAdmin(template.ConnectionFactory);
            rabbitAdmin.DeleteQueue(queue.Name);
            rabbitAdmin.DeclareQueue(queue);

            Assert.AreEqual(100L, queue.Arguments["x-message-ttl"]);
            template.ConvertAndSend(queue.Name, "message");

            Thread.Sleep(200);
            var result = (String)template.ReceiveAndConvert(queue.Name);
            Assert.AreEqual(null, result);

        }
    }
}
