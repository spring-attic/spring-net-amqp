// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitAdminIntegrationTests.cs" company="The original author or authors.">
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
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Context.Support;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core
{
    /// <summary>
    /// Rabbit admin integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class RabbitAdminIntegrationTests : AbstractRabbitIntegrationTest
    {
        /// <summary>
        /// The connection factory.
        /// </summary>
        private readonly CachingConnectionFactory connectionFactory = new CachingConnectionFactory();

        /// <summary>
        /// The context.
        /// </summary>
        private GenericApplicationContext context;

        /// <summary>
        /// The rabbit admin field.
        /// </summary>
        private RabbitAdmin rabbitAdmin;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitAdminIntegrationTests"/> class. 
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public RabbitAdminIntegrationTests() { this.connectionFactory.Port = BrokerTestUtils.GetPort(); }

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
        public override void AfterFixtureSetUp() { this.brokerIsRunning = BrokerRunning.IsRunning(); }

        /// <summary>
        /// Code to execute after fixture teardown.
        /// </summary>
        public override void AfterFixtureTearDown() { }
        #endregion

        /// <summary>
        /// Inits this instance.
        /// </summary>
        [SetUp]
        public void Init()
        {
            this.context = new GenericApplicationContext();
            this.rabbitAdmin = new RabbitAdmin(this.connectionFactory);

            this.rabbitAdmin.DeleteQueue("test.queue");

            // Force connection factory to forget that it has been used to delete the queue
            this.connectionFactory.Dispose();
            this.rabbitAdmin.ApplicationContext = this.context;
            this.rabbitAdmin.AutoStartup = true;
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        [TearDown]
        public void Close()
        {
            if (this.context != null)
            {
                this.context.Dispose();
            }

            if (this.connectionFactory != null)
            {
                this.connectionFactory.Dispose();
            }
        }

        /// <summary>
        /// Tests the startup with lazy declaration.
        /// </summary>
        [Test]
        public void TestStartupWithLazyDeclaration()
        {
            var queue = new Queue("test.queue");
            this.context.ObjectFactory.RegisterSingleton("foo", queue);
            this.rabbitAdmin.AfterPropertiesSet();

            // A new connection is initialized so the queue is declared
            Assert.True(this.rabbitAdmin.DeleteQueue(queue.Name));
        }

        /// <summary>
        /// Tests the double declaration of exclusive queue.
        /// </summary>
        [Test]
        public void TestDoubleDeclarationOfExclusiveQueue()
        {
            // Expect exception because the queue is locked when it is declared a second time.
            var connectionFactory1 = new CachingConnectionFactory();
            connectionFactory1.Port = BrokerTestUtils.GetPort();
            var connectionFactory2 = new CachingConnectionFactory();
            connectionFactory2.Port = BrokerTestUtils.GetPort();
            var queue = new Queue("test.queue", false, true, true);
            this.rabbitAdmin.DeleteQueue(queue.Name);
            new RabbitAdmin(connectionFactory1).DeclareQueue(queue);
            try
            {
                new RabbitAdmin(connectionFactory2).DeclareQueue(queue);
                Assert.Fail("Expected an exception, and one was not thrown.");
            }
            catch (Exception ex)
            {
                Assert.True(ex is AmqpIOException, "Expecting an AmqpIOException");
            }
            finally
            {
                // Need to release the connection so the exclusive queue is deleted
                connectionFactory1.Dispose();
            }
        }

        /// <summary>
        /// Tests the double declaration of autodelete queue.
        /// </summary>
        [Test]
        public void TestDoubleDeclarationOfAutodeleteQueue()
        {
            // No error expected here: the queue is autodeleted when the last consumer is cancelled, but this one never has
            // any consumers.
            var connectionFactory1 = new CachingConnectionFactory();
            connectionFactory1.Port = BrokerTestUtils.GetPort();
            var connectionFactory2 = new CachingConnectionFactory();
            connectionFactory2.Port = BrokerTestUtils.GetPort();
            var queue = new Queue("test.queue", false, false, true);
            new RabbitAdmin(connectionFactory1).DeclareQueue(queue);
            new RabbitAdmin(connectionFactory2).DeclareQueue(queue);
            connectionFactory1.Dispose();
            connectionFactory2.Dispose();
        }

        /// <summary>The test queue with auto delete.</summary>
        [Test]
        public void TestQueueWithAutoDelete()
        {
            var queue = new Queue("test.queue", false, true, true);
            this.context.ObjectFactory.RegisterSingleton("foo", queue);
            this.rabbitAdmin.AfterPropertiesSet();

            // Queue created on spring startup
            this.rabbitAdmin.Initialize();
            Assert.True(this.QueueExists(queue));

            // Stop and broker deletes queue (only verifiable in native API)
            this.connectionFactory.Dispose();
            Assert.False(this.QueueExists(queue));

            // Start and queue re-created by the connection listener
            this.connectionFactory.CreateConnection();
            Assert.True(this.QueueExists(queue));

            // Queue manually deleted
            Assert.True(this.rabbitAdmin.DeleteQueue(queue.Name));
            Assert.False(this.QueueExists(queue));
        }

        /// <summary>The test queue without auto delete.</summary>
        [Test]
        public void TestQueueWithoutAutoDelete()
        {
            var queue = new Queue("test.queue", false, false, false);
            this.context.ObjectFactory.RegisterSingleton("foo", queue);
            this.rabbitAdmin.AfterPropertiesSet();

            // Queue created on Spring startup
            this.rabbitAdmin.Initialize();
            Assert.True(this.QueueExists(queue));

            // Stop and broker retains queue (only verifiable in native API)
            this.connectionFactory.Dispose();
            Assert.True(this.QueueExists(queue));

            // Start and queue still exists
            this.connectionFactory.CreateConnection();
            Assert.True(this.QueueExists(queue));

            // Queue manually deleted
            Assert.True(this.rabbitAdmin.DeleteQueue(queue.Name));
            Assert.False(this.QueueExists(queue));
        }

        /// <summary>The test declare exchange with default exchange.</summary>
        [Test]
        public void TestDeclareExchangeWithDefaultExchange()
        {
            var exchange = new DirectExchange(RabbitAdmin.DEFAULT_EXCHANGE_NAME);

            this.rabbitAdmin.DeclareExchange(exchange);

            // Pass by virtue of RabbitMQ not firing a 403 reply code
        }

        /// <summary>The test spring with default exchange.</summary>
        [Test]
        public void TestSpringWithDefaultExchange()
        {
            var exchange = new DirectExchange(RabbitAdmin.DEFAULT_EXCHANGE_NAME);
            this.context.ObjectFactory.RegisterSingleton("foo", exchange);
            this.rabbitAdmin.AfterPropertiesSet();

            this.rabbitAdmin.Initialize();

            // Pass by virtue of RabbitMQ not firing a 403 reply code
        }

        /// <summary>The test delete exchange with default exchange.</summary>
        [Test]
        public void TestDeleteExchangeWithDefaultExchange()
        {
            var result = this.rabbitAdmin.DeleteExchange(RabbitAdmin.DEFAULT_EXCHANGE_NAME);

            Assert.True(result);
        }

        /// <summary>The test declare binding with default exchange implicit binding.</summary>
        [Test]
        public void TestDeclareBindingWithDefaultExchangeImplicitBinding()
        {
            var exchange = new DirectExchange(RabbitAdmin.DEFAULT_EXCHANGE_NAME);
            var queueName = "test.queue";
            var queue = new Queue(queueName, false, false, false);
            this.rabbitAdmin.DeclareQueue(queue);
            var binding = new Binding(queueName, Binding.DestinationType.Queue, exchange.Name, queueName, null);

            this.rabbitAdmin.DeclareBinding(binding);

            // Pass by virtue of RabbitMQ not firing a 403 reply code for both exchange and binding declaration
            Assert.True(this.QueueExists(queue));
        }

        /// <summary>The test spring with default exchange implicit binding.</summary>
        [Test]
        public void TestSpringWithDefaultExchangeImplicitBinding()
        {
            var exchange = new DirectExchange(RabbitAdmin.DEFAULT_EXCHANGE_NAME);
            this.context.ObjectFactory.RegisterSingleton("foo", exchange);
            var queueName = "test.queue";
            var queue = new Queue(queueName, false, false, false);
            this.context.ObjectFactory.RegisterSingleton("bar", queue);
            var binding = new Binding(queueName, Binding.DestinationType.Queue, exchange.Name, queueName, null);
            this.context.ObjectFactory.RegisterSingleton("baz", binding);
            this.rabbitAdmin.AfterPropertiesSet();

            this.rabbitAdmin.Initialize();

            // Pass by virtue of RabbitMQ not firing a 403 reply code for both exchange and binding declaration
            Assert.True(this.QueueExists(queue));
        }

        /// <summary>The test remove binding with default exchange implicit binding.</summary>
        [Test]
        public void TestRemoveBindingWithDefaultExchangeImplicitBinding()
        {
            var queueName = "test.queue";
            var queue = new Queue(queueName, false, false, false);
            this.rabbitAdmin.DeclareQueue(queue);
            var binding = new Binding(queueName, Binding.DestinationType.Queue, RabbitAdmin.DEFAULT_EXCHANGE_NAME, queueName, null);

            this.rabbitAdmin.RemoveBinding(binding);

            // Pass by virtue of RabbitMQ not firing a 403 reply code
        }

        /// <summary>The test declare binding with default exchange non implicit binding.</summary>
        [Test]
        public void TestDeclareBindingWithDefaultExchangeNonImplicitBinding()
        {
            var exchange = new DirectExchange(RabbitAdmin.DEFAULT_EXCHANGE_NAME);
            var queueName = "test.queue";
            var queue = new Queue(queueName, false, false, false);
            this.rabbitAdmin.DeclareQueue(queue);
            var binding = new Binding(queueName, Binding.DestinationType.Queue, exchange.Name, "test.routingKey", null);

            try
            {
                this.rabbitAdmin.DeclareBinding(binding);
            }
            catch (AmqpIOException ex)
            {
                Exception cause = ex;
                Exception rootCause = null;
                while (cause != null)
                {
                    rootCause = cause;
                    cause = cause.InnerException;
                }

                Assert.True(rootCause.Message.Contains("code=403"));
                Assert.True(rootCause.Message.Contains("operation not permitted on the default exchange"));
            }
        }

        /// <summary>The test spring with default exchange non implicit binding.</summary>
        [Test]
        public void TestSpringWithDefaultExchangeNonImplicitBinding()
        {
            var exchange = new DirectExchange(RabbitAdmin.DEFAULT_EXCHANGE_NAME);
            this.context.ObjectFactory.RegisterSingleton("foo", exchange);
            var queueName = "test.queue";
            var queue = new Queue(queueName, false, false, false);
            this.context.ObjectFactory.RegisterSingleton("bar", queue);
            var binding = new Binding(queueName, Binding.DestinationType.Queue, exchange.Name, "test.routingKey", null);
            this.context.ObjectFactory.RegisterSingleton("baz", binding);
            this.rabbitAdmin.AfterPropertiesSet();

            try
            {
                this.rabbitAdmin.DeclareBinding(binding);
            }
            catch (AmqpIOException ex)
            {
                Exception cause = ex;
                Exception rootCause = null;
                while (cause != null)
                {
                    rootCause = cause;
                    cause = cause.InnerException;
                }

                Assert.True(rootCause.Message.Contains("code=403"));
                Assert.True(rootCause.Message.Contains("operation not permitted on the default exchange"));
            }
        }

        /// <summary>Queues the exists.</summary>
        /// <param name="queue">The queue.</param>
        /// <returns>The System.Boolean.</returns>
        /// Use native Rabbit API to test queue, bypassing all the connection and channel caching and callbacks in Spring
        /// AMQP.
        /// @param connection the raw connection to use
        /// @param queue the Queue to test
        /// @return true if the queue exists
        private bool QueueExists(Queue queue)
        {
            var connectionFactory = new ConnectionFactory();
            connectionFactory.Port = BrokerTestUtils.GetPort();
            var connection = connectionFactory.CreateConnection();
            var channel = connection.CreateModel();

            try
            {
                var result = channel.QueueDeclarePassive(queue.Name);
                return result != null;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("RESOURCE_LOCKED"))
                {
                    return true;
                }

                return false;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
