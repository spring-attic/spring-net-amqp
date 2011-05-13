using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMoq;
using Moq;
using NUnit.Framework;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Threading.AtomicTypes;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// Tests for the single connection factory.
    /// </summary>
    /// <remarks></remarks>
    [TestFixture]
    public class SingleConnectionFactoryTests
    {

        /// <summary>
        /// Tests the with listener.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestWithListener()
        {
            var mocker = new AutoMoqer();

            var mockConnectionFactory = mocker.GetMock<RabbitMQ.Client.ConnectionFactory>();
            var mockConnection = mocker.GetMock<RabbitMQ.Client.IConnection>();

            mockConnectionFactory.Setup(c => c.CreateConnection()).Returns(mockConnection.Object);

            var called = new AtomicInteger(0);
            var connectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);
            var mockConnectionListener = new Mock<IConnectionListener>();
            mockConnectionListener.Setup(m => m.OnCreate(It.IsAny<IConnection>())).Callback((IConnection conn) => called.IncrementValueAndReturn());
            mockConnectionListener.Setup(m => m.OnClose(It.IsAny<IConnection>())).Callback((IConnection conn) => called.DecrementValueAndReturn());

            connectionFactory.ConnectionListeners = new List<IConnectionListener>() { mockConnectionListener.Object };

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
        /// <remarks></remarks>
        [Test]
        public void TestWithListenerRegisteredAfterOpen()
        {
            var mocker = new AutoMoqer();

            var mockConnectionFactory = mocker.GetMock<RabbitMQ.Client.ConnectionFactory>();
            var mockConnection = mocker.GetMock<RabbitMQ.Client.IConnection>();

            mockConnectionFactory.Setup(c => c.CreateConnection()).Returns(mockConnection.Object);

            var called = new AtomicInteger(0);
            var connectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);
            var con = connectionFactory.CreateConnection();
            Assert.AreEqual(0, called.Value);

            var mockConnectionListener = new Mock<IConnectionListener>();
            mockConnectionListener.Setup(m => m.OnCreate(It.IsAny<IConnection>())).Callback((IConnection conn) => called.IncrementValueAndReturn());
            mockConnectionListener.Setup(m => m.OnClose(It.IsAny<IConnection>())).Callback((IConnection conn) => called.DecrementValueAndReturn());

            connectionFactory.ConnectionListeners = new List<IConnectionListener>() { mockConnectionListener.Object };
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
        /// <remarks></remarks>
        [Test]
        public void TestCloseInvalidConnection()
        {
            var mocker = new AutoMoqer();

            var mockConnectionFactory = mocker.GetMock<RabbitMQ.Client.ConnectionFactory>();
            var mockConnection1 = new Mock<RabbitMQ.Client.IConnection>();
            var mockConnection2 = new Mock<RabbitMQ.Client.IConnection>();

            mockConnectionFactory.Setup(c => c.CreateConnection()).ReturnsInOrder(mockConnection1.Object, mockConnection2.Object);

            // simulate a dead connection
            mockConnection1.Setup(c => c.IsOpen).Returns(false);

            var connectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);

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
        /// <remarks></remarks>
        [Test]
        public void TestDestroyBeforeUsed()
        {
            var mocker = new AutoMoqer();

            var mockConnectionFactory = mocker.GetMock<RabbitMQ.Client.ConnectionFactory>();

            var connectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);
            connectionFactory.Dispose();

            mockConnectionFactory.Verify(c => c.CreateConnection(), Times.Never());
        }
    }
}
