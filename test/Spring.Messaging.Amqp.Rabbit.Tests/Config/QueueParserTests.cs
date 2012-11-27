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
        public void TestQueue()
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
        public void TestAliasQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("alias");
            Assert.IsNotNull(queue);
            Assert.AreEqual("spam", queue.Name);
            Assert.AreNotSame("alias", queue.Name);
        }

        /// <summary>The test override queue.</summary>
        [Test]
        public void TestOverrideQueue()
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
        public void TestOverrideAliasQueue()
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
        public void TestAnonymousQueue()
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
        public void TestArgumentsQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("arguments");
            Assert.IsNotNull(queue);
            Assert.AreEqual("bar", queue.Arguments["foo"]);
            Assert.AreEqual(100L, queue.Arguments["x-message-ttl"]);
            Assert.AreEqual("all", queue.Arguments["x-ha-policy"]);
        }

        /// <summary>The test anonymous arguments queue.</summary>
        [Test]
        public void TestAnonymousArgumentsQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("anonymousArguments");
            Assert.IsNotNull(queue);
            Assert.AreEqual("spam", queue.Arguments["foo"]);
        }

        /// <summary>The test referenced arguments queue.</summary>
        [Test]
        public void TestReferencedArgumentsQueue()
        {
            var queue = this.objectFactory.GetObject<Queue>("referencedArguments");
            Assert.IsNotNull(queue);
            Assert.AreEqual("qux", queue.Arguments["baz"]);
        }

        /// <summary>The test illegal anonymous queue.</summary>
        [Test]
        public void TestIllegalAnonymousQueue()
        {
            var resourceName =
                @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                + typeof(QueueParserTests).Name + "IllegalAnonymous-context.xml";
            var resource = new AssemblyResource(resourceName);

            Assert.Throws<ObjectDefinitionStoreException>(() => new XmlObjectFactory(resource), "Parser fails to reject invalid state of anonymous queues");
        }

        [Test]
        public void When_ExplicitIdSetWithoutExplicitName_ObjectRegistrationUsesIdAsObjectDefintionName()
        {
            Assert.That(objectFactory.ContainsObject("explicit-id-but-no-explicit-name"), Is.True);
        }

        [Test]
        public void When_ExplicitNameSetWithoutExplicitId_ObjectRegistrationUsesNameAsObjectDefintionName()
        {
            Assert.That(objectFactory.ContainsObject("explicit-name-but-no-explicit-id"), Is.True);
        }

        [Test]
        public void When_ExplicitNameSetWithoutExplicitId_NameIsUsedAsQueueName()
        {
            const string objectIdentifier = "explicit-name-but-no-explicit-id";

            var queue = objectFactory.GetObject<Queue>(objectIdentifier);
            Assert.That(queue.Name, Is.EqualTo(objectIdentifier));
        }

        [Test]
        public void When_ExplicitIdAndExplicitNameSet_ObjectRegistrationUsesIdAsObjectDefintionName_and_NameIsUsedAsQueueName()
        {
            var queue = objectFactory.GetObject<Queue>("explicit-id-and-explicit-name");
            Assert.That(queue.Name, Is.EqualTo("the-queue-name"));
        }
    }

}
