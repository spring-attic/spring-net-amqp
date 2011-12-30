
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Spring.Context.Config;
using Spring.Context.Support;
using Spring.Context;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Xml;

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
            objectFactory = new XmlObjectFactory(resource);
        }

        [Test]
        public void testQueue()
        {
            Queue queue = objectFactory.GetObject<Queue>("foo");
            Assert.IsNotNull(queue);
            Assert.AreEqual("foo", queue.Name);
            Assert.True(queue.Durable);
            Assert.False(queue.AutoDelete);
            Assert.False(queue.Exclusive);
        }

        [Test]
        public void testAliasQueue()
        {
            Queue queue = objectFactory.GetObject<Queue>("alias");
            Assert.IsNotNull(queue);
            Assert.AreEqual("spam", queue.Name);
            Assert.AreNotSame("alias", queue.Name);
        }

        [Test]
        public void testOverrideQueue()
        {
            Queue queue = objectFactory.GetObject<Queue>("override");
            Assert.IsNotNull(queue);
            Assert.AreEqual("override", queue.Name);
            Assert.True(queue.Durable);
            Assert.True(queue.Exclusive);
            Assert.True(queue.AutoDelete);
        }

        [Test]
        public void testOverrideAliasQueue()
        {
            Queue queue = objectFactory.GetObject<Queue>("overrideAlias");
            Assert.IsNotNull(queue);
            Assert.AreEqual("bar", queue.Name);
            Assert.True(queue.Durable);
            Assert.True(queue.Exclusive);
            Assert.True(queue.AutoDelete);
        }

        [Test]
        [Ignore]
        public void testAnonymousQueue()
        {
            Queue queue = objectFactory.GetObject<Queue>("anonymous");
            Assert.IsNotNull(queue);
            Assert.AreNotEqual(queue.Name, "anonymous");
            Assert.True(queue is AnonymousQueue);
            Assert.False(queue.Durable, "Durable is incorrect value");
            Assert.True(queue.Exclusive, "Exclusive is incorrect value");
            Assert.True(queue.AutoDelete, "AutoDelete is incorrect value");
        }

        [Test]
        public void testArgumentsQueue()
        {
            Queue queue = objectFactory.GetObject<Queue>("arguments");
            Assert.IsNotNull(queue);
            Assert.AreEqual("bar", queue.Arguments["foo"]);
            Assert.AreEqual("baz", queue.Arguments["bar"]);
        }

        [Test]
        public void testAnonymousArgumentsQueue()
        {
            Queue queue = objectFactory.GetObject<Queue>("anonymousArguments");
            Assert.IsNotNull(queue);
            Assert.AreEqual("spam", queue.Arguments["foo"]);
            Assert.AreEqual("more-spam", queue.Arguments["bar"]);
        }

        [Test]
        public void testIllegalAnonymousQueue()
        {
                var resourceName =
                    @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                    + typeof(QueueParserTests).Name + "IllegalAnonymous-context.xml";
                var resource = new AssemblyResource(resourceName);

                Assert.Throws<ObjectDefinitionStoreException>(() => new XmlObjectFactory(resource),"Parser fails to reject invalid state of anonymous queues");
        }

        [Test]
        [Ignore("id <--> name parity NOT IMPLEMENTED")]
        public void WhenExplicitIdSetWithoutExplicitName_ObjectRegistrationUsesIdAsObjectDefintionName()
        {
            Assert.That(objectFactory.ContainsObject("explicit-id-but-no-explicit-name"), Is.True);
        }

        [Test]
        [Ignore("id <--> name parity NOT IMPLEMENTED")]
        public void WhenExplicitIdSetWithoutExplicitName_IdIsUsedAsQueueName()
        {
            const string objectIdentifier = "explicit-id-but-no-explicit-name";

            var queue = objectFactory.GetObject<Queue>(objectIdentifier);
            Assert.That(queue.Name, Is.EqualTo(objectIdentifier));
        }
        
        [Test]
        [Ignore("id <--> name parity NOT IMPLEMENTED")]
        public void WhenExplicitNameSetWithoutExplicitId_ObjectRegistrationUsesNameAsObjectDefintionName()
        {
            Assert.That(objectFactory.ContainsObject("explicit-name-but-no-explicit-id"), Is.True);
        }

        [Test]
        [Ignore("id <--> name parity NOT IMPLEMENTED")]
        public void WhenExplicitNameSetWithoutExplicitId_NameIsUsedAsQueueName()
        {
            const string objectIdentifier = "explicit-name-but-no-explicit-id";

            var queue = objectFactory.GetObject<Queue>(objectIdentifier);
            Assert.That(queue.Name, Is.EqualTo(objectIdentifier));
        }   
        
        [Test]
        [Ignore("id <--> name parity NOT IMPLEMENTED")]
        public void WhenExplicitIdAndExplicitNameSet_ObjectRegistrationUsesIdAsObjectDefintionName_and_NameIsUsedAsQueueName()
        {
            var queue = objectFactory.GetObject<Queue>("explicit-id-and-explicit-name");
            Assert.That(queue.Name, Is.EqualTo("the-queue-name"));
        }
    }


}
