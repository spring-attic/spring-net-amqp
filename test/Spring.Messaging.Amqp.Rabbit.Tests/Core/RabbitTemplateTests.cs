// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTemplateTests.cs" company="The original author or authors.">
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
using System.Collections;
using System.Reflection;
using System.Text;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.v0_9_1;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Transaction;
using Spring.Transaction.Support;
using IConnection = RabbitMQ.Client.IConnection;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core
{
    /// <summary>
    /// Rabbit Template Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class RabbitTemplateTests
    {
        /// <summary>The return connection after commit.</summary>
        [Test]
        public void ReturnConnectionAfterCommit()
        {
            var txTemplate = new TransactionTemplate(new RabbitTemplateTestsTransactionManager());
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IModel>();

            mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(m => m.IsOpen).Returns(true);
            mockConnection.Setup(m => m.CreateModel()).Returns(mockChannel.Object);
            mockChannel.Setup(m => m.IsOpen).Returns(true);
            mockChannel.Setup(m => m.CreateBasicProperties()).Returns(() => new BasicProperties());

            var template = new RabbitTemplate(new CachingConnectionFactory(mockConnectionFactory.Object));
            template.ChannelTransacted = true;

            txTemplate.Execute(
                status =>
                {
                    template.ConvertAndSend("foo", "bar");
                    return null;
                });

            txTemplate.Execute(
                status =>
                {
                    template.ConvertAndSend("baz", "qux");
                    return null;
                });

            mockConnectionFactory.Verify(m => m.CreateConnection(), Times.Once());

            // ensure we used the same channel
            mockConnection.Verify(m => m.CreateModel(), Times.Once());
        }

        /// <summary>The test convert bytes.</summary>
        [Test]
        public void TestConvertBytes()
        {
            var template = new RabbitTemplate();
            byte[] payload = Encoding.UTF8.GetBytes("Hello, world!");
            var convertMessageIfNecessaryMethod = typeof(RabbitTemplate).GetMethod("ConvertMessageIfNecessary", BindingFlags.NonPublic | BindingFlags.Instance);
            var message = (Message)convertMessageIfNecessaryMethod.Invoke(template, new[] { payload });
            Assert.AreSame(payload, message.Body);
        }

        /// <summary>The test convert string.</summary>
        [Test]
        public void TestConvertString()
        {
            var template = new RabbitTemplate();
            var payload = "Hello, world!";
            var convertMessageIfNecessaryMethod = typeof(RabbitTemplate).GetMethod("ConvertMessageIfNecessary", BindingFlags.NonPublic | BindingFlags.Instance);
            var message = (Message)convertMessageIfNecessaryMethod.Invoke(template, new[] { payload });
            Assert.AreEqual(payload, Encoding.GetEncoding(SimpleMessageConverter.DEFAULT_CHARSET).GetString(message.Body));
        }

        /// <summary>The test convert serializable.</summary>
        [Test]
        public void TestConvertSerializable()
        {
            var template = new RabbitTemplate();
            var payload = 43L;
            var convertMessageIfNecessaryMethod = typeof(RabbitTemplate).GetMethod("ConvertMessageIfNecessary", BindingFlags.NonPublic | BindingFlags.Instance);
            var message = (Message)convertMessageIfNecessaryMethod.Invoke(template, new object[] { payload });

            Assert.AreEqual(payload, SerializationUtils.DeserializeObject(message.Body));
        }

        /// <summary>The test convert message.</summary>
        [Test]
        public void TestConvertMessage()
        {
            var template = new RabbitTemplate();
            var input = new Message(Encoding.UTF8.GetBytes("Hello, world!"), new MessageProperties());
            var convertMessageIfNecessaryMethod = typeof(RabbitTemplate).GetMethod("ConvertMessageIfNecessary", BindingFlags.NonPublic | BindingFlags.Instance);
            var message = (Message)convertMessageIfNecessaryMethod.Invoke(template, new object[] { input });
            Assert.AreSame(input, message);
        }

        /// <summary>The dont hang consumer thread.</summary>
        [Test] // AMQP-249
        public void DontHangConsumerThread()
        {
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IModel>();

            mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(m => m.IsOpen).Returns(true);
            mockConnection.Setup(m => m.CreateModel()).Returns(mockChannel.Object);
            mockChannel.Setup(m => m.QueueDeclare()).Returns(new QueueDeclareOk("foo", 0, 0));
            mockChannel.Setup(m => m.CreateBasicProperties()).Returns(() => new BasicProperties());

            var consumer = new AtomicReference<DefaultBasicConsumer>();
            mockChannel.Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary>(), It.IsAny<IBasicConsumer>())).Callback
                <string, bool, string, bool, bool, IDictionary, IBasicConsumer>(
                    (a1, a2, a3, a4, a5, a6, a7) => consumer.LazySet((DefaultBasicConsumer)a7));

            var template = new RabbitTemplate(new SingleConnectionFactory(mockConnectionFactory.Object));
            template.ReplyTimeout = 1;
            var input = new Message(Encoding.UTF8.GetBytes("Hello, world!"), new MessageProperties());
            var doSendAndReceiveWithTemporaryMethod = typeof(RabbitTemplate).GetMethod("DoSendAndReceiveWithTemporary", BindingFlags.NonPublic | BindingFlags.Instance);
            doSendAndReceiveWithTemporaryMethod.Invoke(template, new object[] { "foo", "bar", input });
            var envelope = new BasicGetResult(1, false, "foo", "bar", 0, new BasicProperties(), null);

            // used to hang here because of the SynchronousQueue and DoSendAndReceive() already exited
            consumer.Value.HandleBasicDeliver("foo", envelope.DeliveryTag, envelope.Redelivered, envelope.Exchange, envelope.RoutingKey, new BasicProperties(), new byte[0]);
        }
    }

    internal class RabbitTemplateTestsTransactionManager : AbstractPlatformTransactionManager
    {
        /// <summary>The do get transaction.</summary>
        /// <returns>The System.Object.</returns>
        protected override object DoGetTransaction() { return new object(); }

        /// <summary>The do begin.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="definition">The definition.</param>
        protected override void DoBegin(object transaction, ITransactionDefinition definition) { }

        /// <summary>The do commit.</summary>
        /// <param name="status">The status.</param>
        protected override void DoCommit(DefaultTransactionStatus status) { }

        /// <summary>The do rollback.</summary>
        /// <param name="status">The status.</param>
        protected override void DoRollback(DefaultTransactionStatus status) { }
    }
}
