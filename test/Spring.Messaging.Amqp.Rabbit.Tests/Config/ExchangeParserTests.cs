// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeParserTests.cs" company="The original author or authors.">
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
using NUnit.Framework;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;
#endregion

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

        /// <summary>The setup.</summary>
        [TestFixtureSetUp]
        public void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/" + typeof(ExchangeParserTests).Name + "-context.xml";
            var resource = new AssemblyResource(resourceName);
            this.objectFactory = new XmlObjectFactory(resource);
        }

        /// <summary>The test direct exchange.</summary>
        [Test]
        public void testDirectExchange()
        {
            var exchange = this.objectFactory.GetObject<DirectExchange>("direct");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("direct", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        /// <summary>The test alias direct exchange.</summary>
        [Test]
        public void testAliasDirectExchange()
        {
            var exchange = this.objectFactory.GetObject<DirectExchange>("alias");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("direct-alias", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        /// <summary>The test topic exchange.</summary>
        [Test]
        public void testTopicExchange()
        {
            var exchange = this.objectFactory.GetObject<TopicExchange>("topic");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("topic", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        /// <summary>The test fanout exchange.</summary>
        [Test]
        public void testFanoutExchange()
        {
            var exchange = this.objectFactory.GetObject<FanoutExchange>("fanout");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("fanout", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        /// <summary>The test headers exchange.</summary>
        [Test]
        public void testHeadersExchange()
        {
            var exchange = this.objectFactory.GetObject<HeadersExchange>("headers");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("headers", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        /// <summary>The test direct exchange override.</summary>
        [Test]
        public void testDirectExchangeOverride()
        {
            var exchange = this.objectFactory.GetObject<DirectExchange>("direct-override");
            Assert.IsNotNull(exchange);
            Assert.AreEqual("direct-override", exchange.Name);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        /// <summary>The test direct exchange with arguments.</summary>
        [Test]
        public void testDirectExchangeWithArguments()
        {
            var exchange = this.objectFactory.GetObject<DirectExchange>("direct-arguments");

            Assert.IsNotNull(exchange);
            Assert.AreEqual("direct-arguments", exchange.Name);
            Assert.AreEqual("bar", exchange.Arguments["foo"]);
            Assert.AreEqual("baz", exchange.Arguments["bar"]);
        }
    }
}
