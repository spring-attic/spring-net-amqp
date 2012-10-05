// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionFactoryParserTests.cs" company="The original author or authors.">
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
        public void testKitchenSink()
        {
            var connectionFactory = this.objectFactory.GetObject<CachingConnectionFactory>("kitchenSink");
            Assert.IsNotNull(connectionFactory);
            Assert.AreEqual(10, connectionFactory.ChannelCacheSize);
        }

        /// <summary>The test native.</summary>
        [Test]
        public void testNative()
        {
            var connectionFactory = this.objectFactory.GetObject<CachingConnectionFactory>("native");
            Assert.IsNotNull(connectionFactory);
            Assert.AreEqual(10, connectionFactory.ChannelCacheSize);
        }
    }
}
