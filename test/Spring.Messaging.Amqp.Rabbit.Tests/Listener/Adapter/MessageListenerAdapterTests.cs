// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageListenerAdapterTests.cs" company="The original author or authors.">
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
using System;
using System.Text;
using NUnit.Framework;
using Spring.Aop.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using Spring.Messaging.Amqp.Support.Converter;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener.Adapter
{
    /// <summary>The message listener adapter tests.</summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class MessageListenerAdapterTests
    {
        private MessageProperties messageProperties;
        private MessageListenerAdapter adapter;

        /// <summary>The init.</summary>
        [SetUp]
        public void Init()
        {
            SimpleService.Called = false;

            this.messageProperties = new MessageProperties();
            this.messageProperties.ContentType = MessageProperties.CONTENT_TYPE_TEXT_PLAIN;

            this.adapter = new MockMessageListenerAdapter();
            this.adapter.MessageConverter = new SimpleMessageConverter();
        }

        /// <summary>
        /// Tests the default listener method.
        /// </summary>
        [Test]
        public void TestDefaultListenerMethod()
        {
            var called = new AtomicBoolean(false);
            var handlerDelegate = new HandlerDelegate(called);

            this.adapter.HandlerObject = handlerDelegate;
            this.adapter.OnMessage(new Message(Encoding.UTF8.GetBytes("foo"), this.messageProperties));
            Assert.True(handlerDelegate.Called);
        }

        /// <summary>The test func listener method.</summary>
        [Test]
        public void TestFuncListenerMethod()
        {
            var called = new AtomicBoolean(false);
            var handlerDelegate = new Func<string, string>(
                input =>
                {
                    called.LazySet(true);
                    return "processed" + input;
                });

            this.adapter.HandlerObject = handlerDelegate;
            this.adapter.OnMessage(new Message(Encoding.UTF8.GetBytes("foo"), this.messageProperties));
            Assert.True(called.Value);
        }

        /// <summary>
        /// Tests the explicit listener method.
        /// </summary>
        [Test]
        public void TestExplicitListenerMethod()
        {
            this.adapter.DefaultListenerMethod = "Handle";
            this.adapter.HandlerObject = new SimpleService();
            this.adapter.OnMessage(new Message(Encoding.UTF8.GetBytes("foo"), this.messageProperties));
            Assert.True(SimpleService.Called);
        }

        /// <summary>
        /// Tests the proxy listener.
        /// </summary>
        [Test]
        public void TestProxyListener()
        {
            this.adapter.DefaultListenerMethod = "NotDefinedOnInterface";
            var factory = new ProxyFactory(new SimpleService());
            factory.ProxyTargetType = true;
            this.adapter.HandlerObject = factory.GetProxy();
            this.adapter.OnMessage(new Message(Encoding.UTF8.GetBytes("foo"), this.messageProperties));
            Assert.True(SimpleService.Called);
        }

        /// <summary>
        /// Tests the JDK proxy listener.
        /// </summary>
        [Test]
        public void TestJdkProxyListener()
        {
            this.adapter.DefaultListenerMethod = "Handle";
            var factory = new ProxyFactory(new SimpleService());
            factory.ProxyTargetType = false;
            this.adapter.HandlerObject = factory.GetProxy();
            this.adapter.OnMessage(new Message(Encoding.UTF8.GetBytes("foo"), this.messageProperties));
            Assert.True(SimpleService.Called);
        }
    }

    /// <summary>
    /// A handler delegate.
    /// </summary>
    internal class HandlerDelegate
    {
        public AtomicBoolean Called;

        /// <summary>Initializes a new instance of the <see cref="HandlerDelegate"/> class.</summary>
        /// <param name="called">The called.</param>
        public HandlerDelegate(AtomicBoolean called) { this.Called = called; }

        /// <summary>Handles the message.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The handled message.</returns>
        public string HandleMessage(string input)
        {
            this.Called.LazySet(true);
            return "processed" + input;
        }
    }

    /// <summary>
    /// An IService inteface.
    /// </summary>
    public interface IService
    {
        /// <summary>Handles the specified input.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The handled input.</returns>
        string Handle(string input);
    }

    /// <summary>
    /// A simple service.
    /// </summary>
    public class SimpleService : IService
    {
        /// <summary>
        /// Whether this has been called.
        /// </summary>
        public static bool Called;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleService"/> class. 
        /// </summary>
        public SimpleService() { Called = false; }

        /// <summary>Handles the specified input.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The handled input.</returns>
        public string Handle(string input)
        {
            Called = true;
            return "processed" + input;
        }

        /// <summary>Nots the defined on interface.</summary>
        /// <param name="input">The input.</param>
        /// <returns>Whether the input is defined on the interface.</returns>
        public string NotDefinedOnInterface(string input)
        {
            Called = true;
            return "processed" + input;
        }
    }

    /// <summary>
    /// A mock message listener adapter.
    /// </summary>
    internal class MockMessageListenerAdapter : MessageListenerAdapter
    {
        /// <summary>Handle the given exception that arose during listener execution.
        /// The default implementation logs the exception at error level.
        /// <para>This method only applies when used with <see cref="IMessageListener"/>.
        /// In case of the Spring <see cref="IChannelAwareMessageListener"/> mechanism,
        /// exceptions get handled by the caller instead.</para>
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        protected override void HandleListenerException(Exception ex)
        {
            if (ex is SystemException)
            {
                throw ex;
            }

            throw new InvalidOperationException(ex.Message, ex);
        }
    }
}
