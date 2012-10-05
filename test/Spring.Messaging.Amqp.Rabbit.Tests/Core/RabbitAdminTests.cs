// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitAdminTests.cs" company="The original author or authors.">
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
using System;
using NUnit.Framework;
using Spring.Context.Support;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core
{
    /// <summary>
    /// Rabbit admin tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class RabbitAdminTests
    {
        /// <summary>
        /// Tests the setting of null rabbit template.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSettingOfNullRabbitTemplate()
        {
            IConnectionFactory connectionFactory = null;
            try
            {
                new RabbitAdmin(connectionFactory);
                Assert.Fail("should have thrown ArgumentException when RabbitTemplate is not set.");
            }
            catch (Exception e)
            {
                Assert.True(e is ArgumentException, "Expecting an ArgumentException");
            }
        }

        /// <summary>
        /// Tests the no fail on startup with missing broker.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestNoFailOnStartupWithMissingBroker()
        {
            var connectionFactory = new SingleConnectionFactory("foo");
            connectionFactory.Port = 434343;
            var applicationContext = new GenericApplicationContext();
            applicationContext.ObjectFactory.RegisterSingleton("foo", new Queue("queue"));
            var rabbitAdmin = new RabbitAdmin(connectionFactory);
            rabbitAdmin.ApplicationContext = applicationContext;
            rabbitAdmin.AutoStartup = true;
            rabbitAdmin.AfterPropertiesSet();
        }

        /// <summary>
        /// Tests the fail on first use with missing broker.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestFailOnFirstUseWithMissingBroker()
        {
            var connectionFactory = new SingleConnectionFactory("foo");
            connectionFactory.Port = 434343;
            var applicationContext = new GenericApplicationContext();
            applicationContext.ObjectFactory.RegisterSingleton("foo", new Queue("queue"));
            var rabbitAdmin = new RabbitAdmin(connectionFactory);
            rabbitAdmin.ApplicationContext = applicationContext;
            rabbitAdmin.AutoStartup = true;
            rabbitAdmin.AfterPropertiesSet();

            try
            {
                rabbitAdmin.DeclareQueue();
            }
            catch (Exception ex)
            {
                // TODO: Should this be an ArgumentException instead of an AmqpIOException??
                // Assert.True(ex is ArgumentException, "Expecting an ArgumentException");
                Assert.True(ex is AmqpIOException, "Expecting an AmqpIOException");
            }
        }
    }
}
