using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Spring.Messaging.Amqp.Rabbit.Test;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    using Common.Logging;

    /// <summary>
    /// Used to verify raw Rabbit .NET Client behaviour for corner cases.
    /// </summary>
    /// @author Dave Syer
    [TestFixture]
    [Category(TestCategory.Integration)]
    [Ignore("Ignored in the spring-amqp also. Initiated discussion with the rabbitmq folks to determine why publish/consume/reject/consume won't work as expected...")]
    public class UnackedRawIntegrationTests : AbstractRabbitIntegrationTest
    {
        private ILog logger = LogManager.GetCurrentClassLogger();

        private ConnectionFactory factory = new ConnectionFactory();
        private IConnection conn;
        private IModel noTxChannel;
        private IModel txChannel;

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
            this.factory.HostName = Dns.GetHostName().ToUpper();
            this.factory.Port = BrokerTestUtils.GetPort();
            this.conn = this.factory.CreateConnection();
            this.noTxChannel = this.conn.CreateModel();
            this.txChannel = this.conn.CreateModel();
            
            // Note: the Java client includes a convenience method with one int argument that sets the prefetchSize to 0 and global to false.
            this.txChannel.BasicQos(0, 1, false);
            this.txChannel.TxSelect();

            try
            {
                this.noTxChannel.QueueDelete("test.queue");
            }
            catch (Exception e)
            {
                this.noTxChannel = this.conn.CreateModel();
            }

            this.noTxChannel.QueueDeclare("test.queue", true, false, false, null);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        [TearDown]
        public void Clear()
        {
            if (this.txChannel != null)
            {
                try
                {
                    this.txChannel.Close();
                }
                catch (Exception e)
                {
                    logger.Error("An error occurred closing the channel", e);
                }
            }

            if (this.noTxChannel != null)
            {
                try
                {
                    this.noTxChannel.QueueDelete("test.queue");
                }
                catch (Exception e)
                {
                    logger.Error("An error occurred deleting the queue 'test.queue'", e);
                }

                this.noTxChannel.Close();
            }

            this.conn.Close();
        }

        /// <summary>
        /// Tests the one publish unacked requeued.
        /// </summary>
        [Test]
        public void TestOnePublishUnackedRequeued()
        {
            // TODO
            this.noTxChannel.BasicPublish(string.Empty, "test.queue", null, Encoding.UTF8.GetBytes("foo"));

            var callback = new QueueingBasicConsumer(this.txChannel);
            this.txChannel.BasicConsume("test.queue", false, callback);
            object next;
            callback.Queue.Dequeue(1000, out next);
            Assert.IsNotNull(next);
            this.txChannel.BasicReject(((BasicDeliverEventArgs)next).DeliveryTag, true);
            this.txChannel.TxCommit();
            
            var get = this.noTxChannel.BasicGet("test.queue", true);
            Assert.IsNotNull(get);
        }

        /// <summary>
        /// Tests the four publish unacked requeued.
        /// </summary>
        [Test]
        public void TestFourPublishUnackedRequeued()
        {
            this.noTxChannel.BasicPublish(string.Empty, "test.queue", null, Encoding.UTF8.GetBytes("foo"));
            this.noTxChannel.BasicPublish(string.Empty, "test.queue", null, Encoding.UTF8.GetBytes("bar"));
            this.noTxChannel.BasicPublish(string.Empty, "test.queue", null, Encoding.UTF8.GetBytes("one"));
            this.noTxChannel.BasicPublish(string.Empty, "test.queue", null, Encoding.UTF8.GetBytes("two"));

            var callback = new QueueingBasicConsumer(this.txChannel);
            this.txChannel.BasicConsume("test.queue", false, callback);
            object next;
            callback.Queue.Dequeue(1000, out next);
            Assert.IsNotNull(next);
            this.txChannel.BasicReject(((BasicDeliverEventArgs)next).DeliveryTag, true);
            this.txChannel.TxRollback();

            var get = this.noTxChannel.BasicGet("test.queue", true);
            Assert.IsNotNull(get);
        }
    }
}
