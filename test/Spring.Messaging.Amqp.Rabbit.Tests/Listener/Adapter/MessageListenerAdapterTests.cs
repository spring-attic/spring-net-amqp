// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageListenerAdapterTests.cs" company="The original author or authors.">
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
using System;
using System.Text;
using NUnit.Framework;
using Spring.Aop.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
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
        private SimpleService service;

        /// <summary>The init.</summary>
        [SetUp]
        public void Init()
        {
            this.service = new SimpleService();

            this.messageProperties = new MessageProperties();
            this.messageProperties.ContentType = MessageProperties.CONTENT_TYPE_TEXT_PLAIN;

            this.adapter = new MockMessageListenerAdapter();
            this.adapter.MessageConverter = new SimpleMessageConverter();
        }

        /// <summary>
        /// Tests the default listener method.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestDefaultListenerMethod()
        {
            var handler = new HandlerDelegate(this.service);

            this.adapter.HandlerObject = handler;

            this.adapter.OnMessage(new Message(Encoding.UTF8.GetBytes("foo"), this.messageProperties));
            Assert.True(SimpleService.called);
        }

        /// <summary>
        /// Tests the explicit listener method.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestExplicitListenerMethod()
        {
            this.adapter.DefaultListenerMethod = "Handle";
            this.adapter.HandlerObject = this.service;
            this.adapter.OnMessage(new Message(Encoding.UTF8.GetBytes("foo"), this.messageProperties));
            Assert.True(SimpleService.called);
        }

        /// <summary>
        /// Tests the proxy listener.
        /// </summary>
        [Test]
        // [Ignore("Need Steve or Mark to look at this... Validated that the proxied type does get called, but this.service.called doesn't return true...?")]
        public void TestProxyListener()
        {
            this.adapter.DefaultListenerMethod = "NotDefinedOnInterface";
            var factory = new ProxyFactory();
            factory.Target = this.service;
            factory.ProxyTargetType = true;
            this.adapter.HandlerObject = factory.GetProxy();
            this.adapter.OnMessage(new Message(Encoding.UTF8.GetBytes("foo"), this.messageProperties));
            Assert.True(SimpleService.called);
        }

        /// <summary>
        /// Tests the JDK proxy listener.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestJdkProxyListener()
        {
            this.adapter.DefaultListenerMethod = "Handle";
            var factory = new ProxyFactory(this.service);
            factory.ProxyTargetType = false;
            this.adapter.HandlerObject = factory.GetProxy();
            this.adapter.OnMessage(new Message(Encoding.UTF8.GetBytes("foo"), this.messageProperties));
            Assert.True(SimpleService.called);
        }
    }

    /// <summary>
    /// A handler delegate.
    /// </summary>
    /// <remarks></remarks>
    internal class HandlerDelegate
    {
        /// <summary>
        /// The service.
        /// </summary>
        private SimpleService service;

        /// <summary>Initializes a new instance of the <see cref="HandlerDelegate"/> class.</summary>
        /// <param name="service">The service.</param>
        /// <remarks></remarks>
        public HandlerDelegate(SimpleService service) { this.service = service; }

        /// <summary>Handles the message.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The handled message.</returns>
        /// <remarks></remarks>
        public string HandleMessage(string input)
        {
            SimpleService.called = true;
            return "processed" + input;
        }
    }

    /// <summary>
    /// An IService inteface.
    /// </summary>
    /// <remarks></remarks>
    public interface IService
    {
        /// <summary>Handles the specified input.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The handled input.</returns>
        /// <remarks></remarks>
        string Handle(string input);
    }

    /// <summary>
    /// A simple service.
    /// </summary>
    /// <remarks></remarks>
    public class SimpleService : IService
    {
        /// <summary>
        /// Whether this has been called.
        /// </summary>
        public static bool called;

        /// <summary>Gets or sets a value indicating whether called.</summary>
        public bool Called { get { return called; } set { called = value; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleService"/> class. 
        /// </summary>
        /// <remarks>
        /// </remarks>
        public SimpleService() { called = false; }

        /// <summary>Handles the specified input.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The handled input.</returns>
        /// <remarks></remarks>
        public string Handle(string input)
        {
            called = true;
            return "processed" + input;
        }

        /// <summary>Nots the defined on interface.</summary>
        /// <param name="input">The input.</param>
        /// <returns>Whether the input is defined on the interface.</returns>
        /// <remarks></remarks>
        public string NotDefinedOnInterface(string input)
        {
            called = true;
            return "processed" + input;
        }
    }

    /// <summary>
    /// A mock message listener adapter.
    /// </summary>
    /// <remarks></remarks>
    internal class MockMessageListenerAdapter : MessageListenerAdapter
    {
        /// <summary>Handle the given exception that arose during listener execution.
        /// The default implementation logs the exception at error level.
        /// <para>This method only applies when used with <see cref="IMessageListener"/>.
        /// In case of the Spring <see cref="IChannelAwareMessageListener"/> mechanism,
        /// exceptions get handled by the caller instead.</para>
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        /// <remarks></remarks>
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
