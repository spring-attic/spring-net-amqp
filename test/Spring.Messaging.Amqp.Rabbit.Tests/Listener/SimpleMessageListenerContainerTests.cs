using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using NUnit.Framework;

using Spring.Messaging.Amqp.Rabbit.Tests.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Transaction;
using Spring.Transaction.Support;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// Simple message listener container tests.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class SimpleMessageListenerContainerTests
    {

        // @Rule
        // public ExpectedException expectedException = ExpectedException.none();

        /// <summary>
        /// Tests the inconsistent transaction configuration.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestInconsistentTransactionConfiguration()
        {
            var container = new SimpleMessageListenerContainer(new SingleConnectionFactory());
            container.MessageListener = new MessageListenerAdapter(this);
            container.QueueNames = new string[] { "foo" };
            container.IsChannelTransacted = false;
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.NONE;
            container.TransactionManager = new TestTransactionManager();

            try
            {
                container.AfterPropertiesSet();
            }
            catch (Exception e)
            {
                Assert.True(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// Tests the inconsistent acknowledge configuration.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestInconsistentAcknowledgeConfiguration()
        {
            var container = new SimpleMessageListenerContainer(new SingleConnectionFactory());
            container.MessageListener = new MessageListenerAdapter(this);
            container.QueueNames = new string[] { "foo" };
            container.IsChannelTransacted = true;
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.NONE;

            try
            {
                container.AfterPropertiesSet();
            }
            catch (Exception e)
            {
                Assert.True(e is InvalidOperationException);
            }
        }

        /// <summary>
        /// Tests the default consumer count.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestDefaultConsumerCount()
        {
            var container = new SimpleMessageListenerContainer(new SingleConnectionFactory());
            container.MessageListener = new MessageListenerAdapter(this);
            container.QueueNames = new string[] { "foo" };
            container.AutoStartup = false;
            container.AfterPropertiesSet();
            Assert.AreEqual(1, ReflectionUtils.GetInstanceFieldValue(container, "concurrentConsumers"));
        }

        /// <summary>
        /// Tests the lazy consumer count.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestLazyConsumerCount()
        {
            var container = new SimpleMessageListenerContainer(new SingleConnectionFactory());
            
            // TODO: I added this, but should we be setting a default queue name, instead of blowing up when queueNames is empty?
            container.QueueNames = new string[] { "foo" };
            container.Start();
            Assert.AreEqual(1, ReflectionUtils.GetInstanceFieldValue(container, "concurrentConsumers"));
        }
    }

    /// <summary>
    /// A test transaction manager.
    /// </summary>
    internal class TestTransactionManager : AbstractPlatformTransactionManager
    {
        /// <summary>
        /// Begin a new transaction with the given transaction definition.
        /// </summary>
        /// <param name="transaction">Transaction object returned by
        /// <see cref="M:Spring.Transaction.Support.AbstractPlatformTransactionManager.DoGetTransaction"/>.</param>
        /// <param name="definition"><see cref="T:Spring.Transaction.ITransactionDefinition"/> instance, describing
        /// propagation behavior, isolation level, timeout etc.</param>
        protected override void DoBegin(object transaction, ITransactionDefinition definition)
        {
        }

        /// <summary>
        /// Perform an actual commit on the given transaction.
        /// </summary>
        /// <param name="status">The status representation of the transaction.</param>
        protected override void DoCommit(DefaultTransactionStatus status)
        {
        }

        /// <summary>
        /// Return the current transaction object.
        /// </summary>
        /// <returns>The current transaction object.</returns>
        protected override object DoGetTransaction()
        {
            return new object();
        }

        /// <summary>
        /// Perform an actual rollback on the given transaction.
        /// </summary>
        /// <param name="status">The status representation of the transaction.</param>
        protected override void DoRollback(DefaultTransactionStatus status)
        {
        }
    }
}
