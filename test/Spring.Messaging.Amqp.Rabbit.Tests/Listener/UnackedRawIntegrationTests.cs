// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnackedRawIntegrationTests.cs" company="The original author or authors.">
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
using System.Net;
using System.Text;
using Common.Logging;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// Used to verify raw Rabbit .NET Client behaviour for corner cases.
    /// </summary>
    /// @author Dave Syer
    [TestFixture]
    [Category(TestCategory.Integration)]
    [Ignore("Ignored in the spring-amqp also. Initiated discussion with the rabbitmq folks to determine why publish/consume/reject/consume won't work as expected...")]
    public class UnackedRawIntegrationTests : AbstractRabbitIntegrationTest
    {
        private static new readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly ConnectionFactory factory = new ConnectionFactory();
        private IConnection conn;
        private IModel noTxChannel;
        private IModel txChannel;

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
                    Logger.Error("An error occurred closing the channel", e);
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
                    Logger.Error("An error occurred deleting the queue 'test.queue'", e);
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
