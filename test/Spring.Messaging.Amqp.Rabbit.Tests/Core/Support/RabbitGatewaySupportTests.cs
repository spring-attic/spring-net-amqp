// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitGatewaySupportTests.cs" company="The original author or authors.">
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
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Core.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core.Support
{
    /// <summary>
    /// Rabbit gateway support tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class RabbitGatewaySupportTests
    {
        /// <summary>
        /// Tests the rabbit gateway support with connection factory.
        /// </summary>
        [Test]
        public void TestRabbitGatewaySupportWithConnectionFactory()
        {
            var mockConnectionFactory = new Mock<IConnectionFactory>();
            var test = new List<string>();
            var gateway = new TestGateway(test);

            gateway.ConnectionFactory = mockConnectionFactory.Object;
            gateway.AfterPropertiesSet();
            Assert.AreEqual(mockConnectionFactory.Object, gateway.ConnectionFactory, "Correct ConnectionFactory");
            Assert.AreEqual(mockConnectionFactory.Object, gateway.RabbitTemplate.ConnectionFactory, "Correct RabbitTemplate");
            Assert.AreEqual(test.Count, 1, "initGatway called");
        }

        /// <summary>
        /// Tests the rabbit gateway support with JMS template.
        /// </summary>
        [Test]
        public void TestRabbitGatewaySupportWithJmsTemplate()
        {
            var template = new RabbitTemplate();
            var test = new List<string>();
            var gateway = new TestGateway(test);
            gateway.RabbitTemplate = template;
            gateway.AfterPropertiesSet();
            Assert.AreEqual(template, gateway.RabbitTemplate, "Correct RabbitTemplate");
            Assert.AreEqual(test.Count, 1, "initGateway called");
        }

        /// <summary>
        /// A stup test gateway.
        /// </summary>
        internal class TestGateway : RabbitGatewaySupport
        {
            /// <summary>
            /// The test list.
            /// </summary>
            private readonly List<string> testList;

            /// <summary>Initializes a new instance of the <see cref="TestGateway"/> class.</summary>
            /// <param name="testList">The test list.</param>
            public TestGateway(List<string> testList) { this.testList = testList; }

            /// <summary>
            /// Subclasses can override this for custom initialization behavior.
            /// Gets called after population of this instance's properties.
            /// </summary>
            protected override void InitGateway() { this.testList.Add("test"); }
        }
    }
}
