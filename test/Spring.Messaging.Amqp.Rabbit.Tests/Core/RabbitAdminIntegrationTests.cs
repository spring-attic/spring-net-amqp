
using System;
using System.IO;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Impl;
using Spring.Context.Support;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Admin;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Test;
using Spring.Threading.AtomicTypes;
using IConnection = RabbitMQ.Client.IConnection;

namespace Spring.Messaging.Amqp.Rabbit.Core
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
        private CachingConnectionFactory connectionFactory = new CachingConnectionFactory();
        
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
        public RabbitAdminIntegrationTests()
        {
            this.connectionFactory.Port = BrokerTestUtils.GetPort();
        }

        #region Fixture Setup and Teardown
        /// <summary>
        /// Code to execute before fixture setup.
        /// </summary>
        public override void BeforeFixtureSetUp()
        {
        }

        /// <summary>
        /// Code to execute before fixture teardown.
        /// </summary>
        public override void BeforeFixtureTearDown()
        {
        }

        /// <summary>
        /// Code to execute after fixture setup.
        /// </summary>
        public override void AfterFixtureSetUp()
        {
            this.brokerIsRunning = BrokerRunning.IsRunning();
        }

        /// <summary>
        /// Code to execute after fixture teardown.
        /// </summary>
        public override void AfterFixtureTearDown()
        {
        }
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
        /// <remarks></remarks>
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
        /// <remarks></remarks>
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

        /// <summary>
        /// Tests the startup with autodelete.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestStartupWithAutodelete()
        {
            var queue = new Queue("test.queue", false, true, true);
            this.context.ObjectFactory.RegisterSingleton("foo", queue);
            this.rabbitAdmin.AfterPropertiesSet();

            var connectionHolder = new AtomicReference<IConnection>();

            var rabbitTemplate = new RabbitTemplate(this.connectionFactory);
            var exists = rabbitTemplate.Execute<bool>(delegate(IModel channel)
            {
                var result = channel.QueueDeclarePassive(queue.Name);
                connectionHolder.LazySet(((CachedModel)channel).Connection);
                return result != null;
            });
            Assert.True(exists, "Expected Queue to exist");

            Assert.True(this.QueueExists(connectionHolder.Value, queue));

            exists = rabbitTemplate.Execute<bool>(delegate(IModel channel)
            {
                var result = channel.QueueDeclarePassive(queue.Name);
                connectionHolder.LazySet(((CachedModel)channel).Connection);
                return result != null;
            });
            Assert.True(exists, "Expected Queue to exist");

            this.connectionFactory.Dispose();

            // Broker now deletes queue (only verifiable in native API)
            Assert.False(this.QueueExists(null, queue));

            // Broker auto-deleted queue, but it is re-created by the connection listener
            exists = rabbitTemplate.Execute<bool>(delegate(IModel channel)
            {
                var result = channel.QueueDeclarePassive(queue.Name);
                connectionHolder.LazySet(((CachedModel)channel).Connection);
                return result != null;
            });
            Assert.True(exists, "Expected Queue to exist");

            Assert.True(this.QueueExists(connectionHolder.Value, queue));
            Assert.True(this.rabbitAdmin.DeleteQueue(queue.Name));
            Assert.False(this.QueueExists(null, queue));

        }

        /// <summary>
        /// Tests the startup with non durable.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestStartupWithNonDurable()
        {
            var queue = new Queue("test.queue", false, false, false);
            this.context.ObjectFactory.RegisterSingleton("foo", queue);
            this.rabbitAdmin.AfterPropertiesSet();

            var connectionHolder = new AtomicReference<IConnection>();

            var rabbitTemplate = new RabbitTemplate(this.connectionFactory);

            // Force RabbitAdmin to initialize the queue
            var exists = rabbitTemplate.Execute<bool>(delegate(IModel channel)
            {
                var result = channel.QueueDeclarePassive(queue.Name);
                connectionHolder.LazySet(((CachedModel)channel).Connection);
                return result != null;
            });
            Assert.True(exists, "Expected Queue to exist");

            Assert.True(this.QueueExists(connectionHolder.Value, queue));

            // simulate broker going down and coming back up...
            this.rabbitAdmin.DeleteQueue(queue.Name);
            this.connectionFactory.Dispose();
            Assert.False(this.QueueExists(null, queue));

            // Broker auto-deleted queue, but it is re-created by the connection listener
            exists = rabbitTemplate.Execute<bool>(delegate(IModel channel)
            {
                var result = channel.QueueDeclarePassive(queue.Name);
                connectionHolder.LazySet(((CachedModel)channel).Connection);
                return result != null;
            });
            Assert.True(exists, "Expected Queue to exist");

            Assert.True(this.QueueExists(connectionHolder.Value, queue));
            Assert.True(this.rabbitAdmin.DeleteQueue(queue.Name));
            Assert.False(this.QueueExists(null, queue));
        }

        /// <summary>
        /// Queues the exists.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="queue">The queue.</param>
        /// <returns></returns>
        /// Use native Rabbit API to test queue, bypassing all the connection and channel caching and callbacks in Spring
        /// AMQP.
        /// @param connection the raw connection to use
        /// @param queue the Queue to test
        /// @return true if the queue exists
        /// <remarks></remarks>
        private bool QueueExists(IConnection connection, Queue queue)
        {
            var target = connection;
            if (target == null)
            {
                var connectionFactory = new ConnectionFactory();
                connectionFactory.Port = BrokerTestUtils.GetPort();
                target = connectionFactory.CreateConnection();
            }

            var channel = target.CreateModel();
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
                if (connection == null)
                {
                    target.Close();
                }
            }
        }
    }
}
