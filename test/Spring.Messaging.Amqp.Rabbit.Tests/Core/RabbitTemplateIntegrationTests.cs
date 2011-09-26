
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMoq;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Admin;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Test;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Transaction;
using Spring.Transaction.Support;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// Rabbit template integration tests.
    /// </summary>
    /// <remarks></remarks>
    public class RabbitTemplateIntegrationTests
    {
        /// <summary>
        /// The test route.
        /// </summary>
        private static readonly string ROUTE = "test.queue";

        /// <summary>
        /// The rabbit template.
        /// </summary>
        private RabbitTemplate template;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            var brokerAdmin = new RabbitBrokerAdmin();
            brokerAdmin.StartupTimeout = 10000;
            brokerAdmin.StartBrokerApplication();
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(ROUTE);
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            var brokerAdmin = new RabbitBrokerAdmin();
            brokerAdmin.StopBrokerApplication();
            brokerAdmin.StopNode();
        }

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <remarks></remarks>
        [SetUp]
        public void Create()
        {
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.Port = BrokerTestUtils.GetPort();
            this.template = new RabbitTemplate(connectionFactory);
        }

        /// <summary>
        /// The broker running.
        /// </summary>
        private BrokerRunning brokerIsRunning;

        /// <summary>
        /// Tests the send to non existent and then receive.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendToNonExistentAndThenReceive()
        {
            // If transacted then the commit fails on send, so we get a nice synchronous exception
            this.template.IsChannelTransacted = true;
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
        /// Tests the send and receive.
        /// </summary>
        /// <remarks></remarks>
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
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveTransacted()
        {
            this.template.IsChannelTransacted = true;
            this.template.ConvertAndSend(ROUTE, "message");
            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send and receive transacted with uncached connection.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveTransactedWithUncachedConnection()
        {
            var template = new RabbitTemplate(new AbstractConnectionFactory());
            template.IsChannelTransacted = true;
            template.ConvertAndSend(ROUTE, "message");
            var result = (string)template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send and receive transacted with implicit rollback.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveTransactedWithImplicitRollback()
        {
            this.template.IsChannelTransacted = true;
            this.template.ConvertAndSend(ROUTE, "message");

            // Rollback of manual receive is implicit because the channel is
            // closed...
            try
            {
                this.template.Execute<string>(delegate(IModel channel)
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
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveInCallback()
        {
            this.template.ConvertAndSend(ROUTE, "message");
            var result = this.template.Execute<string>(delegate(IModel channel)
            {
                // We need noAck=false here for the message to be expicitly
                // acked
                var response = channel.BasicGet(ROUTE, false);
                var messageProps = RabbitUtils.CreateMessageProperties(response.BasicProperties, response, "UTF-8");
                
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
        /// <remarks></remarks>
        [Test]
        public void TestReceiveInExternalTransaction()
        {
            var mocker = new AutoMoqer();

            var mockCallback = mocker.GetMock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() => (string)this.template.ReceiveAndConvert(ROUTE));
            
            this.template.ConvertAndSend(ROUTE, "message");
            this.template.IsChannelTransacted = true;
            var result = (string)new TransactionTemplate(new TestTransactionManager()).Execute(mockCallback.Object);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the receive in external transaction auto ack.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestReceiveInExternalTransactionAutoAck()
        {
            var mocker = new AutoMoqer();

            var mockCallback = mocker.GetMock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() => (string)this.template.ReceiveAndConvert(ROUTE));

            this.template.ConvertAndSend(ROUTE, "message");

            // Should just result in auto-ack (not synched with external tx)
            this.template.IsChannelTransacted = true;
            var result = (string)new TransactionTemplate(new TestTransactionManager()).Execute(mockCallback.Object);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }


        /// <summary>
        /// Tests the receive in external transaction with rollback.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestReceiveInExternalTransactionWithRollback()  
        {
            var mocker = new AutoMoqer();

            var mockCallback = mocker.GetMock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() =>
                                                                                                   {
                                                                                                       this.template.ReceiveAndConvert(ROUTE);
                                                                                                       throw new PlannedException();
                                                                                                   });

            // Makes receive (and send in principle) transactional
            this.template.IsChannelTransacted = true;
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
        /// <remarks></remarks>
        [Test]
        public void TestReceiveInExternalTransactionWithNoRollback()
        {
            var mocker = new AutoMoqer();

            var mockCallback = mocker.GetMock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() =>
                                                                                                   {
                                                                                                       this.template.ReceiveAndConvert(ROUTE);
                                                                                                       throw new PlannedException();
                                                                                                   });

            // Makes receive non-transactional
            this.template.IsChannelTransacted = false;
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
        /// <remarks></remarks>
        [Test]
        public void TestSendInExternalTransaction()
        {
            var mocker = new AutoMoqer();

            var mockCallback = mocker.GetMock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() =>
                                                                                                   {
                                                                                                       this.template.ConvertAndSend(ROUTE, "message");
                                                                                                       return null;
                                                                                                   });

            this.template.IsChannelTransacted = true;
            new TransactionTemplate(new TestTransactionManager()).Execute(mockCallback.Object);
            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send in external transaction with rollback.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendInExternalTransactionWithRollback()
        {
            var mocker = new AutoMoqer();

            var mockCallback = mocker.GetMock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() =>
                                                                                                   {
                                                                                                       this.template.ConvertAndSend(ROUTE, "message");
                                                                                                       throw new PlannedException();
                                                                                                   });
            this.template.IsChannelTransacted = true;
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
        /// <remarks></remarks>
        [Test]
        public void TestAtomicSendAndReceive()  
        {
            var template = new RabbitTemplate(new CachingConnectionFactory());
            template.RoutingKey = ROUTE;
            template.Queue = ROUTE;

            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(() => 
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
                    template.Send(insidemessage.MessageProperties.ReplyTo.RoutingKey, insidemessage);
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

        /// <summary>
        /// Tests the atomic send and receive with routing key.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestAtomicSendAndReceiveWithRoutingKey()  
        {
            var template = new RabbitTemplate(new CachingConnectionFactory());
            
            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(() => 
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
                    template.Send(internalmessage.MessageProperties.ReplyTo.RoutingKey, internalmessage);
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
        /// <remarks></remarks>
        [Test]
        public void TestAtomicSendAndReceiveWithExchangeAndRoutingKey()  
        {
            var template = new RabbitTemplate(new CachingConnectionFactory());
            
            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(() => 
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
                    template.Send(internalmessage.MessageProperties.ReplyTo.RoutingKey, internalmessage);
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
        /// <remarks></remarks>
        [Test]
        public void TestAtomicSendAndReceiveWithConversion()  
        {
            var template = new RabbitTemplate(new CachingConnectionFactory());
            template.RoutingKey = ROUTE;
            template.Queue = ROUTE;
            
            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(() => 
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
                    template.Send(message.MessageProperties.ReplyTo.RoutingKey, message);
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
        /// <remarks></remarks>
        [Test]
        public void TestAtomicSendAndReceiveWithConversionUsingRoutingKey()  
        {
            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(() => 
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
                    template.Send(message.MessageProperties.ReplyTo.RoutingKey, message);
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
        /// <remarks></remarks>
        [Test]
        public void TestAtomicSendAndReceiveWithConversionUsingExchangeAndRoutingKey()  
        {
            // Set up a consumer to respond to our producer
            var received = Task.Factory.StartNew(() => 
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
                    template.Send(message.MessageProperties.ReplyTo.RoutingKey, message);
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
    }

    /// <summary>
    /// A planned exception.
    /// </summary>
    /// <remarks></remarks>
    internal class PlannedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlannedException"/> class. 
        /// </summary>
        /// <remarks>
        /// </remarks>
        public PlannedException() : base("Planned")
        {
        }
    }

    /// <summary>
    /// A test transaction manager.
    /// </summary>
    /// <remarks></remarks>
    internal class TestTransactionManager : AbstractPlatformTransactionManager
    {
        /// <summary>
        /// Begin a new transaction with the given transaction definition.
        /// </summary>
        /// <param name="transaction">Transaction object returned by
        /// <see cref="M:Spring.Transaction.Support.AbstractPlatformTransactionManager.DoGetTransaction"/>.</param>
        /// <param name="definition"><see cref="T:Spring.Transaction.ITransactionDefinition"/> instance, describing
        /// propagation behavior, isolation level, timeout etc.</param>
        /// <exception cref="T:Spring.Transaction.TransactionException">
        /// In the case of creation or system errors.
        ///   </exception>
        /// <remarks></remarks>
        protected override void DoBegin(object transaction, ITransactionDefinition definition)
        {
        }

        /// <summary>
        /// Perform an actual commit on the given transaction.
        /// </summary>
        /// <param name="status">The status representation of the transaction.</param>
        /// <exception cref="T:Spring.Transaction.TransactionException">
        /// In the case of system errors.
        ///   </exception>
        /// <remarks></remarks>
        protected override void DoCommit(DefaultTransactionStatus status)
        {
        }

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
        /// <remarks></remarks>
        protected override object DoGetTransaction()
        {
            return new object();
        }

        /// <summary>
        /// Perform an actual rollback on the given transaction.
        /// </summary>
        /// <param name="status">The status representation of the transaction.</param>
        /// <exception cref="T:Spring.Transaction.TransactionException">
        /// In the case of system errors.
        ///   </exception>
        /// <remarks></remarks>
        protected override void DoRollback(DefaultTransactionStatus status)
        {
        }
    }

    /// <summary>
    /// Test transaction callback.
    /// </summary>
    /// <typeparam name="T">Type T.</typeparam>
    /// <remarks></remarks>
    public class TestTransactionCallback<T> : ITransactionCallback
    {
        #region Implementation of ITransactionCallback

        /// <summary>
        /// Gets called by TransactionTemplate.Execute within a 
        ///             transaction context.
        /// </summary>
        /// <param name="status">The associated transaction status.</param>
        /// <returns>
        /// A result object or <c>null</c>.
        /// </returns>
        public object DoInTransaction(ITransactionStatus status)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
