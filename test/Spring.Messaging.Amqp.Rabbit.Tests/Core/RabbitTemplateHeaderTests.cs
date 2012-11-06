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
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;

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
            var mockConnection = new Mock<RabbitMQ.Client.IConnection>();
            var mockChannel = new Mock<IModel>();

            mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(m => m.IsOpen).Returns(true);
            mockConnection.Setup(m => m.CreateModel()).Returns(mockChannel.Object);

            var template = new RabbitTemplate(new SingleConnectionFactory(mockConnectionFactory.Object));
            var replyQueue = new Queue("new.replyTo");
            template.ReplyQueue = replyQueue;

            var messageProperties = new MessageProperties();
            messageProperties.ReplyTo = "replyTo1";
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
            Assert.IsNotNull(reply);

            Assert.AreEqual(1, props.Count);
            var basicProperties = props[0];
            Assert.AreEqual("new.replyTo", basicProperties.ReplyTo);
            Assert.AreEqual("replyTo1", basicProperties.Headers[RabbitTemplate.STACKED_REPLY_TO_HEADER]);
            Assert.IsNotNull(basicProperties.Headers[RabbitTemplate.STACKED_CORRELATION_HEADER]);
        }

        /*
	[Test]
	public void testReplyToTwoDeep()  {
		ConnectionFactory mockConnectionFactory = mock(ConnectionFactory.class);
		Connection mockConnection = mock(Connection.class);
		Channel mockChannel = mock(Channel.class);

		when(mockConnectionFactory.newConnection((ExecutorService) null)).thenReturn(mockConnection);
		when(mockConnection.isOpen()).thenReturn(true);
		when(mockConnection.createChannel()).thenReturn(mockChannel);

		final RabbitTemplate template = new RabbitTemplate(new SingleConnectionFactory(mockConnectionFactory));
		Queue replyQueue = new Queue("new.replyTo");
		template.setReplyQueue(replyQueue);

		MessageProperties messageProperties = new MessageProperties();
		messageProperties.setReplyTo("replyTo2");
		messageProperties.setHeader(RabbitTemplate.STACKED_REPLY_TO_HEADER, "replyTo1");
		messageProperties.setHeader(RabbitTemplate.STACKED_CORRELATION_HEADER, "a");
		Message message = new Message("Hello, world!".getBytes(), messageProperties);
		final List<BasicProperties> props = new ArrayList<BasicProperties>();
		doAnswer(new Answer<Object>() {
			public Object answer(InvocationOnMock invocation) throws Throwable {
				BasicProperties basicProps = (BasicProperties) invocation.getArguments()[4];
				props.add(basicProps);
				MessageProperties springProps = new DefaultMessagePropertiesConverter()
						.toMessageProperties(basicProps, null, "UTF-8");
				Message replyMessage = new Message("!dlrow olleH".getBytes(), springProps);
				template.onMessage(replyMessage);
				return null;
			}}
		).when(mockChannel).basicPublish(Mockito.any(String.class),
				Mockito.any(String.class), Mockito.anyBoolean(),
				Mockito.anyBoolean(), Mockito.any(BasicProperties.class), Mockito.any(byte[].class));
		Message reply = template.sendAndReceive(message);

		Assert.AreEqual(1, props.size());
		BasicProperties basicProperties = props.get(0);
		Assert.AreEqual("new.replyTo", basicProperties.getReplyTo());
		Assert.AreEqual("replyTo2:replyTo1", basicProperties.getHeaders().get(RabbitTemplate.STACKED_REPLY_TO_HEADER));
		assertTrue(((String)basicProperties.getHeaders().get(RabbitTemplate.STACKED_CORRELATION_HEADER)).endsWith(":a"));

		Assert.AreEqual("replyTo1", reply.getMessageProperties().getHeaders().get(RabbitTemplate.STACKED_REPLY_TO_HEADER));
		Assert.AreEqual("a", reply.getMessageProperties().getHeaders().get(RabbitTemplate.STACKED_CORRELATION_HEADER));
	}

	[Test]
	public void testReplyToThreeDeep() {
		ConnectionFactory mockConnectionFactory = mock(ConnectionFactory.class);
		Connection mockConnection = mock(Connection.class);
		Channel mockChannel = mock(Channel.class);

		when(mockConnectionFactory.newConnection((ExecutorService) null)).thenReturn(mockConnection);
		when(mockConnection.isOpen()).thenReturn(true);
		when(mockConnection.createChannel()).thenReturn(mockChannel);

		final RabbitTemplate template = new RabbitTemplate(new SingleConnectionFactory(mockConnectionFactory));
		Queue replyQueue = new Queue("new.replyTo");
		template.setReplyQueue(replyQueue);

		MessageProperties messageProperties = new MessageProperties();
		messageProperties.setReplyTo("replyTo2");
		messageProperties.setHeader(RabbitTemplate.STACKED_REPLY_TO_HEADER, "replyTo1");
		messageProperties.setHeader(RabbitTemplate.STACKED_CORRELATION_HEADER, "a");
		Message message = new Message("Hello, world!".getBytes(), messageProperties);
		final List<BasicProperties> props = new ArrayList<BasicProperties>();
		final AtomicInteger count = new AtomicInteger();
		final List<String> nestedReplyTo = new ArrayList<String>();
		final List<String> nestedReplyStack = new ArrayList<String>();
		final List<String> nestedCorrelation = new ArrayList<String>();
		doAnswer(new Answer<Object>() {
			public Object answer(InvocationOnMock invocation) throws Throwable {
				BasicProperties basicProps = (BasicProperties) invocation.getArguments()[4];
				props.add(basicProps);
				MessageProperties springProps = new DefaultMessagePropertiesConverter()
						.toMessageProperties(basicProps, null, "UTF-8");
				Message replyMessage = new Message("!dlrow olleH".getBytes(), springProps);
				if (count.incrementAndGet() < 2) {
					Message anotherMessage = new Message("Second".getBytes(), springProps);
					replyMessage = template.sendAndReceive(anotherMessage);
					nestedReplyTo.add(replyMessage.getMessageProperties().getReplyTo());
					nestedReplyStack.add((String) replyMessage
							.getMessageProperties().getHeaders()
							.get(RabbitTemplate.STACKED_REPLY_TO_HEADER));
					nestedCorrelation.add((String) replyMessage
							.getMessageProperties().getHeaders()
							.get(RabbitTemplate.STACKED_CORRELATION_HEADER));
				}
				template.onMessage(replyMessage);
				return null;
			}}
		).when(mockChannel).basicPublish(Mockito.any(String.class),
				Mockito.any(String.class), Mockito.anyBoolean(),
				Mockito.anyBoolean(), Mockito.any(BasicProperties.class), Mockito.any(byte[].class));
		Message reply = template.sendAndReceive(message);
		Assert.IsNotNull(reply);

		Assert.AreEqual(2, props.size());
		BasicProperties basicProperties = props.get(0);
		Assert.AreEqual("new.replyTo", basicProperties.getReplyTo());
		Assert.AreEqual("replyTo2:replyTo1", basicProperties.getHeaders().get(RabbitTemplate.STACKED_REPLY_TO_HEADER));
		assertTrue(((String)basicProperties.getHeaders().get(RabbitTemplate.STACKED_CORRELATION_HEADER)).endsWith(":a"));

		basicProperties = props.get(1);
		Assert.AreEqual("new.replyTo", basicProperties.getReplyTo());
		Assert.AreEqual("new.replyTo:replyTo2:replyTo1", basicProperties.getHeaders().get(RabbitTemplate.STACKED_REPLY_TO_HEADER));
		assertTrue(((String)basicProperties.getHeaders().get(RabbitTemplate.STACKED_CORRELATION_HEADER)).endsWith(":a"));

		Assert.AreEqual("replyTo1", reply.getMessageProperties().getHeaders().get(RabbitTemplate.STACKED_REPLY_TO_HEADER));
		Assert.AreEqual("a", reply.getMessageProperties().getHeaders().get(RabbitTemplate.STACKED_CORRELATION_HEADER));

		Assert.AreEqual(1, nestedReplyTo.size());
		Assert.AreEqual(1, nestedReplyStack.size());
		Assert.AreEqual(1, nestedCorrelation.size());
		Assert.AreEqual("replyTo2:replyTo1", nestedReplyStack.get(0));
		assertTrue(nestedCorrelation.get(0).endsWith(":a"));

	}*/
    }
}
