// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExternalTxManagerTests.cs" company="The original author or authors.">
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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.v0_9_1;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using Spring.Transaction;
using Spring.Transaction.Support;
using IConnection = RabbitMQ.Client.IConnection;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class ExternalTxManagerTests
    {
        /// <summary>Verifies that an up-stack RabbitTemplate uses the listener's channel (MessageListener).</summary>
        [Test]
        public void TestMessageListener()
        {
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            var onlyChannel = new Mock<IModel>();
            onlyChannel.Setup(m => m.IsOpen).Returns(true);
            onlyChannel.Setup(m => m.CreateBasicProperties()).Returns(() => new BasicProperties());

            var cachingConnectionFactory = new CachingConnectionFactory(mockConnectionFactory.Object);

            mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(m => m.IsOpen).Returns(true);

            var tooManyChannels = new AtomicReference<Exception>();
            var done = false;
            mockConnection.Setup(m => m.CreateModel()).Returns(
                () =>
                {
                    if (!done)
                    {
                        done = true;
                        return onlyChannel.Object;
                    }

                    tooManyChannels.LazySet(new Exception("More than one channel requested"));
                    var channel = new Mock<IModel>();
                    channel.Setup(m => m.IsOpen).Returns(true);
                    return channel.Object;
                });

            var consumer = new AtomicReference<IBasicConsumer>();

            onlyChannel.Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicConsumer>())).Callback<string, bool, IBasicConsumer>(
                (a1, a2, a3) => consumer.LazySet(a3));

            var commitLatch = new CountdownEvent(1);
            onlyChannel.Setup(m => m.TxCommit()).Callback(() => commitLatch.Signal());

            var latch = new CountdownEvent(1);
            var container = new SimpleMessageListenerContainer(cachingConnectionFactory);
            container.MessageListener = new Action<Message>(
                message =>
                {
                    var rabbitTemplate = new RabbitTemplate(cachingConnectionFactory);
                    rabbitTemplate.ChannelTransacted = true;

                    // should use same channel as container
                    rabbitTemplate.ConvertAndSend("foo", "bar", "baz");
                    latch.Signal();
                });

            container.QueueNames = new[] { "queue" };
            container.ChannelTransacted = true;
            container.ShutdownTimeout = 100;
            container.TransactionManager = new DummyTxManager();
            container.AfterPropertiesSet();
            container.Start();

            consumer.Value.HandleBasicDeliver("qux", 1, false, "foo", "bar", new BasicProperties(), new byte[] { 0 });

            Assert.IsTrue(latch.Wait(new TimeSpan(0, 0, 10)));

            var e = tooManyChannels.Value;
            if (e != null)
            {
                throw e;
            }

            mockConnection.Verify(m => m.CreateModel(), Times.Once());
            Assert.True(commitLatch.Wait(new TimeSpan(0, 0, 10)));
            onlyChannel.Verify(m => m.TxCommit(), Times.Once());
            onlyChannel.Verify(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once());

            // verify close() was never called on the channel
            var cachedChannelsTransactionalField = typeof(CachingConnectionFactory).GetField("cachedChannelsTransactional", BindingFlags.NonPublic | BindingFlags.Instance);
            var channels = (LinkedList<IChannelProxy>)cachedChannelsTransactionalField.GetValue(cachingConnectionFactory);
            Assert.AreEqual(0, channels.Count);

            container.Stop();
        }
    }

    internal class DummyTxManager : AbstractPlatformTransactionManager
    {
        /// <summary>Initializes a new instance of the <see cref="DummyTxManager"/> class.</summary>
        public DummyTxManager() { this.TransactionSynchronization = TransactionSynchronizationState.Always; }

        /// <summary>The do begin.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="definition">The definition.</param>
        protected override void DoBegin(object transaction, ITransactionDefinition definition) { }

        /// <summary>The do commit.</summary>
        /// <param name="status">The status.</param>
        protected override void DoCommit(DefaultTransactionStatus status) { }

        /// <summary>The do get transaction.</summary>
        /// <returns>The System.Object.</returns>
        protected override object DoGetTransaction() { return new object(); }

        /// <summary>The do rollback.</summary>
        /// <param name="status">The status.</param>
        protected override void DoRollback(DefaultTransactionStatus status) { }
    }
}
