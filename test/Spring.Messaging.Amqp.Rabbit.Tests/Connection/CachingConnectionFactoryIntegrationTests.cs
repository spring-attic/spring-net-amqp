using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Spring.Messaging.Amqp.Rabbit.Admin;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Test;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// Caching connection factory integration tests.
    /// </summary>
    /// <remarks></remarks>
    [TestFixture]
    public class CachingConnectionFactoryIntegrationTests
    {
        /// <summary>
        /// The connection factory.
        /// </summary>
        private CachingConnectionFactory connectionFactory = new CachingConnectionFactory();

        /// <summary>
        /// The broker is running.
        /// </summary>
        public BrokerRunning brokerIsRunning;
        
        // public ExpectedException exception = ExpectedException.none();

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
            //this.brokerAdmin = new RabbitBrokerAdmin();
            //this.brokerAdmin.StartBrokerApplication();
            // this.brokerIsRunning = BrokerRunning.IsRunning();
            this.connectionFactory.Port = BrokerTestUtils.GetPort();
        }

        /// <summary>
        /// Tears down.
        /// </summary>
        /// <remarks></remarks>
        [TearDown]
        public void TearDown()
        {
            //this.brokerAdmin.StopBrokerApplication();

            // Release resources
            this.connectionFactory.Reset();
        }

        /// <summary>
        /// Tests the send and receive from volatile queue.
        /// </summary>
        /// <remarks></remarks>
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
        /// <remarks></remarks>
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
        /// <remarks></remarks>
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
    }
}
