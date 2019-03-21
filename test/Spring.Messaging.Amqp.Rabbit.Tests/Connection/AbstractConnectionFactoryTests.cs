// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AbstractConnectionFactoryTests.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Support;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using IConnection = RabbitMQ.Client.IConnection;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Connection
{
    /// <summary>
    /// Abstract connection factory tests.
    /// </summary>
    public abstract class AbstractConnectionFactoryTests
    {
        /// <summary>Creates the connection factory.</summary>
        /// <param name="mockConnectionFactory">The mock connection factory.</param>
        /// <returns>The connection factory.</returns>
        protected abstract AbstractConnectionFactory CreateConnectionFactory(ConnectionFactory mockConnectionFactory);

        /// <summary>
        /// Tests the with listener.
        /// </summary>
        [Test]
        public void TestWithListener()
        {
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();

            mockConnectionFactory.Setup(factory => factory.CreateConnection()).Returns(mockConnection.Object);

            var called = new AtomicInteger(0);
            var connectionFactory = this.CreateConnectionFactory(mockConnectionFactory.Object);
            var connectionListeners = new List<IConnectionListener>();
            var mockConnectionListener = new Mock<IConnectionListener>();
            mockConnectionListener.Setup(listener => listener.OnCreate(It.IsAny<Rabbit.Connection.IConnection>())).Callback(() => called.IncrementValueAndReturn());
            mockConnectionListener.Setup(listener => listener.OnClose(It.IsAny<Rabbit.Connection.IConnection>())).Callback(() => called.DecrementValueAndReturn());
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
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();

            mockConnectionFactory.Setup(factory => factory.CreateConnection()).Returns(mockConnection.Object);

            var called = new AtomicInteger(0);
            var connectionFactory = this.CreateConnectionFactory(mockConnectionFactory.Object);
            var con = connectionFactory.CreateConnection();
            Assert.AreEqual(0, called.Value);

            var connectionListeners = new List<IConnectionListener>();
            var mockConnectionListener = new Mock<IConnectionListener>();
            mockConnectionListener.Setup(listener => listener.OnCreate(It.IsAny<Rabbit.Connection.IConnection>())).Callback(() => called.IncrementValueAndReturn());
            mockConnectionListener.Setup(listener => listener.OnClose(It.IsAny<Rabbit.Connection.IConnection>())).Callback(() => called.DecrementValueAndReturn());
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
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection1 = new Mock<IConnection>();
            var mockConnection2 = new Mock<IConnection>();

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
            var mockConnectionFactory = new Mock<ConnectionFactory>();

            var connectionFactory = this.CreateConnectionFactory(mockConnectionFactory.Object);
            connectionFactory.Dispose();

            mockConnectionFactory.Verify(c => c.CreateConnection(), Times.Never());
        }
    }
}
