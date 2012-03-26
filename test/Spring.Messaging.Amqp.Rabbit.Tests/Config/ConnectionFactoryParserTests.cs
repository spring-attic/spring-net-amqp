
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Spring.Core.IO;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// ConnectionFactoryParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class ConnectionFactoryParserTests
    {
        private XmlObjectFactory objectFactory;

        [TestFixtureSetUp]
        public void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/" + typeof(ConnectionFactoryParserTests).Name + "-context.xml";
            var resource = new AssemblyResource(resourceName);
            objectFactory = new XmlObjectFactory(resource);
        }

        [Test]
        public void testKitchenSink() 
        {
            var connectionFactory = objectFactory.GetObject<CachingConnectionFactory>("kitchenSink");
		    Assert.IsNotNull(connectionFactory);
		    Assert.AreEqual(10, connectionFactory.ChannelCacheSize);
	    }	

        [Test]
        public void testNative() 
        {
		    var connectionFactory = objectFactory.GetObject<CachingConnectionFactory>("native");
		    Assert.IsNotNull(connectionFactory);
		    Assert.AreEqual(10, connectionFactory.ChannelCacheSize);
	    }
    }
}
