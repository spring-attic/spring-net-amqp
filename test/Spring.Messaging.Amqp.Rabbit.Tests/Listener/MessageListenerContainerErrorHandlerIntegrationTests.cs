using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AutoMoq;
using Common.Logging;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Test;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    /// Message listener container error handler integration tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class MessageListenerContainerErrorHandlerIntegrationTests : AbstractRabbitIntegrationTest
    {
        private static ILog logger = LogManager.GetLogger(typeof(MessageListenerContainerErrorHandlerIntegrationTests));

        private static Queue queue = new Queue("test.queue");

        // Mock error handler
        private Mock<IErrorHandler> errorHandler;

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

        //@Rule
        //public Log4jLevelAdjuster logLevels = new Log4jLevelAdjuster(Level.INFO, RabbitTemplate.class,
        //		SimpleMessageListenerContainer.class, BlockingQueueConsumer.class,
        //		MessageListenerContainerErrorHandlerIntegrationTests.class);
        
        /// <summary>
        /// Sets up.
        /// </summary>
        /// <remarks></remarks>
        [SetUp]
        public void SetUp()
        {
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue);
            this.brokerIsRunning.Apply();

            var mockErrorHandler = new Mock<IErrorHandler>();
            this.errorHandler = mockErrorHandler;
        }

        /// <summary>
        /// Tests the error handler invoke exception from poco.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestErrorHandlerInvokeExceptionFromPoco()
        {
            var messageCount = 3;
            var latch = new CountdownEvent(messageCount);
            this.DoTest(messageCount, this.errorHandler.Object, latch, new MessageListenerAdapter(new PocoThrowingExceptionListener(latch, new Exception("Pojo exception"))));

            // Verify that error handler was invoked
            this.errorHandler.Verify(h => h.HandleError(It.IsAny<Exception>()), Times.Exactly(messageCount));
        }

        /// <summary>
        /// Tests the error handler invoke runtime exception from poco.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestErrorHandlerInvokeRuntimeExceptionFromPoco()
        {
            var messageCount = 3;
            var latch = new CountdownEvent(messageCount);
            this.DoTest(messageCount, this.errorHandler.Object, latch, new MessageListenerAdapter(new PocoThrowingExceptionListener(latch, new Exception("Pojo runtime exception"))));

            // Verify that error handler was invoked
            this.errorHandler.Verify(h => h.HandleError(It.IsAny<Exception>()), Times.Exactly(messageCount));
        }

        /// <summary>
        /// Tests the error handler listener execution failed exception from listener.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestErrorHandlerListenerExecutionFailedExceptionFromListener()
        {
            var messageCount = 3;
            var latch = new CountdownEvent(messageCount);
            this.DoTest(messageCount, this.errorHandler.Object, latch, new ThrowingExceptionListener(latch, new ListenerExecutionFailedException("Listener throws specific runtime exception", null)));

            // Verify that error handler was invoked
            this.errorHandler.Verify(h => h.HandleError(It.IsAny<Exception>()), Times.Exactly(messageCount));
        }

        /// <summary>
        /// Tests the error handler regular runtime exception from listener.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestErrorHandlerRegularRuntimeExceptionFromListener()
        {
            var messageCount = 3;
            var latch = new CountdownEvent(messageCount);
            this.DoTest(messageCount, this.errorHandler.Object, latch, new ThrowingExceptionListener(latch, new Exception("Listener runtime exception")));

            // Verify that error handler was invoked
            this.errorHandler.Verify(h => h.HandleError(It.IsAny<Exception>()), Times.Exactly(messageCount));
        }

        /// <summary>
        /// Tests the error handler invoke exception from channel aware listener.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestErrorHandlerInvokeExceptionFromChannelAwareListener()
        {
            var messageCount = 3;
            var latch = new CountdownEvent(messageCount);
            this.DoTest(messageCount, this.errorHandler.Object, latch, new ThrowingExceptionChannelAwareListener(latch, new Exception("Channel aware listener exception")));

            // Verify that error handler was invoked
            this.errorHandler.Verify(h => h.HandleError(It.IsAny<Exception>()), Times.Exactly(messageCount));
        }

        /// <summary>
        /// Tests the error handler invoke runtime exception from channel aware listener.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestErrorHandlerInvokeRuntimeExceptionFromChannelAwareListener()
        {
            var messageCount = 3;
            var latch = new CountdownEvent(messageCount);
            this.DoTest(messageCount, this.errorHandler.Object, latch, new ThrowingExceptionChannelAwareListener(latch, new Exception("Channel aware listener runtime exception")));

            // Verify that error handler was invoked
            this.errorHandler.Verify(h => h.HandleError(It.IsAny<Exception>()), Times.Exactly(messageCount));
        }

        /// <summary>
        /// Does the test.
        /// </summary>
        /// <param name="messageCount">The message count.</param>
        /// <param name="errorHandler">The error handler.</param>
        /// <param name="latch">The latch.</param>
        /// <param name="listener">The listener.</param>
        /// <remarks></remarks>
        public void DoTest(int messageCount, IErrorHandler errorHandler, CountdownEvent latch, object listener)
        {
            var concurrentConsumers = 1;
            var template = this.CreateTemplate(concurrentConsumers);

            // Send messages to the queue
            for (var i = 0; i < messageCount; i++)
            {
                template.ConvertAndSend(queue.Name, i + "foo");
            }

            var container = new SimpleMessageListenerContainer(template.ConnectionFactory);
            container.MessageListener = listener;
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.NONE;
            container.IsChannelTransacted = false;
            container.ConcurrentConsumers = concurrentConsumers;

            container.PrefetchCount = messageCount;
            container.TxSize = messageCount;
            container.QueueNames = new string[] { queue.Name };
            container.ErrorHandler = errorHandler;
            container.AfterPropertiesSet();
            container.Start();

            var waited = latch.Wait(1000);
            if (messageCount > 1)
            {
                Assert.True(waited, "Expected to receive all messages before stop");
            }

            try
            {
                Assert.Null(template.ReceiveAndConvert(queue.Name));
            }
            finally
            {
                container.Shutdown();
            }
        }

        /// <summary>
        /// Creates the template.
        /// </summary>
        /// <param name="concurrentConsumers">The concurrent consumers.</param>
        /// <returns>The template.</returns>
        /// <remarks></remarks>
        private RabbitTemplate CreateTemplate(int concurrentConsumers)
        {
            var template = new RabbitTemplate();

            // AbstractConnectionFactory connectionFactory = new AbstractConnectionFactory();
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.ChannelCacheSize = concurrentConsumers;
            connectionFactory.Port = BrokerTestUtils.GetPort();
            template.ConnectionFactory = connectionFactory;
            return template;
        }
    }

    /// <summary>
    /// A POCO throwing exception listener.
    /// </summary>
    /// <remarks></remarks>
    public class PocoThrowingExceptionListener
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PocoThrowingExceptionListener));
        private CountdownEvent latch;
        private Exception exception;

        /// <summary>
        /// Initializes a new instance of the <see cref="PocoThrowingExceptionListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <param name="exception">The exception.</param>
        /// <remarks></remarks>
        public PocoThrowingExceptionListener(CountdownEvent latch, Exception exception)
        {
            this.latch = latch;
            this.exception = exception;
        }

        /// <summary>
        /// Handles the message.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks></remarks>
        public void HandleMessage(string value)
        {
            try
            {
                Logger.Debug("Message in poco: " + value);
                Thread.Sleep(100);
                throw this.exception;
            }
            finally
            {
                Logger.Info("Latch Current Count: " + this.latch.CurrentCount);
                this.latch.Signal();
            }
        }
    }

    /// <summary>
    /// A throwing exception listener.
    /// </summary>
    /// <remarks></remarks>
    public class ThrowingExceptionListener : IMessageListener
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ThrowingExceptionListener));
        private CountdownEvent latch;
        private Exception exception;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrowingExceptionListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <param name="exception">The exception.</param>
        /// <remarks></remarks>
        public ThrowingExceptionListener(CountdownEvent latch, Exception exception)
        {
            this.latch = latch;
            this.exception = exception;
        }

        /// <summary>
        /// Called when a Message is received.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <remarks></remarks>
        public void OnMessage(Message message)
        {
            try
            {
                var value = Encoding.UTF8.GetString(message.Body);
                Logger.Debug("Message in listener: " + value);
                try
                {
                    Thread.Sleep(100);
                }
                catch (ThreadInterruptedException e)
                {
                    // Ignore this exception
                }

                throw this.exception;
            }
            finally
            {
                Logger.Info("Latch Current Count: " + this.latch.CurrentCount);
                this.latch.Signal();
            }
        }
    }

    /// <summary>
    /// A throwing exception channel aware listener.
    /// </summary>
    /// <remarks></remarks>
    public class ThrowingExceptionChannelAwareListener : IChannelAwareMessageListener
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ThrowingExceptionChannelAwareListener));
        private CountdownEvent latch;
        private Exception exception;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrowingExceptionChannelAwareListener"/> class.
        /// </summary>
        /// <param name="latch">The latch.</param>
        /// <param name="exception">The exception.</param>
        /// <remarks></remarks>
        public ThrowingExceptionChannelAwareListener(CountdownEvent latch, Exception exception)
        {
            this.latch = latch;
            this.exception = exception;
        }

        /// <summary>
        /// Called when [message].
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channel">The channel.</param>
        /// <remarks></remarks>
        public void OnMessage(Message message, IModel channel)
        {
            try
            {
                var value = Encoding.UTF8.GetString(message.Body);
                Logger.Debug("Message in channel aware listener: " + value);
                try
                {
                    Thread.Sleep(100);
                }
                catch (ThreadInterruptedException e)
                {
                    // Ignore this exception
                }

                throw this.exception;
            }
            finally
            {
                Logger.Info("Latch Current Count: " + this.latch.CurrentCount);
                this.latch.Signal();
            }
        }
    }
}
