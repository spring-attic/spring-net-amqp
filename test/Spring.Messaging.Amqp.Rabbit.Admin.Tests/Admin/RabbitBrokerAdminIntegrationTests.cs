#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using log4net.Core;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Test;

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    [TestFixture]
    public class RabbitBrokerAdminIntegrationTests
    {
        /// <summary>
        /// The connection factory.
        /// </summary>
        protected SingleConnectionFactory connectionFactory;

        /// <summary>
        /// The broker admin.
        /// </summary>
        private RabbitBrokerAdmin brokerAdmin;


        /// <summary>
        /// Sets up.
        /// </summary>
        /// <remarks></remarks>
        [SetUp]
        public void SetUp()
        {
            //if (environment.isActive())
            //{
            // Set up broker admin for non-root user
            this.brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(); //"rabbit@LOCALHOST", 5672);
            this.brokerAdmin.StartNode();
            //panic.setBrokerAdmin(brokerAdmin);
            // }
        }

        /// <summary>
        /// Tears down.
        /// </summary>
        /// <remarks></remarks>
        [TearDown]
        public void TearDown()
        {
            this.brokerAdmin.StopNode();
        }

        /// <summary>
        /// Users the crud.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void UserCrud()
        {
            List<String> users = this.brokerAdmin.ListUsers();
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
        /// <remarks></remarks>
        [Test]
        public void IntegrationTestsUserCrudWithModuleAdapter()
        {

            var adapter = new Dictionary<string, string>();
            // Switch two functions with identical inputs!
            adapter.Add("rabbit_auth_backend_internal%add_user", "rabbit_auth_backend_internal%change_password");
            adapter.Add("rabbit_auth_backend_internal%change_password", "rabbit_auth_backend_internal%add_user");
            brokerAdmin.ModuleAdapter = adapter;

            var users = brokerAdmin.ListUsers();
            if (users.Contains("joe"))
            {
                brokerAdmin.DeleteUser("joe");
            }
            Thread.Sleep(200);
            brokerAdmin.ChangeUserPassword("joe", "sales");
            Thread.Sleep(200);
            brokerAdmin.AddUser("joe", "trader");
            Thread.Sleep(200);
            users = brokerAdmin.ListUsers();
            if (users.Contains("joe"))
            {
                Thread.Sleep(200);
                brokerAdmin.DeleteUser("joe");
            }

        }

        [Test]
        public void TestGetEmptyQueues()
        {
            List<QueueInfo> queues = brokerAdmin.GetQueues();
            Assert.AreEqual(0, queues.Count);
        }

        [Test]
        public void TestGetQueues()
        {
            SingleConnectionFactory connectionFactory = new SingleConnectionFactory();
            connectionFactory.Port = BrokerTestUtils.GetAdminPort();
            Queue queue = new RabbitAdmin(connectionFactory).DeclareQueue();
            Assert.AreEqual("/", connectionFactory.VirtualHost);
            List<QueueInfo> queues = brokerAdmin.GetQueues();
            Assert.AreEqual(queue.Name, queues[0].Name);
        }
    }
}