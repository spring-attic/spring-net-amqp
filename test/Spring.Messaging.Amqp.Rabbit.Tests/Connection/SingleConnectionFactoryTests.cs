// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SingleConnectionFactoryTests.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using IConnection = RabbitMQ.Client.IConnection;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Connection
{
    /// <summary>
    /// Tests for the single connection factory.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class SingleConnectionFactoryTests : AbstractConnectionFactoryTests
    {
        /// <summary>Creates the connection factory.</summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <returns>The created connection factory.</returns>
        protected override AbstractConnectionFactory CreateConnectionFactory(ConnectionFactory connectionFactory) { return new SingleConnectionFactory(connectionFactory); }

        /// <summary>The test with channel listener.</summary>
        [Test]
        public void TestWithChannelListener()
        {
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IModel>();

            mockConnectionFactory.Setup(factory => factory.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(c => c.IsOpen).Returns(true);
            mockConnection.Setup(connection => connection.CreateModel()).Returns(mockChannel.Object);

            var called = new AtomicInteger(0);
            var connectionFactory = this.CreateConnectionFactory(mockConnectionFactory.Object);
            var channelListeners = new List<IChannelListener>();
            var mockChannelListener = new Mock<IChannelListener>();
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
