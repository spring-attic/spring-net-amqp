// -----------------------------------------------------------------------
// <copyright file="RabbitTemplateHeaderTests.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Rabbit Template Header Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class RabbitTemplateHeaderTests
    {
        [Test]
        public void TestPushPop()
        {
            var template = new RabbitTemplate();
            var pushHeaderMethod = typeof(RabbitTemplate).GetMethod("PushHeaderValue", BindingFlags.NonPublic | BindingFlags.Instance);
            var header = (string)pushHeaderMethod.Invoke(template, new object[] { "a", null });
            Assert.AreEqual("a", header);
            header = (string)pushHeaderMethod.Invoke(template, new object[] { "b", header });
            Assert.AreEqual("b:a", header);
            header = (string)pushHeaderMethod.Invoke(template, new object[] { "c", header });
            Assert.AreEqual("c:b:a", header);

            var popHeaderMethod = typeof(RabbitTemplate).GetMethod("PopHeaderValue", BindingFlags.NonPublic | BindingFlags.Instance);
            var poppedHeader = popHeaderMethod.Invoke(template, new object[] { header });
            var poppedValueField = poppedHeader.GetType().GetField("poppedValue", BindingFlags.NonPublic | BindingFlags.Instance);
            var newValueField = poppedHeader.GetType().GetField("newValue", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.AreEqual("c", poppedValueField.GetValue(poppedHeader));
            poppedHeader = popHeaderMethod.Invoke(template, new[] { newValueField.GetValue(poppedHeader) });
            Assert.AreEqual("b", poppedValueField.GetValue(poppedHeader));
            poppedHeader = popHeaderMethod.Invoke(template, new[] { newValueField.GetValue(poppedHeader) });
            Assert.AreEqual("a", poppedValueField.GetValue(poppedHeader));
            Assert.IsNull(newValueField.GetValue(poppedHeader));
        }

        [Test]
        public void TestReplyToOneDeep()
        {
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IModel>();

            mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(m => m.IsOpen).Returns(true);
            mockConnection.Setup(m => m.CreateModel()).Returns(mockChannel.Object);
            mockChannel.Setup(m => m.CreateBasicProperties()).Returns(() => new RabbitMQ.Client.Framing.v0_9_1.BasicProperties());

            var template = new RabbitTemplate(new SingleConnectionFactory(mockConnectionFactory.Object));
            var replyQueue = new Queue("new.replyTo");
            template.ReplyQueue = replyQueue;

            var messageProperties = new MessageProperties();
            messageProperties.ReplyTo = "replyTo1";
            var message = new Message(Encoding.UTF8.GetBytes("Hello, world!"), messageProperties);
            var props = new List<IBasicProperties>();
            mockChannel.Setup(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>())).Callback<string, string, bool, bool, IBasicProperties, byte[]>(
                    (a1, a2, a3, a4, a5, a6) =>
                    {
                        var basicProps = a5;
                        props.Add(basicProps);
                        var springProps = new DefaultMessagePropertiesConverter().ToMessageProperties(basicProps, null, "UTF-8");
                        var replyMessage = new Message(Encoding.UTF8.GetBytes("!dlrow olleH"), springProps);
                        template.OnMessage(replyMessage);
                    });

            var reply = template.SendAndReceive(message);
            Assert.IsNotNull(reply);

            Assert.AreEqual(1, props.Count);
            var basicProperties = props[0];
            Assert.AreEqual("new.replyTo", basicProperties.ReplyTo);
            Assert.AreEqual("replyTo1", basicProperties.Headers[RabbitTemplate.STACKED_REPLY_TO_HEADER]);
            Assert.IsNotNull(basicProperties.Headers[RabbitTemplate.STACKED_CORRELATION_HEADER]);
        }


        [Test]
        public void TestReplyToTwoDeep()
        {
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IModel>();

            mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(m => m.IsOpen).Returns(true);
            mockConnection.Setup(m => m.CreateModel()).Returns(mockChannel.Object);
            mockChannel.Setup(m => m.CreateBasicProperties()).Returns(() => new RabbitMQ.Client.Framing.v0_9_1.BasicProperties());

            var template = new RabbitTemplate(new SingleConnectionFactory(mockConnectionFactory.Object));
            var replyQueue = new Queue("new.replyTo");
            template.ReplyQueue = replyQueue;

            var messageProperties = new MessageProperties();
            messageProperties.ReplyTo = "replyTo2";
            messageProperties.SetHeader(RabbitTemplate.STACKED_REPLY_TO_HEADER, "replyTo1");
            messageProperties.SetHeader(RabbitTemplate.STACKED_CORRELATION_HEADER, "a");
            var message = new Message(Encoding.UTF8.GetBytes("Hello, world!"), messageProperties);
            var props = new List<IBasicProperties>();

            mockChannel.Setup(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>())).Callback<string, string, bool, bool, IBasicProperties, byte[]>
                (
                    (a1, a2, a3, a4, a5, a6) =>
                    {
                        var basicProps = a5;
                        props.Add(basicProps);
                        var springProps = new DefaultMessagePropertiesConverter().ToMessageProperties(basicProps, null, "UTF-8");
                        var replyMessage = new Message(Encoding.UTF8.GetBytes("!dlrow olleH"), springProps);
                        template.OnMessage(replyMessage);
                    });

            var reply = template.SendAndReceive(message);

            Assert.AreEqual(1, props.Count);
            var basicProperties = props[0];
            Assert.AreEqual("new.replyTo", basicProperties.ReplyTo);
            Assert.AreEqual("replyTo2:replyTo1", basicProperties.Headers[RabbitTemplate.STACKED_REPLY_TO_HEADER]);
            Assert.IsTrue(((string)basicProperties.Headers[RabbitTemplate.STACKED_CORRELATION_HEADER]).EndsWith(":a"));

            Assert.AreEqual("replyTo1", reply.MessageProperties.Headers[RabbitTemplate.STACKED_REPLY_TO_HEADER]);
            Assert.AreEqual("a", reply.MessageProperties.Headers[RabbitTemplate.STACKED_CORRELATION_HEADER]);
        }

        [Test]
        public void TestReplyToThreeDeep()
        {
            var mockConnectionFactory = new Mock<ConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IModel>();

            mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(m => m.IsOpen).Returns(true);
            mockConnection.Setup(m => m.CreateModel()).Returns(mockChannel.Object);
            mockChannel.Setup(m => m.CreateBasicProperties()).Returns(() => new RabbitMQ.Client.Framing.v0_9_1.BasicProperties());

            var template = new RabbitTemplate(new SingleConnectionFactory(mockConnectionFactory.Object));
            var replyQueue = new Queue("new.replyTo");
            template.ReplyQueue = replyQueue;

            var messageProperties = new MessageProperties();
            messageProperties.ReplyTo = "replyTo2";
            messageProperties.SetHeader(RabbitTemplate.STACKED_REPLY_TO_HEADER, "replyTo1");
            messageProperties.SetHeader(RabbitTemplate.STACKED_CORRELATION_HEADER, "a");
            var message = new Message(Encoding.UTF8.GetBytes("Hello, world!"), messageProperties);
            var props = new List<IBasicProperties>();

            var count = new AtomicInteger();
            var nestedReplyTo = new List<string>();
            var nestedReplyStack = new List<string>();
            var nestedCorrelation = new List<string>();

            mockChannel.Setup(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>())).Callback<string, string, bool, bool, IBasicProperties, byte[]>
                (
                    (a1, a2, a3, a4, a5, a6) =>
                    {
                        var basicProps = a5;
                        props.Add(basicProps);
                        var springProps = new DefaultMessagePropertiesConverter().ToMessageProperties(basicProps, null, "UTF-8");
                        var replyMessage = new Message(Encoding.UTF8.GetBytes("!dlrow olleH"), springProps);
                        if (count.IncrementValueAndReturn() < 2)
                        {
                            var anotherMessage = new Message(Encoding.UTF8.GetBytes("Second"), springProps);
                            replyMessage = template.SendAndReceive(anotherMessage);
                            nestedReplyTo.Add(replyMessage.MessageProperties.ReplyTo);
                            nestedReplyStack.Add((string)replyMessage.MessageProperties.Headers[RabbitTemplate.STACKED_REPLY_TO_HEADER]);
                            nestedCorrelation.Add((string)replyMessage.MessageProperties.Headers[RabbitTemplate.STACKED_CORRELATION_HEADER]);
                        }

                        template.OnMessage(replyMessage);
                    });

            var reply = template.SendAndReceive(message);
            Assert.IsNotNull(reply);

            Assert.AreEqual(2, props.Count);
            var basicProperties = props[0];
            Assert.AreEqual("new.replyTo", basicProperties.ReplyTo);
            Assert.AreEqual("replyTo2:replyTo1", basicProperties.Headers[RabbitTemplate.STACKED_REPLY_TO_HEADER]);
            Assert.IsTrue(((string)basicProperties.Headers[RabbitTemplate.STACKED_CORRELATION_HEADER]).EndsWith(":a"));

            basicProperties = props[1];
            Assert.AreEqual("new.replyTo", basicProperties.ReplyTo);
            Assert.AreEqual("new.replyTo:replyTo2:replyTo1", basicProperties.Headers[RabbitTemplate.STACKED_REPLY_TO_HEADER]);
            Assert.IsTrue(((string)basicProperties.Headers[RabbitTemplate.STACKED_CORRELATION_HEADER]).EndsWith(":a"));

            Assert.AreEqual("replyTo1", reply.MessageProperties.Headers[RabbitTemplate.STACKED_REPLY_TO_HEADER]);
            Assert.AreEqual("a", reply.MessageProperties.Headers[RabbitTemplate.STACKED_CORRELATION_HEADER]);

            Assert.AreEqual(1, nestedReplyTo.Count);
            Assert.AreEqual(1, nestedReplyStack.Count);
            Assert.AreEqual(1, nestedCorrelation.Count);
            Assert.AreEqual("replyTo2:replyTo1", nestedReplyStack[0]);
            Assert.IsTrue(nestedCorrelation[0].EndsWith(":a"));
        }
    }
}
