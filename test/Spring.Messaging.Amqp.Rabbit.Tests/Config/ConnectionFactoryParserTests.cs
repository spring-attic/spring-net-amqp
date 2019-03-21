// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionFactoryParserTests.cs" company="The original author or authors.">
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
using NUnit.Framework;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// ConnectionFactoryParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class ConnectionFactoryParserTests
    {
        private XmlObjectFactory objectFactory;

        /// <summary>The setup.</summary>
        [TestFixtureSetUp]
        public void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/" + typeof(ConnectionFactoryParserTests).Name + "-context.xml";
            var resource = new AssemblyResource(resourceName);
            this.objectFactory = new XmlObjectFactory(resource);
        }

        /// <summary>The test kitchen sink.</summary>
        [Test]
        public void TestKitchenSink()
        {
            var connectionFactory = this.objectFactory.GetObject<CachingConnectionFactory>("kitchenSink");
            Assert.IsNotNull(connectionFactory);
            Assert.AreEqual(10, connectionFactory.ChannelCacheSize);

            // assertNull(dfa.getPropertyValue("executorService"));
            Assert.AreEqual(true, connectionFactory.IsPublisherConfirms);
            Assert.AreEqual(true, connectionFactory.IsPublisherReturns);
        }

        /// <summary>The test native.</summary>
        [Test]
        public void TestNative()
        {
            var connectionFactory = this.objectFactory.GetObject<CachingConnectionFactory>("native");
            Assert.IsNotNull(connectionFactory);
            Assert.AreEqual(10, connectionFactory.ChannelCacheSize);
        }

        /// <summary>The test with executor.</summary>
        [Test]
        public void TestWithExecutor()
        {
            var connectionFactory = this.objectFactory.GetObject<CachingConnectionFactory>("withExecutor");
            Assert.NotNull(connectionFactory);
            Assert.AreEqual(10, connectionFactory.ChannelCacheSize);

            // var executor = new DirectFieldAccessor(connectionFactory).getPropertyValue("executorService");
            // Assert.NotNull(executor);
            // var exec = this.objectFactory.GetObject<IThreadPoolTaskExecutor>("exec");
            // Assert.AreSame(exec.getThreadPoolExecutor(), executor);
            // var dfa = new DirectFieldAccessor(connectionFactory);
            Assert.AreEqual(false, connectionFactory.IsPublisherConfirms);
            Assert.AreEqual(false, connectionFactory.IsPublisherReturns);
        }

        /// <summary>The test with executor service.</summary>
        [Test]
        public void TestWithExecutorService()
        {
            var connectionFactory = this.objectFactory.GetObject<CachingConnectionFactory>("withExecutorService");
            Assert.NotNull(connectionFactory);
            Assert.AreEqual(10, connectionFactory.ChannelCacheSize);

            // var executor = new DirectFieldAccessor(connectionFactory).getPropertyValue("executorService");
            // Assert.NotNull(executor);
            // var exec = this.objectFactory.GetObject<IExecutorService>("execService");
            // Assert.AreSame(exec, executor);
        }

        /// <summary>The test multi host.</summary>
        [Test]
        public void TestMultiHost()
        {
            var connectionFactory = this.objectFactory.GetObject<CachingConnectionFactory>("multiHost");
            Assert.NotNull(connectionFactory);
            Assert.AreEqual(10, connectionFactory.ChannelCacheSize);

            // DirectFieldAccessor dfa =  new DirectFieldAccessor(connectionFactory);
            var addresses = connectionFactory.AmqpTcpEndpoints;
            Assert.AreEqual(3, addresses.Length);
            Assert.AreEqual("host1", addresses[0].HostName);
            Assert.AreEqual(1234, addresses[0].Port);
            Assert.AreEqual("host2", addresses[1].HostName);

            // Assert.AreEqual(-1, addresses[1].Port);
            Assert.AreEqual(5672, addresses[1].Port);
            Assert.AreEqual("host3", addresses[2].HostName);
            Assert.AreEqual(4567, addresses[2].Port);
        }
    }
}
