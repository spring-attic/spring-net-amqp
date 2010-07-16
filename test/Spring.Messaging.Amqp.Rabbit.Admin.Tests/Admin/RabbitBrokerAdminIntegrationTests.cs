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
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    [TestFixture]
    public class RabbitBrokerAdminIntegrationTests
    {
        protected SingleConnectionFactory connectionFactory;

        private RabbitBrokerAdmin brokerAdmin;

        [SetUp]
        public void SetUp()
        {                      
            connectionFactory = new SingleConnectionFactory();
            brokerAdmin = new RabbitBrokerAdmin(connectionFactory);
        }

        [Test]
        public void UserCrud()
        {
            IList<string> users = brokerAdmin.ListUsers();
            if (users.Contains("joe"))
            {
                brokerAdmin.DeleteUser("joe");
            }
            brokerAdmin.AddUser("joe", "trader");
            users = brokerAdmin.ListUsers();
            Assert.AreEqual("guest", users[0]);
            Assert.AreEqual("joe", users[1]);
            brokerAdmin.ChangeUserPassword("joe", "sales");
            users = brokerAdmin.ListUsers();
            if (users.Contains("joe"))
            {
                brokerAdmin.DeleteUser("joe");
            }
        }
        

        [Test]
        public void TestStatus()
        {
            RabbitStatus status = brokerAdmin.Status;
            AssertBrokerAppRunning(status);

        }

        [Test]
        public void GetQueues()
        {
            brokerAdmin.DeclareQueue(new Queue("test.queue"));
            IList<QueueInfo> queues = brokerAdmin.Queues;
            Assert.AreEqual("test.queue", queues[0].Name);
            Console.WriteLine(queues[0]);

        }

        private void AssertBrokerAppRunning(RabbitStatus status)
        {
            Assert.AreEqual(status.RunningNodes.Count, 1);
            Assert.IsTrue(status.RunningNodes[0].Name.Contains("rabbit"));
        }


    }
}