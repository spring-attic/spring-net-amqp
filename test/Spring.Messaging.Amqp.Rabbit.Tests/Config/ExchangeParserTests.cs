
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// ExchangeParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class ExchangeParserTests
    {
        private XmlObjectFactory objectFactory;

        [TestFixtureSetUp]
        public void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/" + typeof(ExchangeParserTests).Name + "-context.xml";
            var resource = new AssemblyResource(resourceName);
            objectFactory = new XmlObjectFactory(resource);
        }

        [Test]
        public void testDirectExchange() 
        {
		    var exchange = objectFactory.GetObject<DirectExchange>("direct");
		    Assert.IsNotNull(exchange);
		    Assert.AreEqual("direct", exchange.Name);
		    Assert.True(exchange.Durable);
		    Assert.False(exchange.AutoDelete);
	    }

        [Test]
        public void testAliasDirectExchange()
        {
            var exchange = objectFactory.GetObject<DirectExchange>("alias");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("direct-alias", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        [Test]
        public void testTopicExchange()
        {
            var exchange = objectFactory.GetObject<TopicExchange>("topic");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("topic", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        [Test]
        public void testFanoutExchange()
        {
            var exchange = objectFactory.GetObject<FanoutExchange>("fanout");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("fanout", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        [Test]
        public void testHeadersExchange()
        {
            var exchange = objectFactory.GetObject<HeadersExchange>("headers");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("headers", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        [Test]
        public void testDirectExchangeOverride()
        {
            var exchange = objectFactory.GetObject<DirectExchange>("direct-override");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("direct-override", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        [Test]
        public void testDirectExchangeWithArguments()
        {
            var exchange = objectFactory.GetObject<DirectExchange>("direct-arguments");
            
            Assert.IsNotNull(exchange);
            Assert.AreEqual("direct-arguments", exchange.Name);
            Assert.AreEqual("bar", exchange.Arguments["foo"]);
        }
    }
}
