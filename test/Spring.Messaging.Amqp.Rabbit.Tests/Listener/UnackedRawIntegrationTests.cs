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
    /// <summary>
    /// Used to verify raw Rabbit .NET Client behaviour for corner cases.
    /// </summary>
    /// @author Dave Syer
    /// <remarks></remarks>
    public class UnackedRawIntegrationTests : AbstractRabbitIntegrationTest
    {
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
        /// <remarks></remarks>
        [SetUp]
        public void Init()
        {
            this.factory.HostName = Dns.GetHostName().ToUpper();
            this.factory.Port = BrokerTestUtils.GetPort();
            this.conn = this.factory.CreateConnection();
            this.noTxChannel = this.conn.CreateModel();
            this.txChannel = this.conn.CreateModel();

            // TODO: this should be: this.txChannel.BasicQos(1, 0, false);
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
        /// <remarks></remarks>
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
                    Console.Write(e.StackTrace);
                }
            }

            if (this.noTxChannel != null)
            {
                try
                {
                    try
                    {
                        this.noTxChannel.QueueDelete("test.queue");
                    }
                    catch (Exception e)
                    {
                    }

                    this.noTxChannel.Close();
                }
                catch (Exception e)
                {
                    Console.Write(e.StackTrace);
                }
            }

            this.conn.Close();
        }

        /// <summary>
        /// Tests the one publish unacked requeued.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestOnePublishUnackedRequeued()
        {
            this.noTxChannel.BasicPublish(string.Empty, "test.queue", null, Encoding.UTF8.GetBytes("foo"));

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

        /// <summary>
        /// Tests the four publish unacked requeued.
        /// </summary>
        /// <remarks></remarks>
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
