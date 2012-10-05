// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueueParserTests.cs" company="The original author or authors.">
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
using Spring.Objects.Factory;
using Spring.Objects.Factory.Xml;
    // using Spring.Context.Config;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// QueueParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class QueueParserTests
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
                + typeof(QueueParserTests).Name + "-context.xml";
            var resource = new AssemblyResource(resourceName);
            this.objectFactory = new XmlObjectFactory(resource);
        }

        /// <summary>The test queue.</summary>
        [Test]
        public void testQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("foo");
            Assert.IsNotNull(queue);
            Assert.AreEqual("foo", queue.Name);
            Assert.True(queue.Durable);
            Assert.False(queue.AutoDelete);
            Assert.False(queue.Exclusive);
        }

        /// <summary>The test alias queue.</summary>
        [Test]
        public void testAliasQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("alias");
            Assert.IsNotNull(queue);
            Assert.AreEqual("spam", queue.Name);
            Assert.AreNotSame("alias", queue.Name);
        }

        /// <summary>The test override queue.</summary>
        [Test]
        public void testOverrideQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("override");
            Assert.IsNotNull(queue);
            Assert.AreEqual("override", queue.Name);
            Assert.True(queue.Durable);
            Assert.True(queue.Exclusive);
            Assert.True(queue.AutoDelete);
        }

        /// <summary>The test override alias queue.</summary>
        [Test]
        public void testOverrideAliasQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("overrideAlias");
            Assert.IsNotNull(queue);
            Assert.AreEqual("bar", queue.Name);
            Assert.True(queue.Durable);
            Assert.True(queue.Exclusive);
            Assert.True(queue.AutoDelete);
        }

        /// <summary>The test anonymous queue.</summary>
        [Test]
        public void testAnonymousQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("anonymous");
            Assert.IsNotNull(queue);
            Assert.AreNotEqual(queue.Name, "anonymous");
            Assert.True(queue is AnonymousQueue);
            Assert.False(queue.Durable, "Durable is incorrect value");
            Assert.True(queue.Exclusive, "Exclusive is incorrect value");
            Assert.True(queue.AutoDelete, "AutoDelete is incorrect value");
        }

        /// <summary>The test arguments queue.</summary>
        [Test]
        public void testArgumentsQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("arguments");
            Assert.IsNotNull(queue);
            Assert.AreEqual("bar", queue.Arguments["foo"]);
            Assert.AreEqual("baz", queue.Arguments["bar"]);
        }

        /// <summary>The test anonymous arguments queue.</summary>
        [Test]
        public void testAnonymousArgumentsQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("anonymousArguments");
            Assert.IsNotNull(queue);
            Assert.AreEqual("spam", queue.Arguments["foo"]);
            Assert.AreEqual("more-spam", queue.Arguments["bar"]);
        }

        /// <summary>The test illegal anonymous queue.</summary>
        [Test]
        public void testIllegalAnonymousQueue()
        {
            var resourceName =
                @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                + typeof(QueueParserTests).Name + "IllegalAnonymous-context.xml";
            var resource = new AssemblyResource(resourceName);

            Assert.Throws<ObjectDefinitionStoreException>(() => new XmlObjectFactory(resource), "Parser fails to reject invalid state of anonymous queues");
        }
    }
}
