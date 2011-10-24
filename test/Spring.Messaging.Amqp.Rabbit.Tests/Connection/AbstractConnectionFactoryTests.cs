
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMoq;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;

using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Support;
using Spring.Threading.AtomicTypes;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Connection
{
    /// <summary>
    /// Abstract connection factory tests.
    /// </summary>
    public abstract class AbstractConnectionFactoryTests
    {
        /// <summary>
        /// Creates the connection factory.
        /// </summary>
        /// <param name="mockConnectionFactory">The mock connection factory.</param>
        /// <returns>The connection factory.</returns>
        protected abstract AbstractConnectionFactory CreateConnectionFactory(ConnectionFactory mockConnectionFactory);

        /// <summary>
        /// Tests the with listener.
        /// </summary>
        [Test]
        public void TestWithListener()
        {
            var mocker = new AutoMoqer();

            var mockConnectionFactory = mocker.GetMock<RabbitMQ.Client.ConnectionFactory>();
            var mockConnection = mocker.GetMock<RabbitMQ.Client.IConnection>();

            mockConnectionFactory.Setup(factory => factory.CreateConnection()).Returns(mockConnection.Object);

            var called = new AtomicInteger(0);
            var connectionFactory = this.CreateConnectionFactory(mockConnectionFactory.Object);
            var connectionListeners = new List<IConnectionListener>();
            var mockConnectionListener = mocker.GetMock<IConnectionListener>();
            mockConnectionListener.Setup(listener => listener.OnCreate(It.IsAny<Spring.Messaging.Amqp.Rabbit.Connection.IConnection>())).Callback(() => called.IncrementValueAndReturn());
            mockConnectionListener.Setup(listener => listener.OnClose(It.IsAny<Spring.Messaging.Amqp.Rabbit.Connection.IConnection>())).Callback(() => called.DecrementValueAndReturn());
            connectionListeners.Add(mockConnectionListener.Object);
            connectionFactory.ConnectionListeners = connectionListeners;

            var con = connectionFactory.CreateConnection();
            Assert.AreEqual(1, called.Value);

            con.Close();
            Assert.AreEqual(1, called.Value);
            mockConnection.Verify(c => c.Close(), Times.Never());
            connectionFactory.CreateConnection();
            Assert.AreEqual(1, called.Value);

            connectionFactory.Dispose();
            Assert.AreEqual(0, called.Value);
            mockConnection.Verify(c => c.Close(), Times.AtLeastOnce());

            mockConnectionFactory.Verify(c => c.CreateConnection(), Times.Exactly(1));
        }

        /// <summary>
        /// Tests the with listener registered after open.
        /// </summary>
        [Test]
        public void TestWithListenerRegisteredAfterOpen() 
        {
            var mocker = new AutoMoqer();

            var mockConnectionFactory = mocker.GetMock<RabbitMQ.Client.ConnectionFactory>();
            var mockConnection = mocker.GetMock<RabbitMQ.Client.IConnection>();

            mockConnectionFactory.Setup(factory => factory.CreateConnection()).Returns(mockConnection.Object);

            var called = new AtomicInteger(0);
            var connectionFactory = this.CreateConnectionFactory(mockConnectionFactory.Object);
            var con = connectionFactory.CreateConnection();
            Assert.AreEqual(0, called.Value);
            
            var connectionListeners = new List<IConnectionListener>();
            var mockConnectionListener = mocker.GetMock<IConnectionListener>();
            mockConnectionListener.Setup(listener => listener.OnCreate(It.IsAny<Spring.Messaging.Amqp.Rabbit.Connection.IConnection>())).Callback(() => called.IncrementValueAndReturn());
            mockConnectionListener.Setup(listener => listener.OnClose(It.IsAny<Spring.Messaging.Amqp.Rabbit.Connection.IConnection>())).Callback(() => called.DecrementValueAndReturn());
            connectionListeners.Add(mockConnectionListener.Object);
            connectionFactory.ConnectionListeners = connectionListeners;

            Assert.AreEqual(1, called.Value);

            con.Close();
            Assert.AreEqual(1, called.Value);
            mockConnection.Verify(c => c.Close(), Times.Never());

            connectionFactory.CreateConnection();
            Assert.AreEqual(1, called.Value);

            connectionFactory.Dispose();
            Assert.AreEqual(0, called.Value);
            mockConnection.Verify(c => c.Close(), Times.AtLeastOnce());

            mockConnectionFactory.Verify(c => c.CreateConnection(), Times.Exactly(1));
        }

        /// <summary>
        /// Tests the close invalid connection.
        /// </summary>
        [Test]
        public void TestCloseInvalidConnection()
        {
            var mocker = new AutoMoqer();

            var mockConnectionFactory = mocker.GetMock<RabbitMQ.Client.ConnectionFactory>();
            var mockConnection1 = mocker.GetMock<RabbitMQ.Client.IConnection>();
            var mockConnection2 = mocker.GetMock<RabbitMQ.Client.IConnection>();

            mockConnectionFactory.Setup(factory => factory.CreateConnection()).ReturnsInOrder(mockConnection1.Object, mockConnection2.Object);

            // simulate a dead connection
            mockConnection1.Setup(c => c.IsOpen).Returns(false);

            var connectionFactory = this.CreateConnectionFactory(mockConnectionFactory.Object);

            var connection = connectionFactory.CreateConnection();

            // the dead connection should be discarded
            connection.CreateChannel(false);
            mockConnectionFactory.Verify(c => c.CreateConnection(), Times.Exactly(2));
            mockConnection2.Verify(c => c.CreateModel(), Times.Exactly(1));

            connectionFactory.Dispose();
            mockConnection2.Verify(c => c.Close(), Times.Exactly(1));
        }

        /// <summary>
        /// Tests the destroy before used.
        /// </summary>
        [Test]
        public void TestDestroyBeforeUsed()
        {
            var mocker = new AutoMoqer();

            var mockConnectionFactory = mocker.GetMock<RabbitMQ.Client.ConnectionFactory>();

            var connectionFactory = this.CreateConnectionFactory(mockConnectionFactory.Object);
            connectionFactory.Dispose();

            mockConnectionFactory.Verify(c => c.CreateConnection(), Times.Never());
        }
    }
}
