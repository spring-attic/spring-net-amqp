// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueueArgumentsParserTests.cs" company="The original author or authors.">
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
using System.Collections;
using NUnit.Framework;
using Spring.Context;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;
using Spring.Testing.NUnit;
using Queue = Spring.Messaging.Amqp.Core.Queue;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// Queue Arguments Parser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class QueueArgumentsParserTests : AbstractDependencyInjectionSpringContextTests
    {
        protected Queue queue1;

        protected Queue queue2;

        /// <summary>Initializes a new instance of the <see cref="QueueArgumentsParserTests"/> class.</summary>
        public QueueArgumentsParserTests() { this.PopulateProtectedVariables = true; }

        /// <summary>Gets the config locations.</summary>
        protected override string[] ConfigLocations
        {
            get
            {
                var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/" + typeof(QueueArgumentsParserTests).Name + "-context.xml";
                return new[] { resourceName };
            }
        }

        /// <summary>The fixture set up.</summary>
        [TestFixtureSetUp]
        public void FixtureSetUp() { NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler)); }

        /// <summary>The test.</summary>
        [Test]
        public void Test()
        {
            var args = this.applicationContext.GetObject<IDictionary>("args");
            Assert.AreEqual("bar", args["foo"]);
            Assert.AreEqual("qux", this.queue1.Arguments["baz"]);
            Assert.AreEqual("bar", this.queue2.Arguments["foo"]);
        }
    }
}
