using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Spring.Messaging.Amqp.Rabbit.Admin;

namespace Spring.Messaging.Amqp.Rabbit.Test
{
    /// <summary>
    /// A base class for integration tests, to ensure that the broker is started, and that it is shut down after the test is done.
    /// </summary>
    /// <remarks></remarks>
    public class IntegrationTestBase
    {
        /// <summary>
        /// Fixtures the set up.
        /// </summary>
        /// <remarks></remarks>
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            BeforeFixtureSetUp();
            var brokerAdmin = new RabbitBrokerAdmin();
            brokerAdmin.StartupTimeout = 10000;
            brokerAdmin.StartBrokerApplication();
            AfterFixtureSetUp();
        }

        /// <summary>
        /// Fixtures the tear down.
        /// </summary>
        /// <remarks></remarks>
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            BeforeFixtureTearDown();
            var brokerAdmin = new RabbitBrokerAdmin();
            brokerAdmin.StopBrokerApplication();
            brokerAdmin.StopNode();
            AfterFixtureTearDown();
        }

        /// <summary>
        /// Befores the fixture set up.
        /// </summary>
        /// <remarks></remarks>
        public virtual void BeforeFixtureSetUp()
        {
        }

        /// <summary>
        /// Befores the fixture tear down.
        /// </summary>
        /// <remarks></remarks>
        public virtual void BeforeFixtureTearDown()
        {
        }

        /// <summary>
        /// Afters the fixture set up.
        /// </summary>
        /// <remarks></remarks>
        public virtual void AfterFixtureSetUp()
        {
        }

        /// <summary>
        /// Afters the fixture tear down.
        /// </summary>
        /// <remarks></remarks>
        public virtual void AfterFixtureTearDown()
        {
        }
    }
}
