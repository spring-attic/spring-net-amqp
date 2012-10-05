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
        public void testParseWithQueueNames()
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

        /// <summary>The test parse with advice chain.</summary>
        [Test]
        [Ignore("Need to determine how to allow injection of IAdvice[] via config...")]
        public void testParseWithAdviceChain()
        {
            var container = this.objectFactory.GetObject<SimpleMessageListenerContainer>("container3");
            var fields = typeof(SimpleMessageListenerContainer).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var adviceChainField = typeof(SimpleMessageListenerContainer).GetField("adviceChain", BindingFlags.NonPublic | BindingFlags.Instance);

            var adviceChain = adviceChainField.GetValue(container);
            Assert.IsNotNull(adviceChain);
            Assert.AreEqual(3, ((IAdvice[])adviceChain).Count());
        }

        /// <summary>The test parse with defaults.</summary>
        [Test]
        public void testParseWithDefaults()
        {
            var container = this.objectFactory.GetObject<SimpleMessageListenerContainer>("container4");
            var concurrentConsumersField = typeof(SimpleMessageListenerContainer).GetField("concurrentConsumers", BindingFlags.NonPublic | BindingFlags.Instance);

            var concurrentConsumers = concurrentConsumersField.GetValue(container);
            Assert.AreEqual(1, concurrentConsumers);
        }
    }
}
