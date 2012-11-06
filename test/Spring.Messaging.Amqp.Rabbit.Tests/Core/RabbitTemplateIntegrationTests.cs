// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTemplateIntegrationTests.cs" company="The original author or authors.">
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Transaction;
using Spring.Transaction.Support;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core
{
    /// <summary>
    /// Rabbit template integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class RabbitTemplateIntegrationTests : AbstractRabbitIntegrationTest
    {
        #region Fixture Setup and Teardown

        /// <summary>
        /// Code to execute before fixture setup.
        /// </summary>
        public override void BeforeFixtureSetUp() { }

        /// <summary>
        /// Code to execute before fixture teardown.
        /// </summary>
        public override void BeforeFixtureTearDown() { }

        /// <summary>
        /// Code to execute after fixture setup.
        /// </summary>
        public override void AfterFixtureSetUp() { }

        /// <summary>
        /// Code to execute after fixture teardown.
        /// </summary>
        public override void AfterFixtureTearDown() { }
        #endregion

        /// <summary>
        /// The test route.
        /// </summary>
        private static readonly string ROUTE = "test.queue";

        /// <summary>
        /// The rabbit template.
        /// </summary>
        private RabbitTemplate template;

        /// <summary>
        /// Creates this instance.
        /// </summary>
        [SetUp]
        public void Create()
        {
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(ROUTE);
            this.brokerIsRunning.Apply();
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.Port = BrokerTestUtils.GetPort();
            this.template = new RabbitTemplate(connectionFactory);
        }

        /// <summary>
        /// Tests the send to non existent and then receive.
        /// </summary>
        [Test]
        public void TestSendToNonExistentAndThenReceive()
        {
            // If transacted then the commit fails on send, so we get a nice synchronous exception
            this.template.ChannelTransacted = true;
            try
            {
                this.template.ConvertAndSend(string.Empty, "no.such.route", "message");

                // fail("Expected AmqpException");
            }
            catch (AmqpException e)
            {
                // e.printStackTrace();
            }

            // Now send the real message, and all should be well...
            this.template.ConvertAndSend(ROUTE, "message");
            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send and receive with post processor.
        /// </summary>
        [Test]
        public void TestSendAndReceiveWithPostProcessor()
        {
            this.template.ConvertAndSend(
                ROUTE,
                "message",
                message =>
                {
                    message.MessageProperties.ContentType = "text/other";

                    // message.getMessageProperties().setUserId("foo");
                    return message;
                });

            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send and receive.
        /// </summary>
        [Test]
        public void TestSendAndReceive()
        {
            this.template.ConvertAndSend(ROUTE, "message");
            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send and receive transacted.
        /// </summary>
        [Test]
        public void TestSendAndReceiveTransacted()
        {
            this.template.ChannelTransacted = true;
            this.template.ConvertAndSend(ROUTE, "message");
            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send and receive transacted with uncached connection.
        /// </summary>
        [Test]
        public void TestSendAndReceiveTransactedWithUncachedConnection()
        {
            var template = new RabbitTemplate(new SingleConnectionFactory());
            template.ChannelTransacted = true;
            template.ConvertAndSend(ROUTE, "message");
            var result = (string)template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send and receive transacted with implicit rollback.
        /// </summary>
        [Test]
        public void TestSendAndReceiveTransactedWithImplicitRollback()
        {
            this.template.ChannelTransacted = true;
            this.template.ConvertAndSend(ROUTE, "message");

            // Rollback of manual receive is implicit because the channel is
            // closed...
            try
            {
                this.template.Execute<string>(
                    delegate(IModel channel)
                    {
                        // Switch off the auto-ack so the message is rolled back...
                        channel.BasicGet(ROUTE, false);

                        // This is the way to rollback with a cached channel (it is
                        // the way the ConnectionFactoryUtils
                        // handles it via a synchronization):
                        channel.BasicRecover(true);
                        throw new PlannedException();
                    });
                Assert.Fail("Expected PlannedException");
            }
            catch (Exception e)
            {
                Assert.True(e.InnerException is PlannedException);
            }

            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send and receive in callback.
        /// </summary>
        [Test]
        public void TestSendAndReceiveInCallback()
        {
            this.template.ConvertAndSend(ROUTE, "message");
            var messagePropertiesConverter = new DefaultMessagePropertiesConverter();
            var result = this.template.Execute(
                delegate(IModel channel)
                {
                    // We need noAck=false here for the message to be expicitly
                    // acked
                    var response = channel.BasicGet(ROUTE, false);
                    var messageProps = messagePropertiesConverter.ToMessageProperties(response.BasicProperties, response, "UTF-8");

                    // Explicit ack
                    channel.BasicAck(response.DeliveryTag, false);
                    return (string)new SimpleMessageConverter().FromMessage(new Message(response.Body, messageProps));
                });
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the receive in external transaction.
        /// </summary>
        [Test]
        public void TestReceiveInExternalTransaction()
        {
            var mockCallback = new Mock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() => (string)this.template.ReceiveAndConvert(ROUTE));

            this.template.ConvertAndSend(ROUTE, "message");
            this.template.ChannelTransacted = true;
            var result = (string)new TransactionTemplate(new TestTransactionManager()).Execute(mockCallback.Object);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the receive in external transaction auto ack.
        /// </summary>
        [Test]
        public void TestReceiveInExternalTransactionAutoAck()
        {
            var mockCallback = new Mock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() => (string)this.template.ReceiveAndConvert(ROUTE));

            this.template.ConvertAndSend(ROUTE, "message");

            // Should just result in auto-ack (not synched with external tx)
            this.template.ChannelTransacted = true;
            var result = (string)new TransactionTemplate(new TestTransactionManager()).Execute(mockCallback.Object);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the receive in external transaction with rollback.
        /// </summary>
        [Test]
        public void TestReceiveInExternalTransactionWithRollback()
        {
            var mockCallback = new Mock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(
                () =>
                {
                    this.template.ReceiveAndConvert(ROUTE);
                    throw new PlannedException();
                });

            // Makes receive (and send in principle) transactional
            this.template.ChannelTransacted = true;
            this.template.ConvertAndSend(ROUTE, "message");
            try
            {
                new TransactionTemplate(new TestTransactionManager()).Execute(mockCallback.Object);
                Assert.Fail("Expected PlannedException");
            }
            catch (PlannedException e)
            {
                // Expected
            }

            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the receive in external transaction with no rollback.
        /// </summary>
        [Test]
        public void TestReceiveInExternalTransactionWithNoRollback()
        {
            var mockCallback = new Mock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(
                () =>
                {
                    this.template.ReceiveAndConvert(ROUTE);
                    throw new PlannedException();
                });

            // Makes receive non-transactional
            this.template.ChannelTransacted = false;
            this.template.ConvertAndSend(ROUTE, "message");
            try
            {
                new TransactionTemplate(new TestTransactionManager()).Execute(mockCallback.Object);
                Assert.Fail("Expected PlannedException");
            }
            catch (PlannedException e)
            {
                // Expected
            }

            // No rollback
            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send in external transaction.
        /// </summary>
        [Test]
        public void TestSendInExternalTransaction()
        {
            var mockCallback = new Mock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(
                () =>
                {
                    this.template.ConvertAndSend(ROUTE, "message");
                    return null;
                });

            this.template.ChannelTransacted = true;
            new TransactionTemplate(new TestTransactionManager()).Execute(mockCallback.Object);
            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send in external transaction with rollback.
        /// </summary>
        [Test]
        public void TestSendInExternalTransactionWithRollback()
        {
            var mockCallback = new Mock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(
                () =>
                {
                    this.template.ConvertAndSend(ROUTE, "message");
                    throw new PlannedException();
                });
            this.template.ChannelTransacted = true;
            try
            {
                new TransactionTemplate(new TestTransactionManager()).Execute(mockCallback.Object);
                Assert.Fail("Expected PlannedException");
            }
            catch (PlannedException e)
            {
                // Expected
            }

            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the atomic send and receive.
        /// </summary>
        [Test]
        public void TestAtomicSendAndReceive()
        {
            var template = new RabbitTemplate(new CachingConnectionFactory());
            template.RoutingKey = ROUTE;
            template.Queue = ROUTE;

            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(
                () =>
                {
                    Message insidemessage = null;
                    for (int i = 0; i < 10; i++)
                    {
                        insidemessage = template.Receive();
                        if (insidemessage != null)
                        {
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    Assert.IsNotNull(insidemessage, "No message received");
                    template.Send(insidemessage.MessageProperties.ReplyTo, insidemessage);
                    return insidemessage;
                });

            var message = new Message(Encoding.UTF8.GetBytes("test-message"), new MessageProperties());
            var reply = template.SendAndReceive(message);
            var success = received.Wait(1000);
            if (!success)
            {
                Assert.Fail("Timed out receiving the message.");
            }

            Assert.AreEqual(Encoding.UTF8.GetString(message.Body), Encoding.UTF8.GetString(received.Result.Body));
            Assert.IsNotNull(reply, "Reply is expected");
            Assert.AreEqual(Encoding.UTF8.GetString(message.Body), Encoding.UTF8.GetString(reply.Body));

            // Message was consumed so nothing left on queue
            reply = template.Receive();
            Assert.AreEqual(null, reply);
        }

        /*
        
        Not applicable as there is no .NET Executor equivalent
        
        [Test]
        public void testAtomicSendAndReceiveExternalExecutor() 
        {
            var connectionFactory = new CachingConnectionFactory();
            ThreadPoolTaskExecutor exec = new ThreadPoolTaskExecutor();
            var execName = "make-sure-exec-passed-in";
            exec.setBeanName(execName);
            exec.afterPropertiesSet();
            connectionFactory.setExecutor(exec);
            final Field[] fields = new Field[1];
            ReflectionUtils.doWithFields(RabbitTemplate.class, new FieldCallback() {
                public void doWith(Field field) throws IllegalArgumentException,
                        IllegalAccessException {
                    field.setAccessible(true);
                    fields[0] = field;
                }
            }, new FieldFilter() {
                public boolean matches(Field field) {
                    return field.getName().equals("logger");
                }
            });
            Log logger = Mockito.mock(Log.class);
            when(logger.isTraceEnabled()).thenReturn(true);
            
            final AtomicBoolean execConfiguredOk = new AtomicBoolean();
            
            doAnswer(new Answer<Object>(){
                public Object answer(InvocationOnMock invocation) throws Throwable {
                    String log = (String) invocation.getArguments()[0];
                    if (log.startsWith("Message received") &&
                            Thread.currentThread().getName().startsWith(execName)) {
                        execConfiguredOk.set(true);
                    }
                    return null;
                }
            }).when(logger).trace(Mockito.anyString());
            final RabbitTemplate template = new RabbitTemplate(connectionFactory);
            ReflectionUtils.setField(fields[0], template, logger);
            template.setRoutingKey(ROUTE);
            template.setQueue(ROUTE);
            ExecutorService executor = Executors.newFixedThreadPool(1);
            // Set up a consumer to respond to our producer
            Future<Message> received = executor.submit(new Callable<Message>() {
            
                public Message call() throws Exception {
                    Message message = null;
                    for (int i = 0; i < 10; i++) {
                        message = template.receive();
                        if (message != null) {
                            break;
                        }
                        Thread.sleep(100L);
                    }
                    assertNotNull("No message received", message);
                    template.send(message.getMessageProperties().getReplyTo(), message);
                    return message;
                }
            
            });
            Message message = new Message("test-message".getBytes(), new MessageProperties());
            Message reply = template.sendAndReceive(message);
            assertEquals(new String(message.getBody()), new String(received.get(1000, TimeUnit.MILLISECONDS).getBody()));
            assertNotNull("Reply is expected", reply);
            assertEquals(new String(message.getBody()), new String(reply.getBody()));
            // Message was consumed so nothing left on queue
            reply = template.receive();
            assertEquals(null, reply);
            
            assertTrue(execConfiguredOk.get());
        }
        */

        /// <summary>
        /// Tests the atomic send and receive with routing key.
        /// </summary>
        [Test]
        public void TestAtomicSendAndReceiveWithRoutingKey()
        {
            var template = new RabbitTemplate(new CachingConnectionFactory());

            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(
                () =>
                {
                    Message internalmessage = null;
                    for (int i = 0; i < 10; i++)
                    {
                        internalmessage = template.Receive(ROUTE);
                        if (internalmessage != null)
                        {
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    Assert.IsNotNull(internalmessage, "No message received");
                    template.Send(internalmessage.MessageProperties.ReplyTo, internalmessage);
                    return internalmessage;
                });

            var message = new Message(Encoding.UTF8.GetBytes("test-message"), new MessageProperties());
            var reply = template.SendAndReceive(ROUTE, message);
            var success = received.Wait(1000);
            if (!success)
            {
                Assert.Fail("Timed out receiving the message.");
            }

            Assert.AreEqual(Encoding.UTF8.GetString(message.Body), Encoding.UTF8.GetString(received.Result.Body));
            Assert.IsNotNull(reply, "Reply is expected");
            Assert.AreEqual(Encoding.UTF8.GetString(message.Body), Encoding.UTF8.GetString(reply.Body));

            // Message was consumed so nothing left on queue
            reply = template.Receive(ROUTE);
            Assert.AreEqual(null, reply);
        }

        /// <summary>
        /// Tests the atomic send and receive with exchange and routing key.
        /// </summary>
        [Test]
        public void TestAtomicSendAndReceiveWithExchangeAndRoutingKey()
        {
            var template = new RabbitTemplate(new CachingConnectionFactory());

            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(
                () =>
                {
                    Message internalmessage = null;
                    for (var i = 0; i < 10; i++)
                    {
                        internalmessage = template.Receive(ROUTE);
                        if (internalmessage != null)
                        {
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    Assert.IsNotNull(internalmessage, "No message received");
                    template.Send(internalmessage.MessageProperties.ReplyTo, internalmessage);
                    return internalmessage;
                });
            var message = new Message(Encoding.UTF8.GetBytes("test-message"), new MessageProperties());
            var reply = template.SendAndReceive(string.Empty, ROUTE, message);
            var success = received.Wait(1000);
            if (!success)
            {
                Assert.Fail("Timed out receiving the message.");
            }

            Assert.AreEqual(Encoding.UTF8.GetString(message.Body), Encoding.UTF8.GetString(received.Result.Body));
            Assert.IsNotNull(reply, "Reply is expected");
            Assert.AreEqual(Encoding.UTF8.GetString(message.Body), Encoding.UTF8.GetString(reply.Body));

            // Message was consumed so nothing left on queue
            reply = template.Receive(ROUTE);
            Assert.AreEqual(null, reply);
        }

        /// <summary>
        /// Tests the atomic send and receive with conversion.
        /// </summary>
        [Test]
        public void TestAtomicSendAndReceiveWithConversion()
        {
            var template = new RabbitTemplate(new CachingConnectionFactory());
            template.RoutingKey = ROUTE;
            template.Queue = ROUTE;

            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(
                () =>
                {
                    Message message = null;
                    for (var i = 0; i < 10; i++)
                    {
                        message = template.Receive();
                        if (message != null)
                        {
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    Assert.IsNotNull(message, "No message received");
                    template.Send(message.MessageProperties.ReplyTo, message);
                    return (string)template.MessageConverter.FromMessage(message);
                });
            var result = (string)template.ConvertSendAndReceive("message");
            var success = received.Wait(1000);
            if (!success)
            {
                Assert.Fail("Timed out receiving the message.");
            }

            Assert.AreEqual("message", received.Result);
            Assert.AreEqual("message", result);

            // Message was consumed so nothing left on queue
            result = (string)template.ReceiveAndConvert();
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the atomic send and receive with conversion using routing key.
        /// </summary>
        [Test]
        public void TestAtomicSendAndReceiveWithConversionUsingRoutingKey()
        {
            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(
                () =>
                {
                    Message message = null;
                    for (var i = 0; i < 10; i++)
                    {
                        message = this.template.Receive(ROUTE);
                        if (message != null)
                        {
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    Assert.IsNotNull(message, "No message received");
                    this.template.Send(message.MessageProperties.ReplyTo, message);
                    return (string)this.template.MessageConverter.FromMessage(message);
                });

            var result = (string)this.template.ConvertSendAndReceive(ROUTE, "message");
            var success = received.Wait(1000);
            if (!success)
            {
                Assert.Fail("Timed out receiving the message.");
            }

            Assert.AreEqual("message", received.Result);
            Assert.AreEqual("message", result);

            // Message was consumed so nothing left on queue
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the atomic send and receive with conversion using exchange and routing key.
        /// </summary>
        [Test]
        public void TestAtomicSendAndReceiveWithConversionUsingExchangeAndRoutingKey()
        {
            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(
                () =>
                {
                    Message message = null;
                    for (var i = 0; i < 10; i++)
                    {
                        message = this.template.Receive(ROUTE);
                        if (message != null)
                        {
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    Assert.IsNotNull(message, "No message received");
                    this.template.Send(message.MessageProperties.ReplyTo, message);
                    return (string)this.template.MessageConverter.FromMessage(message);
                });
            var result = (string)this.template.ConvertSendAndReceive(string.Empty, ROUTE, "message");
            var success = received.Wait(1000);
            if (!success)
            {
                Assert.Fail("Timed out receiving the message.");
            }

            Assert.AreEqual("message", received.Result);
            Assert.AreEqual("message", result);

            // Message was consumed so nothing left on queue
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        [Test]
        public void TestAtomicSendAndReceiveWithConversionAndMessagePostProcessor()
        {
            var template = new RabbitTemplate(new CachingConnectionFactory());
            template.RoutingKey = ROUTE;
            template.Queue = ROUTE;
            // ExecutorService executor = Executors.newFixedThreadPool(1);
            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(
                () =>
                {
                    Message message = null;
                    for (var i = 0; i < 10; i++)
                    {
                        message = template.Receive();
                        if (message != null)
                        {
                            break;
                        }
                        Thread.Sleep(100);
                    }
                    Assert.IsNotNull(message, "No message received");
                    template.Send(message.MessageProperties.ReplyTo, message);
                    return (string)template.MessageConverter.FromMessage(message);
                });

            var result = (string)template.ConvertSendAndReceive(
                (object)"message",
                message =>
                {
                    try
                    {
                        byte[] newBody = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(message.Body).ToUpper());
                        return new Message(newBody, message.MessageProperties);
                    }
                    catch (Exception e)
                    {
                        throw new AmqpException("unexpected failure in test", e);
                    }
                });
            var success = received.Wait(1000);
            if (!success)
            {
                Assert.Fail("Timed out receiving the message.");
            }

            Assert.AreEqual("MESSAGE", received.Result);
            Assert.AreEqual("MESSAGE", result);
            // Message was consumed so nothing left on queue
            result = (string)template.ReceiveAndConvert();
            Assert.AreEqual(null, result);
        }

        [Test]
        public void TestAtomicSendAndReceiveWithConversionAndMessagePostProcessorUsingRoutingKey()
        {
            // ExecutorService executor = Executors.newFixedThreadPool(1);
            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(
                () =>
                {
                    Message message = null;
                    for (var i = 0; i < 10; i++)
                    {
                        message = template.Receive(ROUTE);
                        if (message != null)
                        {
                            break;
                        }
                        Thread.Sleep(100);
                    }
                    Assert.IsNotNull(message, "No message received");
                    template.Send(message.MessageProperties.ReplyTo, message);
                    return (string)template.MessageConverter.FromMessage(message);
                });

            var result = (string)template.ConvertSendAndReceive(
                ROUTE,
                (object)"message",
                message =>
                {
                    try
                    {
                        byte[] newBody = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(message.Body).ToUpper());
                        return new Message(newBody, message.MessageProperties);
                    }
                    catch (Exception e)
                    {
                        throw new AmqpException("unexpected failure in test", e);
                    }
                });
            var success = received.Wait(1000);
            if (!success)
            {
                Assert.Fail("Timed out receiving the message.");
            }

            Assert.AreEqual("MESSAGE", received.Result);
            Assert.AreEqual("MESSAGE", result);
            // Message was consumed so nothing left on queue
            result = (string)template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        [Test]
        public void TestAtomicSendAndReceiveWithConversionAndMessagePostProcessorUsingExchangeAndRoutingKey()
        {
            // ExecutorService executor = Executors.newFixedThreadPool(1);
            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(
                () =>
                {
                    Message message = null;
                    for (var i = 0; i < 10; i++)
                    {
                        message = template.Receive(ROUTE);
                        if (message != null)
                        {
                            break;
                        }
                        Thread.Sleep(100);
                    }
                    Assert.IsNotNull(message, "No message received");
                    template.Send(message.MessageProperties.ReplyTo, message);
                    return (string)template.MessageConverter.FromMessage(message);
                });

            var result = (string)template.ConvertSendAndReceive(
                ROUTE,
                (object)"message",
                message =>
                {
                    try
                    {
                        byte[] newBody = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(message.Body).ToUpper());
                        return new Message(newBody, message.MessageProperties);
                    }
                    catch (Exception e)
                    {
                        throw new AmqpException("unexpected failure in test", e);
                    }
                });
            var success = received.Wait(1000);
            if (!success)
            {
                Assert.Fail("Timed out receiving the message.");
            }

            Assert.AreEqual("MESSAGE", received.Result);
            Assert.AreEqual("MESSAGE", result);
            // Message was consumed so nothing left on queue
            result = (String)template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }
    }

    /// <summary>
    /// A planned exception.
    /// </summary>
    internal class PlannedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlannedException"/> class. 
        /// </summary>
        /// <remarks>
        /// </remarks>
        public PlannedException() : base("Planned") { }
    }

    /// <summary>
    /// A test transaction manager.
    /// </summary>
    internal class TestTransactionManager : AbstractPlatformTransactionManager
    {
        /// <summary>Begin a new transaction with the given transaction definition.</summary>
        /// <param name="transaction">Transaction object returned by<see cref="M:Spring.Transaction.Support.AbstractPlatformTransactionManager.DoGetTransaction"/>.</param>
        /// <param name="definition"><see cref="T:Spring.Transaction.ITransactionDefinition"/> instance, describing
        /// propagation behavior, isolation level, timeout etc.</param>
        /// <exception cref="T:Spring.Transaction.TransactionException">In the case of creation or system errors.</exception>
        protected override void DoBegin(object transaction, ITransactionDefinition definition) { }

        /// <summary>Perform an actual commit on the given transaction.</summary>
        /// <param name="status">The status representation of the transaction.</param>
        /// <exception cref="T:Spring.Transaction.TransactionException">In the case of system errors.</exception>
        protected override void DoCommit(DefaultTransactionStatus status) { }

        /// <summary>
        /// Return the current transaction object.
        /// </summary>
        /// <returns>The current transaction object.</returns>
        /// <exception cref="T:Spring.Transaction.CannotCreateTransactionException">
        /// If transaction support is not available.
        ///   </exception>
        /// <exception cref="T:Spring.Transaction.TransactionException">
        /// In the case of lookup or system errors.
        ///   </exception>
        protected override object DoGetTransaction() { return new object(); }

        /// <summary>Perform an actual rollback on the given transaction.</summary>
        /// <param name="status">The status representation of the transaction.</param>
        /// <exception cref="T:Spring.Transaction.TransactionException">In the case of system errors.</exception>
        protected override void DoRollback(DefaultTransactionStatus status) { }
    }

    /// <summary>Test transaction callback.</summary>
    /// <typeparam name="T">Type T.</typeparam>
    public class TestTransactionCallback<T> : ITransactionCallback
    {
        #region Implementation of ITransactionCallback

        /// <summary>Gets called by TransactionTemplate.Execute within a 
        ///             transaction context.</summary>
        /// <param name="status">The associated transaction status.</param>
        /// <returns>A result object or <c>null</c>.</returns>
        public object DoInTransaction(ITransactionStatus status) { throw new NotImplementedException(); }
        #endregion
    }
}
