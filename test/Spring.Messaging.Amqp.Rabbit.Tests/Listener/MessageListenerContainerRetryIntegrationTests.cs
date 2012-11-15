// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageListenerContainerRetryIntegrationTests.cs" company="The original author or authors.">
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
using System.Threading;
using System.Threading.Tasks;
using AopAlliance.Aop;
using Common.Logging;
using NUnit.Framework;
using Spring.Aspects;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Messaging.Amqp.Rabbit.Threading.AtomicTypes;
using Spring.Messaging.Amqp.Support.Converter;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>The message listener container retry integration tests.</summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    [Ignore("Spring.NET doesn't support retry yet...")]
    public class MessageListenerContainerRetryIntegrationTests : AbstractRabbitIntegrationTest
    {
        private new static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private static readonly Queue queue = new Queue("test.queue");

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

        // @Rule
        // public Log4jLevelAdjuster logLevels = new Log4jLevelAdjuster(Level.INFO, RabbitTemplate.class,
        // 		SimpleMessageListenerContainer.class, BlockingQueueConsumer.class);

        // @Rule
        // public ExpectedException exception = ExpectedException.none();
        private RabbitTemplate template;

        private RetryAdvice retryTemplate;

        private IMessageConverter messageConverter;

        /// <summary>The setup.</summary>
        [SetUp]
        public void Setup()
        {
            this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue);
            this.brokerIsRunning.Apply();
        }

        /// <summary>Creates the template.</summary>
        /// <param name="concurrentConsumers">The concurrent consumers.</param>
        /// <returns>The template.</returns>
        private RabbitTemplate CreateTemplate(int concurrentConsumers)
        {
            var template = new RabbitTemplate();
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.ChannelCacheSize = concurrentConsumers;
            connectionFactory.Port = BrokerTestUtils.GetPort();
            template.ConnectionFactory = connectionFactory;
            if (this.messageConverter == null)
            {
                var internalmessageConverter = new SimpleMessageConverter();
                internalmessageConverter.CreateMessageIds = true;
                this.messageConverter = internalmessageConverter;
            }

            template.MessageConverter = this.messageConverter;
            return template;
        }

        /// <summary>The test stateful retry with all messages failing.</summary>
        [Test]
        [Ignore]
        public void testStatefulRetryWithAllMessagesFailing()
        {
            var messageCount = 10;
            var txSize = 1;
            var failFrequency = 1;
            var concurrentConsumers = 3;
            this.DoTestStatefulRetry(messageCount, txSize, failFrequency, concurrentConsumers);
        }

        /// <summary>The test stateless retry with all messages failing.</summary>
        [Test]
        [Ignore]
        public void testStatelessRetryWithAllMessagesFailing()
        {
            var messageCount = 10;
            var txSize = 1;
            var failFrequency = 1;
            var concurrentConsumers = 3;
            this.DoTestStatelessRetry(messageCount, txSize, failFrequency, concurrentConsumers);
        }

        /// <summary>The test stateful retry with no message ids.</summary>
        [Test]
        [Ignore]
        public void testStatefulRetryWithNoMessageIds()
        {
            var messageCount = 2;
            var txSize = 1;
            var failFrequency = 1;
            var concurrentConsumers = 1;
            var messageConverter = new SimpleMessageConverter();

            // There will be no key for these messages so they cannot be recovered...
            messageConverter.CreateMessageIds = false;
            this.messageConverter = messageConverter;

            // Beware of context cache busting if retry policy fails...
            /* TODO: Once Spring Retry is implemented.
             * this.retryTemplate = new RetryTemplate();
            this.retryTemplate.setRetryContextCache(new MapRetryContextCache(1));
             */
            // The container should have shutdown, so there are now no active consumers
            // exception.expectMessage("expected:<1> but was:<0>");
            this.DoTestStatefulRetry(messageCount, txSize, failFrequency, concurrentConsumers);
        }

        /// <summary>The test stateful retry with tx size and intermittent failure.</summary>
        [Test]
        [Ignore]
        [Repeat(10)]
        public void testStatefulRetryWithTxSizeAndIntermittentFailure()
        {
            var messageCount = 10;
            var txSize = 4;
            var failFrequency = 3;
            var concurrentConsumers = 3;
            this.DoTestStatefulRetry(messageCount, txSize, failFrequency, concurrentConsumers);
        }

        /// <summary>The test stateful retry with more messages.</summary>
        [Test]
        [Ignore]
        public void testStatefulRetryWithMoreMessages()
        {
            var messageCount = 200;
            var txSize = 10;
            var failFrequency = 6;
            var concurrentConsumers = 3;
            this.DoTestStatefulRetry(messageCount, txSize, failFrequency, concurrentConsumers);
        }

        // Spring Batch Not Implemented - Can't implement this...
        private IAdvice createRetryInterceptor(CountdownEvent latch, bool stateful)
        {
            /*AbstractRetryOperationsInterceptorFactoryObject factory;
            if (stateful) {
                factory = new StatefulRetryOperationsInterceptorFactoryObject();
            } else {
                factory = new StatelessRetryOperationsInterceptorFactoryObject();
            }
            factory.MessageRecoverer(new MessageRecoverer() {
                public void recover(Message message, Throwable cause) {
                    Logger.Info("Recovered: [" + SerializationUtils.deserialize(message.getBody()).toString()+"], message: " +message);
                    latch.Signal();
                }
            });
            if (retryTemplate == null) {
                retryTemplate = new RetryTemplate();
            }
            factory.setRetryOperations(retryTemplate);
            var retryInterceptor = factory.getObject();
            return retryInterceptor;*/
            throw new NotImplementedException();
        }

        private void DoTestStatefulRetry(int messageCount, int txSize, int failFrequency, int concurrentConsumers) { this.DoTestRetry(messageCount, txSize, failFrequency, concurrentConsumers, true); }

        private void DoTestStatelessRetry(int messageCount, int txSize, int failFrequency, int concurrentConsumers) { this.DoTestRetry(messageCount, txSize, failFrequency, concurrentConsumers, false); }

        private void DoTestRetry(int messageCount, int txSize, int failFrequency, int concurrentConsumers, bool stateful)
        {
            var failedMessageCount = messageCount / failFrequency + (messageCount % failFrequency == 0 ? 0 : 1);

            this.template = this.CreateTemplate(concurrentConsumers);
            for (var i = 0; i < messageCount; i++)
            {
                this.template.ConvertAndSend(queue.Name, i.ToString());
            }

            var container = new SimpleMessageListenerContainer(this.template.ConnectionFactory);
            var listener = new RetryPocoListener(failFrequency);
            container.MessageListener = new MessageListenerAdapter(listener);
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
            container.ChannelTransacted = true;
            container.TxSize = txSize;
            container.ConcurrentConsumers = concurrentConsumers;

            var latch = new CountdownEvent(failedMessageCount);

            // container.AdviceChain = new IAdvice[] { CreateRetryInterceptor(latch, stateful) };
            container.QueueNames = new[] { queue.Name };
            container.AfterPropertiesSet();
            container.Start();

            try
            {
                var timeout = Math.Min(1 + messageCount / concurrentConsumers, 30);

                var count = messageCount;
                Logger.Debug("Waiting for messages with timeout = " + timeout + " (s)");
                Task.Factory.StartNew(
                    () =>
                    {
                        while (container.ActiveConsumerCount > 0)
                        {
                            try
                            {
                                Thread.Sleep(100);
                            }
                            catch (ThreadInterruptedException e)
                            {
                                if (latch.CurrentCount > 0)
                                {
                                    latch.Signal();
                                }

                                Thread.CurrentThread.Interrupt();
                                return;
                            }
                        }

                        for (var i = 0; i < count; i++)
                        {
                            if (latch.CurrentCount > 0)
                            {
                                latch.Signal();
                            }
                        }
                    });
                var waited = latch.Wait(timeout * 1000);
                Logger.Info("All messages recovered: " + waited);
                Assert.AreEqual(concurrentConsumers, container.ActiveConsumerCount);
                Assert.True(waited, "Timed out waiting for messages");

                // Retried each failure 3 times (default retry policy)...
                Assert.AreEqual(3 * failedMessageCount, listener.Count);

                // All failed messages recovered
                Assert.AreEqual(null, this.template.ReceiveAndConvert(queue.Name));
            }
            finally
            {
                container.Shutdown();
                Assert.AreEqual(0, container.ActiveConsumerCount);
            }
        }
    }

    /// <summary>
    /// A retry poco listener.
    /// </summary>
    public class RetryPocoListener
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly AtomicInteger count = new AtomicInteger();
        private readonly int failFrequency;

        /// <summary>Initializes a new instance of the <see cref="RetryPocoListener"/> class.</summary>
        /// <param name="failFrequency">The fail frequency.</param>
        public RetryPocoListener(int failFrequency) { this.failFrequency = failFrequency; }

        /// <summary>Handles the message.</summary>
        /// <param name="value">The value.</param>
        public void HandleMessage(int value)
        {
            Logger.Debug(value + ":" + this.count.ReturnValueAndIncrement());
            if (value % this.failFrequency == 0)
            {
                throw new Exception("Planned");
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count { get { return this.count.Value; } }
    }
}
