// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ListenerContainerParserTests.cs" company="The original author or authors.">
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AopAlliance.Aop;
using NUnit.Framework;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// ListenerContainerParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class ListenerContainerParserTests
    {
        private XmlObjectFactory objectFactory;

        /// <summary>The setup.</summary>
        [TestFixtureSetUp]
        public void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/" + typeof(ListenerContainerParserTests).Name + "-context.xml";
            var resource = new AssemblyResource(resourceName);
            this.objectFactory = new XmlObjectFactory(resource);

            // ((IConfigurableObjectFactory)objectFactory).setObjectExpressionResolver(new StandardObjectExpressionResolver());
        }

        /// <summary>The test parse with queue names.</summary>
        [Test]
        public void TestParseWithQueueNames()
        {
            var container = this.objectFactory.GetObject<SimpleMessageListenerContainer>("container1");
            Assert.AreEqual(AcknowledgeModeUtils.AcknowledgeMode.Manual, container.AcknowledgeMode);
            Assert.AreEqual(this.objectFactory.GetObject<IConnectionFactory>(), container.ConnectionFactory);
            Assert.AreEqual(typeof(MessageListenerAdapter), container.MessageListener.GetType());
            var listenerAccessor = container.MessageListener;
            Assert.AreEqual(this.objectFactory.GetObject<TestObject>(), ((MessageListenerAdapter)listenerAccessor).HandlerObject);

            Assert.AreEqual("Handle", ((MessageListenerAdapter)listenerAccessor).DefaultListenerMethod);
            var queue = this.objectFactory.GetObject<Queue>("bar");
            var queueNamesForVerification = "[";
            foreach (var queueName in container.QueueNames)
            {
                queueNamesForVerification += queueNamesForVerification == "[" ? queueName : ", " + queueName;
            }

            queueNamesForVerification += "]";
            Assert.AreEqual("[foo, " + queue.Name + "]", queueNamesForVerification);
        }

        /// <summary>The test parse with queues.</summary>
        [Test]
        public void TestParseWithQueues()
        {
            var container = this.objectFactory.GetObject<SimpleMessageListenerContainer>("container2");
            var queue = this.objectFactory.GetObject<Queue>("bar");
            var queueNamesForVerification = "[";
            foreach (var queueName in container.QueueNames)
            {
                queueNamesForVerification += queueNamesForVerification == "[" ? queueName : ", " + queueName;
            }

            queueNamesForVerification += "]";
            Assert.AreEqual("[foo, " + queue.Name + "]", queueNamesForVerification);
        }

        /// <summary>The test parse with advice chain.</summary>
        [Test]
        public void TestParseWithAdviceChain()
        {
            var container = this.objectFactory.GetObject<SimpleMessageListenerContainer>("container3");
            var fields = typeof(SimpleMessageListenerContainer).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var adviceChainField = typeof(SimpleMessageListenerContainer).GetField("adviceChain", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = new List<IAdvice>();

            var adviceChain = adviceChainField.GetValue(container);
            Assert.IsNotNull(adviceChain);
            Assert.AreEqual(3, ((IAdvice[])adviceChain).Count());
        }

        /// <summary>The test parse with defaults.</summary>
        [Test]
        public void TestParseWithDefaults()
        {
            var container = this.objectFactory.GetObject<SimpleMessageListenerContainer>("container4");
            var concurrentConsumersField = typeof(SimpleMessageListenerContainer).GetField("concurrentConsumers", BindingFlags.NonPublic | BindingFlags.Instance);

            var concurrentConsumers = concurrentConsumersField.GetValue(container);
            Assert.AreEqual(1, concurrentConsumers);
        }

        /// <summary>The test parse with default queue rejected false.</summary>
        [Test]
        public void TestParseWithDefaultQueueRejectedFalse()
        {
            var container = this.objectFactory.GetObject<SimpleMessageListenerContainer>("container5");
            var concurrentConsumersField = typeof(SimpleMessageListenerContainer).GetField("concurrentConsumers", BindingFlags.NonPublic | BindingFlags.Instance);
            var concurrentConsumers = concurrentConsumersField.GetValue(container);
            var defaultRequeueRejectedField = typeof(SimpleMessageListenerContainer).GetField("defaultRequeueRejected", BindingFlags.NonPublic | BindingFlags.Instance);
            var defaultRequeueRejected = defaultRequeueRejectedField.GetValue(container);
            Assert.AreEqual(1, (int)concurrentConsumers);
            Assert.AreEqual(false, (bool)defaultRequeueRejected);
            Assert.IsFalse(container.ChannelTransacted);
        }

        /// <summary>The test parse with tx.</summary>
        [Test]
        public void TestParseWithTx()
        {
            var container = this.objectFactory.GetObject<SimpleMessageListenerContainer>("container6");
            Assert.IsTrue(container.ChannelTransacted);
            var txSizeField = typeof(SimpleMessageListenerContainer).GetField("txSize", BindingFlags.NonPublic | BindingFlags.Instance);
            var txSize = txSizeField.GetValue(container);
            Assert.AreEqual(5, (int)txSize);
        }

        /// <summary>The test incompatible tx atts.</summary>
        [Test]
        [Ignore("TODO")]
        public void TestIncompatibleTxAtts()
        {
            /*
            try
            {
                new ClassPathXmlApplicationContext(getClass().getSimpleName() + "-fail-context.xml", getClass());
                fail("Parse exception exptected");
            }
            catch (BeanDefinitionParsingException e)
            {
                assertTrue(e.getMessage().startsWith(
                        "Configuration problem: Listener Container - cannot set channel-transacted with acknowledge='NONE'"));
            }
            */
        }
    }
}
