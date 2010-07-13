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


using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Rabbit.Connection;

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    [TestFixture]
    public class RabbitBrokerAdminIntegrationTests
    {
        protected CachingConnectionFactory connectionFactory;

        private RabbitBrokerAdmin brokerAdmin;
        [SetUp]
        public void SetUp()
        {
            ConnectionParameters parameters = new ConnectionParameters();
            parameters.UserName = ConnectionParameters.DefaultUser;
            parameters.Password = ConnectionParameters.DefaultPass;
            parameters.VirtualHost = ConnectionParameters.DefaultVHost;            
            connectionFactory = new CachingConnectionFactory(parameters);
            brokerAdmin = new RabbitBrokerAdmin(connectionFactory);
        }
        

        [Test]
        public void TestStatus()
        {
        }
    }
}