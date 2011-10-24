
using System.Collections.Generic;
using AutoMoq;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;

using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Threading.AtomicTypes;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Connection
{
    /// <summary>
    /// Tests for the single connection factory.
    /// </summary>
    public class SingleConnectionFactoryTests : AbstractConnectionFactoryTests
    {
        public SingleConnectionFactoryTests()
        {
            
        }
        /// <summary>
        /// Creates the connection factory.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <returns>The created connection factory.</returns>
        protected override AbstractConnectionFactory CreateConnectionFactory(RabbitMQ.Client.ConnectionFactory connectionFactory)
        {
            return new SingleConnectionFactory(connectionFactory);
        }

        [Test]
        public void TestWithChannelListener()
        {
            var mocker = new AutoMoqer();

            var mockConnectionFactory = mocker.GetMock<ConnectionFactory>();
            var mockConnection = mocker.GetMock<RabbitMQ.Client.IConnection>();
            var mockChannel = new Mock<IModel>();

            mockConnectionFactory.Setup(factory => factory.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(c => c.IsOpen).Returns(true);
            mockConnection.Setup(connection => connection.CreateModel()).Returns(mockChannel.Object);
            
            var called = new AtomicInteger(0);
            var connectionFactory = this.CreateConnectionFactory(mockConnectionFactory.Object);
            var channelListeners = new List<IChannelListener>();
            var mockChannelListener = mocker.GetMock<IChannelListener>();
            mockChannelListener.Setup(listener => listener.OnCreate(It.IsAny<IModel>(), It.IsAny<bool>())).Callback(() => called.IncrementValueAndReturn());
            channelListeners.Add(mockChannelListener.Object);
            connectionFactory.ChannelListeners = channelListeners;

            var con = connectionFactory.CreateConnection();
            var channel = con.CreateChannel(false);
            Assert.AreEqual(1, called.Value);
            channel.Close();

            con.Close();
            mockConnection.Verify(c => c.Close(), Times.Never());

            connectionFactory.CreateConnection();
            con.CreateChannel(false);
            Assert.AreEqual(2, called.Value);

            connectionFactory.Dispose();
            mockConnection.Verify(c => c.Close(), Times.AtLeastOnce());

            mockConnectionFactory.Verify(c => c.CreateConnection());
        }
    }
}
