using System;
using System.Collections.Generic;
using AutoMoq;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Text;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Core.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;

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
        /// <remarks></remarks>
        [Test]
        public void TestRabbitGatewaySupportWithConnectionFactory()
        {
            var mocker = new AutoMoqer();

            var mockConnectionFactory = mocker.GetMock<IConnectionFactory>();
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
        /// <remarks></remarks>
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
        /// <remarks></remarks>
        internal class TestGateway : RabbitGatewaySupport
        {
            /// <summary>
            /// The test list.
            /// </summary>
            private readonly List<string> testList;

            /// <summary>
            /// Initializes a new instance of the <see cref="TestGateway"/> class.
            /// </summary>
            /// <param name="testList">The test list.</param>
            /// <remarks></remarks>
            public TestGateway(List<string> testList) : base()
            {
                this.testList = testList;
            }

            /// <summary>
            /// Subclasses can override this for custom initialization behavior.
            /// Gets called after population of this instance's properties.
            /// </summary>
            /// <remarks></remarks>
            protected override void InitGateway()
            {
                this.testList.Add("test");
            }
        }
    }
}
