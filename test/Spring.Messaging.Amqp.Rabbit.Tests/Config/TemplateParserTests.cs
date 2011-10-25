
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// TemplateParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class TemplateParserTests
    {
        private XmlObjectFactory beanFactory;

        /// <summary>
        /// Setups this instance.
        /// </summary>
        [TestFixtureSetUp]
        public void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName =
                @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                + typeof(TemplateParserTests).Name + "-context.xml";
            var resource = new AssemblyResource(resourceName);
            beanFactory = new XmlObjectFactory(resource);
        }

        [Test]
        public void testTemplate()
        {
            var template = beanFactory.GetObject<IAmqpTemplate>("template");
            Assert.IsNotNull(template);
        }

        [Test]
        public void testKitchenSink()
        {
            var template = beanFactory.GetObject<RabbitTemplate>("kitchenSink");
            Assert.IsNotNull(template);
            Assert.True(template.MessageConverter is SimpleMessageConverter);
        }
    }
}
