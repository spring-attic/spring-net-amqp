// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueueParserPlaceholderTests.cs" company="The original author or authors.">
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
using NUnit.Framework;
using Spring.Context.Support;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// QueueParserPlaceholder Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class QueueParserPlaceholderTests : QueueParserTests
    {
        /// <summary>
        /// Setups this instance.
        /// </summary>
        [TestFixtureSetUp]
        public override void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName =
                @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                + typeof(QueueParserPlaceholderTests).Name + "-context.xml";

            // var resource = new AssemblyResource(resourceName);
            this.objectFactory = new XmlApplicationContext(resourceName);
        }

        /// <summary>The property place holder configurer can config property on non rabbit object.</summary>
        [Test]
        public void PropertyPlaceHolderConfigurerCanConfigPropertyOnNonRabbitObject()
        {
            var obj = this.objectFactory.GetObject<PlaceholderSanityCheckTestObject>("placeholder-sanity-check");
            Assert.That(obj.Name, Is.EqualTo("foo"), "PropertyConfiguration infrastructure is not working as expected.");
            Assert.That(obj.Arguments["foo"], Is.EqualTo("foo"));
        }

        /// <summary>The can get rabbit queue.</summary>
        [Test]
        public void CanGetRabbitQueue()
        {
            var obj = this.objectFactory.GetObject<Queue>("arguments");
            Assert.That(obj.Arguments["foo"], Is.EqualTo("bar"));
            Assert.That(obj.Arguments["bar"], Is.EqualTo("baz"));
        }

        /// <summary>The close object factory.</summary>
        [TestFixtureTearDown]
        public void closeObjectFactory()
        {
            if (this.objectFactory != null)
            {
                this.objectFactory.Dispose();
            }
        }
    }

    /// <summary>The placeholder sanity check test object.</summary>
    public class PlaceholderSanityCheckTestObject
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the arguments.</summary>
        public IDictionary<string, object> Arguments { get; set; }
    }
}
