// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ListenerContainerPlaceholderParserTests.cs" company="The original author or authors.">
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
using Spring.Context.Support;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Xml;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// ListenerContainerPlaceholderParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class ListenerContainerPlaceholderParserTests
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
                + typeof(ListenerContainerPlaceholderParserTests).Name + "-context.xml";
            
            this.objectFactory = new XmlApplicationContext(resourceName);
        }

        /// <summary>The close object factory.</summary>
        [TestFixtureTearDown]
        public void CloseObjectFactory()
        {
            if (this.objectFactory != null)
            {
                this.objectFactory.Dispose();
            }
        }

        /// <summary>The test parse with queue names.</summary>
        [Test]
        public void TestParseWithQueueNames()
        {
            var container = this.objectFactory.GetObject<SimpleMessageListenerContainer>("container1");
            Assert.AreEqual(AcknowledgeModeUtils.AcknowledgeMode.Manual, container.AcknowledgeMode);
            Assert.AreEqual(this.objectFactory.GetObject<IConnectionFactory>("connectionFactory"), container.ConnectionFactory);
            Assert.AreEqual(typeof(MessageListenerAdapter), container.MessageListener.GetType());
            Assert.AreEqual(5, ReflectionUtils.GetInstanceFieldValue(container, "concurrentConsumers"), "concurrency placeholder not processed correctly");
            Assert.AreEqual(1, ReflectionUtils.GetInstanceFieldValue(container, "txSize"), "transaction-size placeholder not processed correctly");
            Assert.IsFalse(container.AutoStartup, "auto-startup placeholder not processed correctly");

            var listenerAccessor = container.MessageListener;
            Assert.AreEqual(this.objectFactory.GetObject<TestObject>("testObject"), ((MessageListenerAdapter)listenerAccessor).HandlerObject);

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
    }
}
