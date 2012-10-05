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
using NUnit.Framework;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Objects.Factory.Xml;
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
        public void testTemplate()
        {
            var template = this.objectFactory.GetObject<IAmqpTemplate>("template");
            Assert.IsNotNull(template);
        }

        /// <summary>The test kitchen sink.</summary>
        [Test]
        public void testKitchenSink()
        {
            var template = this.objectFactory.GetObject<RabbitTemplate>("kitchenSink");
            Assert.IsNotNull(template);
            Assert.True(template.MessageConverter is SimpleMessageConverter);
        }
    }
}
