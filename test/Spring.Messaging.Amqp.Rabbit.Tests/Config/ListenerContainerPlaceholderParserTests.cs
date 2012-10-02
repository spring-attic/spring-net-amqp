
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Spring.Context;
using Spring.Context.Support;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Xml;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// ListenerContainerPlaceholderParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class ListenerContainerPlaceholderParserTests
    {
        protected IObjectFactory objectFactory;

        /// <summary>
        /// Setups this instance.
        /// </summary>
        [TestFixtureSetUp]
        public virtual void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName =
                @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                + typeof(ListenerContainerPlaceholderParserTests).Name + "-context.xml";
            //var resource = new AssemblyResource(resourceName);
            objectFactory = new XmlApplicationContext(resourceName);
        }

        [TestFixtureTearDown]
        public void closeObjectFactory()
        {
            if (objectFactory != null)
            {
                objectFactory.Dispose();
            }
        }

        [Test]
        //TODO: this thing 
        public void testParseWithQueueNames()
        {
		    var container = objectFactory.GetObject<SimpleMessageListenerContainer>("container1");
		    Assert.AreEqual(AcknowledgeModeUtils.AcknowledgeMode.Manual, container.AcknowledgeMode);
		    Assert.AreEqual(objectFactory.GetObject<IConnectionFactory>("connectionFactory"), container.ConnectionFactory);
		    Assert.AreEqual(typeof(MessageListenerAdapter), container.MessageListener.GetType());
            Assert.AreEqual(5, ReflectionUtils.GetInstanceFieldValue(container, "concurrentConsumers"), "concurrency placeholder not processed correctly");
            Assert.AreEqual(1,ReflectionUtils.GetInstanceFieldValue(container, "txSize"),"transaction-size placeholder not processed correctly");
		    Assert.IsFalse(container.AutoStartup,"auto-startup placeholder not processed correctly");

            var listenerAccessor = container.MessageListener;
            Assert.AreEqual(objectFactory.GetObject<TestObject>("testObject"), ((MessageListenerAdapter)listenerAccessor).HandlerObject);
            
            Assert.AreEqual("Handle", ((MessageListenerAdapter)listenerAccessor).DefaultListenerMethod); 

		    var queue = objectFactory.GetObject<Queue>("bar");
            var queueNamesForVerification = "[";
            foreach (var queueName in container.QueueNames)
            {
                queueNamesForVerification += queueNamesForVerification == "[" ? queueName : ", " + queueName;
            }
            queueNamesForVerification += "]";
            Assert.AreEqual("[foo, " + queue.Name + "]", queueNamesForVerification);
	    }
    }
}
