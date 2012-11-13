// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CachingConnectionFactoryIntegrationTests.cs" company="The original author or authors.">
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
using System.Threading;
using Common.Logging;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Admin;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Connection
{
    /// <summary>
    /// Caching connection factory integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class CachingConnectionFactoryIntegrationTests : AbstractRabbitIntegrationTest
    {
        private new static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The connection factory.
        /// </summary>
        private CachingConnectionFactory connectionFactory;

        #region Fixture Setup and Teardown

        /// <summary>
        /// Code to execute before fixture setup.
        /// </summary>
        public override void BeforeFixtureSetUp() { }

        /// <summary>
        /// Code to execute before fixture teardown.
        /// </summary>
        public override void BeforeFixtureTearDown() { }

        /// <summary>
        /// Code to execute after fixture setup.
        /// </summary>
        public override void AfterFixtureSetUp() { }

        /// <summary>
        /// Code to execute after fixture teardown.
        /// </summary>
        public override void AfterFixtureTearDown() { }
        #endregion

        // public ExpectedException exception = ExpectedException.none();

        /// <summary>
        /// The broker admin.
        /// </summary>
        private RabbitBrokerAdmin brokerAdmin;

        /// <summary>
        /// Sets up.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.connectionFactory = new CachingConnectionFactory();
            this.brokerIsRunning = BrokerRunning.IsRunning();
            this.connectionFactory.Port = BrokerTestUtils.GetPort();
        }

        /// <summary>
        /// Tears down.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            // Release resources
            this.brokerIsRunning = null;
            this.connectionFactory.Dispose();
        }

        /// <summary>
        /// Tests the send and receive from volatile queue.
        /// </summary>
        [Test]
        public void TestSendAndReceiveFromVolatileQueue()
        {
            var template = new RabbitTemplate(this.connectionFactory);

            var admin = new RabbitAdmin(this.connectionFactory);
            var queue = admin.DeclareQueue();
            template.ConvertAndSend(queue.Name, "message");
            var result = (string)template.ReceiveAndConvert(queue.Name);
            Assert.AreEqual("message", result);
        }

        /// <summary>
        /// Tests the receive from non existent virtual host.
        /// </summary>
        [Test]
        public void TestReceiveFromNonExistentVirtualHost()
        {
            this.connectionFactory.VirtualHost = "non-existent";
            var template = new RabbitTemplate(this.connectionFactory);

            // Wrong vhost is very unfriendly to client - the exception has no clue (just an EOF)

            // exception.expect(AmqpIOException.class);
            try
            {
                var result = (string)template.ReceiveAndConvert("foo");
                Assert.AreEqual("message", result);
            }
            catch (Exception e)
            {
                Assert.True(e is AmqpIOException);
            }
        }

        /// <summary>
        /// Tests the send and receive from volatile queue after implicit removal.
        /// </summary>
        [Test]
        public void TestSendAndReceiveFromVolatileQueueAfterImplicitRemoval()
        {
            var template = new RabbitTemplate(this.connectionFactory);

            var admin = new RabbitAdmin(this.connectionFactory);
            var queue = admin.DeclareQueue();
            template.ConvertAndSend(queue.Name, "message");

            // Force a physical close of the channel
            this.connectionFactory.Dispose();

            try
            {
                var result = (string)template.ReceiveAndConvert(queue.Name);
                Assert.AreEqual("message", result);
            }
            catch (Exception e)
            {
                Assert.True(e is AmqpIOException);
            }
        }

        /// <summary>
        /// Tests the mix transactional and non transactional.
        /// </summary>
        [Test]
        public void TestMixTransactionalAndNonTransactional()
        {
            var template1 = new RabbitTemplate(this.connectionFactory);
            var template2 = new RabbitTemplate(this.connectionFactory);
            template1.ChannelTransacted = true;

            var admin = new RabbitAdmin(this.connectionFactory);
            var queue = admin.DeclareQueue();

            template1.ConvertAndSend(queue.Name, "message");

            var result = (string)template2.ReceiveAndConvert(queue.Name);
            Assert.AreEqual("message", result);

            try
            {
                template2.Execute<object>(
                    delegate(IModel channel)
                    {
                        // Should be an exception because the channel is not transactional
                        channel.TxRollback();
                        return null;
                    });
            }
            catch (Exception ex)
            {
                Assert.True(ex is AmqpIOException, "The channel is not transactional.");
            }
        }

        /// <summary>The test hard error and reconnect.</summary>
        /// <exception cref="SystemException"></exception>
        [Test]
        public void TestHardErrorAndReconnect()
        {
            var template = new RabbitTemplate(this.connectionFactory);
            var admin = new RabbitAdmin(this.connectionFactory);
            var queue = new Queue("foo");
            admin.DeclareQueue(queue);
            var route = queue.Name;

            var latch = new CountdownEvent(1);
            try
            {
                template.Execute<object>(
                    (IModel channel) =>
                    {
                        ((IChannelProxy)channel).GetConnection().ConnectionShutdown += delegate
                        {
                            Logger.Info("Error");
                            if (latch.CurrentCount > 0)
                            {
                                latch.Signal();
                            }

                            // This will be thrown on the Connection thread just before it dies, so basically ignored
                            throw new SystemException();
                        };

                        var internalTag = channel.BasicConsume(route, false, new DefaultBasicConsumer(channel));

                        // Consume twice with the same tag is a hard error (connection will be reset)
                        var internalResult = channel.BasicConsume(route, false, internalTag, new DefaultBasicConsumer(channel));
                        Assert.Fail("Expected IOException, got: " + internalResult);
                        return null;
                    });

                Assert.Fail("Expected AmqpIOException");
            }
            catch (AmqpIOException e)
            {
                // expected
            }

            template.ConvertAndSend(route, "message");
            Assert.True(latch.Wait(1000));
            var result = (string)template.ReceiveAndConvert(route);
            Assert.AreEqual("message", result);
            result = (string)template.ReceiveAndConvert(route);
            Assert.AreEqual(null, result);
        }
    }
}
