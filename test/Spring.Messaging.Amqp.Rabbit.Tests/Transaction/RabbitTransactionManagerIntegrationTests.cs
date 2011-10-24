using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using AutoMoq;
using Moq;
using NUnit.Framework;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Transaction;
using Spring.Transaction;
using Spring.Transaction.Support;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Transaction
{
    /// <summary>
    /// Rabbit Transaction Manager Integration Tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class RabbitTransactionManagerIntegrationTests : AbstractRabbitIntegrationTest
    {
        /// <summary>
        /// The route.
        /// </summary>
        private static readonly string ROUTE = "test.queue";

        /// <summary>
        /// The template.
        /// </summary>
        private RabbitTemplate template;

        /// <summary>
        /// The transaction template.
        /// </summary>
        private TransactionTemplate transactionTemplate;
        
        #region Fixture Setup and Teardown
        /// <summary>
        /// Code to execute before fixture setup.
        /// </summary>
        public override void BeforeFixtureSetUp()
        {
        }

        /// <summary>
        /// Code to execute before fixture teardown.
        /// </summary>
        public override void BeforeFixtureTearDown()
        {
        }

        /// <summary>
        /// Code to execute after fixture setup.
        /// </summary>
        public override void AfterFixtureSetUp()
        {
        }

        /// <summary>
        /// Code to execute after fixture teardown.
        /// </summary>
        public override void AfterFixtureTearDown()
        {
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitTransactionManagerIntegrationTests"/> class. 
        /// </summary>
        /// <remarks>
        /// </remarks>
        public RabbitTransactionManagerIntegrationTests()
        {
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(ROUTE);
            this.brokerIsRunning.Apply();
        }

        /// <summary>
        /// Inits this instance.
        /// </summary>
        /// <remarks></remarks>
        [SetUp]
        public void Init()
        {
            var connectionFactory = new CachingConnectionFactory();
            this.template = new RabbitTemplate(connectionFactory);
            this.template.IsChannelTransacted = true;
            var transactionManager = new RabbitTransactionManager(connectionFactory);
            this.transactionTemplate = new TransactionTemplate(transactionManager);
            this.transactionTemplate.TransactionIsolationLevel = IsolationLevel.Unspecified;
        }

        /// <summary>
        /// Tests the send and receive in transaction.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveInTransaction()
        {
            var mocker = new AutoMoqer();

            var mockCallback = mocker.GetMock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() =>
                                                                                                   {
                                                                                                       this.template.ConvertAndSend(ROUTE, "message");
                                                                                                       return (string)this.template.ReceiveAndConvert(ROUTE);
                                                                                                   });
            var result = (string)this.transactionTemplate.Execute(mockCallback.Object);

            Assert.AreEqual(null, result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
        }

        /// <summary>
        /// Tests the receive in transaction.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestReceiveInTransaction()
        {
            var mocker = new AutoMoqer();

            var mockCallback = mocker.GetMock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() => (string)this.template.ReceiveAndConvert(ROUTE));
            this.template.ConvertAndSend(ROUTE, "message");
            var result = (string)this.transactionTemplate.Execute(mockCallback.Object);

            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the receive in transaction with rollback.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestReceiveInTransactionWithRollback()
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
                var internalresult = (string)this.transactionTemplate.Execute(mockCallback.Object);
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
        /// Tests the send in transaction.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendInTransaction()
        {
            var mocker = new AutoMoqer();

            var mockCallback = mocker.GetMock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() =>
                                                                                                   {
                                                                                                       template.ConvertAndSend(ROUTE, "message");
                                                                                                       return null;
                                                                                                   });
            this.template.IsChannelTransacted = true;
            this.transactionTemplate.Execute(mockCallback.Object);
            var result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual("message", result);
            result = (string)this.template.ReceiveAndConvert(ROUTE);
            Assert.AreEqual(null, result);
        }

        /// <summary>
        /// Tests the send in transaction with rollback.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendInTransactionWithRollback()
        {
            var mocker = new AutoMoqer();

            var mockCallback = mocker.GetMock<ITransactionCallback>();
            mockCallback.Setup(c => c.DoInTransaction(It.IsAny<ITransactionStatus>())).Returns(() =>
                                                                                                   {
                                                                                                       template.ConvertAndSend(ROUTE, "message");
                                                                                                       throw new PlannedException();
                                                                                                   });
            this.template.IsChannelTransacted = true;
            try
            {
                this.transactionTemplate.Execute(mockCallback.Object);
                Assert.Fail("Expected PlannedException");
            }
            catch (PlannedException e)
            {
                // Expected
            }

            var result = (string)this.template.ReceiveAndConvert(ROUTE);
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
}
