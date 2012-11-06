// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TemplateParserTests.cs" company="The original author or authors.">
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
using System.Reflection;
using Moq;
using NUnit.Framework;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Objects.Factory.Xml;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// TemplateParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class TemplateParserTests
    {
        private XmlObjectFactory objectFactory;

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
            this.objectFactory = new XmlObjectFactory(resource);
        }

        /// <summary>The test template.</summary>
        [Test]
        public void TestTemplate()
        {
            var template = this.objectFactory.GetObject<IAmqpTemplate>("template");
            Assert.IsNotNull(template);
            Assert.AreEqual(false, ReflectionUtils.GetInstanceFieldValue(template, "mandatory"));
            Assert.AreEqual(false, ReflectionUtils.GetInstanceFieldValue(template, "immediate"));
            Assert.IsNull(ReflectionUtils.GetInstanceFieldValue(template, "returnCallback"));
            Assert.IsNull(ReflectionUtils.GetInstanceFieldValue(template, "confirmCallback"));
        }

        [Test]
        public void TestTemplateWithCallbacks()
        {
            var template = this.objectFactory.GetObject<IAmqpTemplate>("withCallbacks");
            Assert.IsNotNull(template);
            Assert.AreEqual(true, ReflectionUtils.GetInstanceFieldValue(template, "mandatory"));
            Assert.AreEqual(true, ReflectionUtils.GetInstanceFieldValue(template, "immediate"));
            Assert.IsNotNull(ReflectionUtils.GetInstanceFieldValue(template, "returnCallback"));
            Assert.IsNotNull(ReflectionUtils.GetInstanceFieldValue(template, "confirmCallback"));
        }

        /// <summary>The test kitchen sink.</summary>
        [Test]
        public void TestKitchenSink()
        {
            var template = this.objectFactory.GetObject<RabbitTemplate>("kitchenSink");
            Assert.IsNotNull(template);
            Assert.True(template.MessageConverter is SimpleMessageConverter);
        }

        [Test]
        public void TestWithReplyQ()
        {
            var template = this.objectFactory.GetObject<IAmqpTemplate>("withReplyQ");
            Assert.IsNotNull(template);
            var queue = (Queue)ReflectionUtils.GetInstanceFieldValue(template, "replyQueue");
            Assert.IsNotNull(queue);

            // TODO: Once GetObject<T>(nameValue) works correctly var queueObject = this.objectFactory.GetObject<Queue>("reply.queue");
            var queueObject = this.objectFactory.GetObject<Queue>("replyQId");
            Assert.AreSame(queueObject, queue);
            var container = this.objectFactory.GetObject<SimpleMessageListenerContainer>("withReplyQ.ReplyListener");
            Assert.IsNotNull(container);
            var messageListenerField = typeof(AbstractMessageListenerContainer).GetField("messageListener", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreSame(template, messageListenerField.GetValue(container));
            var messageListenerContainer = this.objectFactory.GetObject<SimpleMessageListenerContainer>();
            var queueNamesField = typeof(AbstractMessageListenerContainer).GetField("queueNames", BindingFlags.NonPublic | BindingFlags.Instance);
            var queueNames = (string[])queueNamesField.GetValue(messageListenerContainer);
            Assert.AreEqual(queueObject.Name, queueNames[0]);
        }
    }

    public class MockCallbackFactory
    {
        public T GetMock<T>() where T : class
        {
            return new Mock<T>().Object;
        }
    }
}
