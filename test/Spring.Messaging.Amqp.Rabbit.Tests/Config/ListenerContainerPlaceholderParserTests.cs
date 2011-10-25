
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

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// ListenerContainerPlaceholderParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    [Ignore("Need to fix...")]
    public class ListenerContainerPlaceholderParserTests
    {
        protected IObjectFactory beanFactory;

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
            var resource = new AssemblyResource(resourceName);
            beanFactory = new XmlApplicationContext(resourceName);
        }

        [TestFixtureTearDown]
        public void closeBeanFactory()
        {
            if (beanFactory != null)
            {
                ((IConfigurableApplicationContext)beanFactory).Dispose(); ;
            }
        }

        [Test]
        public void testParseWithQueueNames()
        {
		    var container = beanFactory.GetObject<SimpleMessageListenerContainer>("container1");
		    Assert.AreEqual(AcknowledgeModeUtils.AcknowledgeMode.MANUAL, container.AcknowledgeMode);
		    Assert.AreEqual(beanFactory.GetObject<IConnectionFactory>("connectionFactory"), container.ConnectionFactory);
		    Assert.AreEqual(typeof(MessageListenerAdapter), container.MessageListener.GetType());
		    var listenerAccessor = container.MessageListener;
            Assert.AreEqual(beanFactory.GetObject<TestObject>("testObject"), ((MessageListenerAdapter)listenerAccessor).HandlerObject);
            
            Assert.AreEqual("Handle", ((MessageListenerAdapter)listenerAccessor).DefaultListenerMethod); 

		    var queue = beanFactory.GetObject<Queue>("bar");
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
