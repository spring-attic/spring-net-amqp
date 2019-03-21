// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitNamespaceHandlerTests.cs" company="The original author or authors.">
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
using Common.Logging;
using NUnit.Framework;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// RabbitNamespaceHandler Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class RabbitNamespaceHandlerTests
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private XmlObjectFactory objectFactory;

        /// <summary>
        /// Setups this instance.
        /// </summary>
        [TestFixtureSetUp]
        public void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/" + typeof(RabbitNamespaceHandlerTests).Name + "-context.xml";
            var resource = new AssemblyResource(resourceName);
            this.objectFactory = new XmlObjectFactory(resource);
        }

        /// <summary>The test queue.</summary>
        [Test]
        public void TestQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("foo");
            Assert.IsNotNull(queue);
            Assert.AreEqual("foo", queue.Name);
        }

        /// <summary>The test alias queue.</summary>
        [Test]
        public void TestAliasQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("spam");
            Assert.IsNotNull(queue);
            Assert.AreNotSame("spam", queue.Name);
            Assert.AreEqual("baz", queue.Name);
        }

        /// <summary>The test anonymous queue.</summary>
        [Test]
        public void TestAnonymousQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("bucket");
            Assert.IsNotNull(queue);
            Assert.AreNotSame("bucket", queue.Name);
            Assert.True(queue is AnonymousQueue);
        }

        /// <summary>The test exchanges.</summary>
        [Test]
        public void TestExchanges()
        {
            Assert.IsNotNull(this.objectFactory.GetObject<DirectExchange>("direct-test"));
            Assert.IsNotNull(this.objectFactory.GetObject<TopicExchange>("topic-test"));
            Assert.IsNotNull(this.objectFactory.GetObject<FanoutExchange>("fanout-test"));
            Assert.IsNotNull(this.objectFactory.GetObject<HeadersExchange>("headers-test"));
        }

        /// <summary>The test bindings.</summary>
        [Test]
        public void TestBindings()
        {
            var bindings = this.objectFactory.GetObjects<Binding>();

            // 4 for each exchange type
            Assert.AreEqual(17, bindings.Count);
        }

        /// <summary>The test admin.</summary>
        [Test]
        public void TestAdmin() { Assert.IsNotNull(this.objectFactory.GetObject<RabbitAdmin>("admin-test")); }
    }
}
