// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitBrokerAdminIntegrationTests.cs" company="The original author or authors.">
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
using System.Collections.Generic;
using System.Threading;
using Common.Logging;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>The rabbit broker admin integration tests.</summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class RabbitBrokerAdminIntegrationTests
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The connection factory.
        /// </summary>
        protected AbstractConnectionFactory connectionFactory;

        /// <summary>
        /// The broker admin.
        /// </summary>
        private RabbitBrokerAdmin brokerAdmin;

        /// <summary>
        /// Determines whether the environment is available.
        /// </summary>
        public static EnvironmentAvailable environment = new EnvironmentAvailable("BROKER_INTEGRATION_TEST");

        /// <summary>
        /// Sets up.
        /// </summary>
        [TestFixtureSetUp]
        public void SetUp()
        {
            try
            {
                if (environment.IsActive())
                {
                    // Set up broker admin for non-root user
                    this.brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(); // "rabbit@LOCALHOST", 5672);
                    this.brokerAdmin.StartNode();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred during SetUp", ex);
                Assert.Fail("An error occurred during SetUp.");
            }
        }

        /// <summary>
        /// Tears down.
        /// </summary>
        [TestFixtureTearDown]
        public void TearDown()
        {
            if (environment.IsActive())
            {
                // after tests, broker often remains in an inconsistent state and unable to accept new connections;
                // by stopping the running broker after these tests are complete, we ensure add'l tests will function properly
                this.brokerAdmin.StopNode();
            }
        }

        /// <summary>
        /// Users the crud.
        /// </summary>
        [Test]
        public void UserCrud()
        {
            var users = this.brokerAdmin.ListUsers();
            if (users.Contains("joe"))
            {
                this.brokerAdmin.DeleteUser("joe");
            }

            Thread.Sleep(200);
            this.brokerAdmin.AddUser("joe", "trader");
            Thread.Sleep(200);
            this.brokerAdmin.ChangeUserPassword("joe", "sales");
            Thread.Sleep(200);
            users = this.brokerAdmin.ListUsers();
            if (users.Contains("joe"))
            {
                Thread.Sleep(200);
                this.brokerAdmin.DeleteUser("joe");
            }
        }

        /// <summary>
        /// Integrations the tests user crud with module adapter.
        /// </summary>
        [Test]
        public void IntegrationTestsUserCrudWithModuleAdapter()
        {
            try
            {
                var adapter = new Dictionary<string, string>();

                // Switch two functions with identical inputs!
                adapter.Add("rabbit_auth_backend_internal%add_user", "rabbit_auth_backend_internal%change_password");
                adapter.Add("rabbit_auth_backend_internal%change_password", "rabbit_auth_backend_internal%add_user");
                this.brokerAdmin.ModuleAdapter = adapter;

                var users = this.brokerAdmin.ListUsers();
                if (users.Contains("joe"))
                {
                    this.brokerAdmin.DeleteUser("joe");
                }

                Thread.Sleep(1000);
                this.brokerAdmin.ChangeUserPassword("joe", "sales");
                Thread.Sleep(1000);
                this.brokerAdmin.AddUser("joe", "trader");
                Thread.Sleep(1000);
                users = this.brokerAdmin.ListUsers();
                if (users.Contains("joe"))
                {
                    Thread.Sleep(1000);
                    this.brokerAdmin.DeleteUser("joe");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred", ex);
                throw;
            }
            finally
            {
                // Need to ensure that we reset the module adapter that we swizzled with above, otherwise our other tests will be unreliable.
                this.brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin();
            }
        }

        /// <summary>The test get empty queues.</summary>
        [Test]
        public void TestGetEmptyQueues()
        {
            var queues = this.brokerAdmin.GetQueues();
            Assert.AreEqual(0, queues.Count);
        }

        /// <summary>The test get queues.</summary>
        [Test]
        public void TestGetQueues()
        {
            AbstractConnectionFactory connectionFactory = new SingleConnectionFactory();
            connectionFactory.Port = BrokerTestUtils.GetAdminPort();
            Queue queue = new RabbitAdmin(connectionFactory).DeclareQueue();
            Assert.AreEqual("/", connectionFactory.VirtualHost);
            List<QueueInfo> queues = this.brokerAdmin.GetQueues();
            Assert.AreEqual(queue.Name, queues[0].Name);
        }
    }
}
