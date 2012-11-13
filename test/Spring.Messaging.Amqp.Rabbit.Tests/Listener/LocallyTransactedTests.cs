// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocallyTransactedTests.cs" company="The original author or authors.">
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
using System.Collections.Concurrent;
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
using Spring.Messaging.Amqp.Rabbit.Tests.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using IConnection = RabbitMQ.Client.IConnection;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// Locally Transacted Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class LocallyTransactedTests
    {
        private static int timeout = 5000;

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

            var tooManyChannels = new BlockingCollection<Exception>(1);
            var done = false;
            mockConnection.Setup(m => m.CreateModel()).Returns(
                () =>
                {
                    if (!done)
                    {
                        done = true;
                        return onlyChannel.Object;
                    }

                    tooManyChannels.Add(new Exception("More than one channel requested"));
                    var channel = new Mock<IModel>();
                    channel.Setup(m => m.IsOpen).Returns(true);
                    return channel.Object;
                });

            var consumer = new BlockingCollection<IBasicConsumer>(1);

            onlyChannel.Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicConsumer>())).Callback<string, bool, IBasicConsumer>(
                (a1, a2, a3) => consumer.Add(a3));

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
            container.AfterPropertiesSet();
            container.Start();

            IBasicConsumer currentConsumer;
            consumer.TryTake(out currentConsumer, timeout);
            Assert.IsNotNull(currentConsumer, "Timed out getting consumer.");
            currentConsumer.HandleBasicDeliver("qux", 1, false, "foo", "bar", new BasicProperties(), new byte[] { 0 });

            Assert.IsTrue(latch.Wait(new TimeSpan(0, 0, 10)));

            var e = tooManyChannels.Count;
            if (e > 0)
            {
                throw tooManyChannels.Take();
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

        /// <summary>Verifies that an up-stack RabbitTemplate uses the listener's channel (ChannelAwareMessageListener).</summary>
        [Test]
        public void TestChannelAwareMessageListener()
        {
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            var onlyChannel = new Mock<IModel>();
            onlyChannel.Setup(m => m.IsOpen).Returns(true);
            onlyChannel.Setup(m => m.CreateBasicProperties()).Returns(() => new BasicProperties());

            var singleConnectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);

            mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(m => m.IsOpen).Returns(true);

            var tooManyChannels = new BlockingCollection<Exception>(1);
            var done = false;
            mockConnection.Setup(m => m.CreateModel()).Returns(
                () =>
                {
                    if (!done)
                    {
                        done = true;
                        return onlyChannel.Object;
                    }

                    tooManyChannels.Add(new Exception("More than one channel requested"));
                    var channel = new Mock<IModel>();
                    channel.Setup(m => m.IsOpen).Returns(true);
                    return channel.Object;
                });

            var consumer = new BlockingCollection<IBasicConsumer>(1);

            onlyChannel.Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicConsumer>())).Callback<string, bool, IBasicConsumer>(
                (a1, a2, a3) => consumer.Add(a3));

            var commitLatch = new CountdownEvent(1);
            onlyChannel.Setup(m => m.TxCommit()).Callback(() => commitLatch.Signal());

            var latch = new CountdownEvent(1);
            var exposed = new AtomicReference<IModel>();
            var container = new SimpleMessageListenerContainer(singleConnectionFactory);
            var mockListener = new Mock<IChannelAwareMessageListener>();
            mockListener.Setup(m => m.OnMessage(It.IsAny<Message>(), It.IsAny<IModel>())).Callback<Message, IModel>(
                (message, channel) =>
                {
                    exposed.LazySet(channel);
                    var rabbitTemplate = new RabbitTemplate(singleConnectionFactory);
                    rabbitTemplate.ChannelTransacted = true;

                    // should use same channel as container
                    rabbitTemplate.ConvertAndSend("foo", "bar", "baz");
                    latch.Signal();
                });
            container.MessageListener = mockListener.Object;

            container.QueueNames = new[] { "queue" };
            container.ChannelTransacted = true;
            container.ShutdownTimeout = 100;
            container.AfterPropertiesSet();
            container.Start();

            IBasicConsumer currentConsumer;
            consumer.TryTake(out currentConsumer, timeout);
            Assert.IsNotNull(currentConsumer, "Timed out getting consumer.");
            currentConsumer.HandleBasicDeliver("qux", 1, false, "foo", "bar", new BasicProperties(), new byte[] { 0 });

            Assert.IsTrue(latch.Wait(new TimeSpan(0, 0, 10)));

            var e = tooManyChannels.Count;
            if (e > 0)
            {
                throw tooManyChannels.Take();
            }

            mockConnection.Verify(m => m.CreateModel(), Times.Once());
            Assert.IsTrue(commitLatch.Wait(new TimeSpan(0, 0, 10)));
            onlyChannel.Verify(m => m.TxCommit(), Times.Once());
            onlyChannel.Verify(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once());

            // verify close() was never called on the channel
            onlyChannel.Verify(m => m.Close(), Times.Never());

            container.Stop();

            Assert.AreSame(onlyChannel.Object, exposed.Value);
        }

        /// <summary>Verifies that the listener channel is not exposed when so configured and up-stack RabbitTemplate uses the additional channel.</summary>
        [Test]
        public void TestChannelAwareMessageListenerDontExpose()
        {
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            var firstChannel = new Mock<IModel>();
            firstChannel.Setup(m => m.IsOpen).Returns(true);
            firstChannel.Setup(m => m.CreateBasicProperties()).Returns(() => new BasicProperties());

            var secondChannel = new Mock<IModel>();
            secondChannel.Setup(m => m.IsOpen).Returns(true);
            secondChannel.Setup(m => m.CreateBasicProperties()).Returns(() => new BasicProperties());


            var singleConnectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);

            mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(m => m.IsOpen).Returns(true);

            var tooManyChannels = new BlockingCollection<Exception>(1);
            var done = false;
            mockConnection.Setup(m => m.CreateModel()).Returns(
                () =>
                {
                    if (!done)
                    {
                        done = true;
                        return firstChannel.Object;
                    }

                    return secondChannel.Object;
                });

            var consumer = new BlockingCollection<IBasicConsumer>(1);

            firstChannel.Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicConsumer>())).Callback<string, bool, IBasicConsumer>(
                (a1, a2, a3) => consumer.Add(a3));

            var commitLatch = new CountdownEvent(1);
            firstChannel.Setup(m => m.TxCommit()).Callback(() => commitLatch.Signal());

            var latch = new CountdownEvent(1);
            var exposed = new AtomicReference<IModel>();
            var container = new SimpleMessageListenerContainer(singleConnectionFactory);
            var mockListener = new Mock<IChannelAwareMessageListener>();
            mockListener.Setup(m => m.OnMessage(It.IsAny<Message>(), It.IsAny<IModel>())).Callback<Message, IModel>(
                (message, channel) =>
                {
                    exposed.LazySet(channel);
                    var rabbitTemplate = new RabbitTemplate(singleConnectionFactory);
                    rabbitTemplate.ChannelTransacted = true;

                    // should use same channel as container
                    rabbitTemplate.ConvertAndSend("foo", "bar", "baz");
                    latch.Signal();
                });
            container.MessageListener = mockListener.Object;

            container.QueueNames = new[] { "queue" };
            container.ChannelTransacted = true;
            container.ExposeListenerChannel = false;
            container.ShutdownTimeout = 100;
            container.AfterPropertiesSet();
            container.Start();

            IBasicConsumer currentConsumer;
            consumer.TryTake(out currentConsumer, timeout);
            Assert.IsNotNull(currentConsumer, "Timed out getting consumer.");
            currentConsumer.HandleBasicDeliver("qux", 1, false, "foo", "bar", new BasicProperties(), new byte[] { 0 });

            Assert.IsTrue(latch.Wait(new TimeSpan(0, 0, 10)));

            var e = tooManyChannels.Count;
            if (e > 0)
            {
                throw tooManyChannels.Take();
            }

            // once for listener, once for exposed + 0 for template (used bound)
            mockConnection.Verify(m => m.CreateModel(), Times.Exactly(2));
            Assert.IsTrue(commitLatch.Wait(new TimeSpan(0, 0, 10)));
            firstChannel.Verify(m => m.TxCommit(), Times.Once());
            secondChannel.Verify(m => m.TxCommit(), Times.Once());
            secondChannel.Verify(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once());

            Assert.AreSame(secondChannel.Object, exposed.Value);

            firstChannel.Verify(m => m.Close(), Times.Never());
            secondChannel.Verify(m => m.Close(), Times.Once());
            container.Stop();
        }
    }
}
