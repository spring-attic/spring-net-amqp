
using System;
using System.Collections.Generic;
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
    [Ignore("Need to fix...")]
    public class QueueParserTests
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
                + typeof(QueueParserTests).Name + "-context.xml";
            var resource = new AssemblyResource(resourceName);
            beanFactory = new XmlObjectFactory(resource);
        }

        [Test]
        public void testQueue()
        {
            Queue queue = beanFactory.GetObject<Queue>("foo");
            Assert.IsNotNull(queue);
            Assert.AreEqual("foo", queue.Name);
            Assert.True(queue.Durable);
            Assert.False(queue.AutoDelete);
            Assert.False(queue.Exclusive);
        }

        [Test]
        public void testAliasQueue()
        {
            Queue queue = beanFactory.GetObject<Queue>("alias");
            Assert.IsNotNull(queue);
            Assert.AreEqual("spam", queue.Name);
            Assert.AreNotSame("alias", queue.Name);
        }

        [Test]
        public void testOverrideQueue()
        {
            Queue queue = beanFactory.GetObject<Queue>("override");
            Assert.IsNotNull(queue);
            Assert.AreEqual("override", queue.Name);
            Assert.True(queue.Durable);
            Assert.True(queue.Exclusive);
            Assert.True(queue.AutoDelete);
        }

        [Test]
        public void testOverrideAliasQueue()
        {
            Queue queue = beanFactory.GetObject<Queue>("overrideAlias");
            Assert.IsNotNull(queue);
            Assert.AreEqual("bar", queue.Name);
            Assert.True(queue.Durable);
            Assert.True(queue.Exclusive);
            Assert.True(queue.AutoDelete);
        }

        [Test]
        public void testAnonymousQueue()
        {
            Queue queue = beanFactory.GetObject<Queue>("anonymous");
            Assert.IsNotNull(queue);
            Assert.AreNotSame("anonymous", queue.Name);
            Assert.True(queue is AnonymousQueue);
            Assert.False(queue.Durable);
            Assert.True(queue.Exclusive);
            Assert.True(queue.AutoDelete);
        }

        [Test]
        public void testArgumentsQueue()
        {
            Queue queue = beanFactory.GetObject<Queue>("arguments");
            Assert.IsNotNull(queue);
            Assert.AreEqual("bar", queue.Arguments["foo"]);
        }

        [Test]
        public void testAnonymousArgumentsQueue()
        {
            Queue queue = beanFactory.GetObject<Queue>("anonymousArguments");
            Assert.IsNotNull(queue);
            Assert.AreEqual("spam", queue.Arguments["foo"]);
        }

        [Test]
        public void testIllegalAnonymousQueue()
        {
            try
            {
                var resourceName =
                    @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                    + typeof(QueueParserTests).Name + "IllegalAnonymous-context.xml";
                var resource = new AssemblyResource(resourceName);
                beanFactory = new XmlObjectFactory(resource);

                Queue queue = beanFactory.GetObject<Queue>("anonymous");
                Assert.IsNotNull(queue);
                Assert.AreNotSame("bucket", queue.Name);
                Assert.True(queue is AnonymousQueue);
            }
            catch (ObjectDefinitionStoreException e)
            {
                // Expected
            }

        }
    }
}
