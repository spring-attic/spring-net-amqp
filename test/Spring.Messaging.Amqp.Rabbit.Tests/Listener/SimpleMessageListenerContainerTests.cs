// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleMessageListenerContainerTests.cs" company="The original author or authors.">
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
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;
using Spring.Messaging.Amqp.Rabbit.Tests.Connection;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Transaction;
using Spring.Transaction.Support;
using Spring.Util;
#endregion

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
        [Test]
        public void TestInconsistentTransactionConfiguration()
        {
            var container = new SimpleMessageListenerContainer(new SingleConnectionFactory());
            container.MessageListener = new MessageListenerAdapter(this);
            container.QueueNames = new[] { "foo" };
            container.ChannelTransacted = false;
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.None;
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
        [Test]
        public void TestInconsistentAcknowledgeConfiguration()
        {
            var container = new SimpleMessageListenerContainer(new SingleConnectionFactory());
            container.MessageListener = new MessageListenerAdapter(this);
            container.QueueNames = new[] { "foo" };
            container.ChannelTransacted = true;
            container.AcknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.None;

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
        [Test]
        public void TestDefaultConsumerCount()
        {
            var container = new SimpleMessageListenerContainer(new SingleConnectionFactory());
            container.MessageListener = new MessageListenerAdapter(this);
            container.QueueNames = new[] { "foo" };
            container.AutoStartup = false;
            container.AfterPropertiesSet();
            Assert.AreEqual(1, ReflectionUtils.GetInstanceFieldValue(container, "concurrentConsumers"));
        }

        /// <summary>
        /// Tests the lazy consumer count.
        /// </summary>
        [Test]
        public void TestLazyConsumerCount()
        {
            var container = new SimpleMessageListenerContainer(new SingleConnectionFactory());

            // TODO: I added this, but should we be setting a default queue name, instead of blowing up when queueNames is empty?
            container.QueueNames = new[] { "foo" };
            container.Start();
            Assert.AreEqual(1, ReflectionUtils.GetInstanceFieldValue(container, "concurrentConsumers"));
        }
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
        protected override void DoBegin(object transaction, ITransactionDefinition definition) { }

        /// <summary>Perform an actual commit on the given transaction.</summary>
        /// <param name="status">The status representation of the transaction.</param>
        protected override void DoCommit(DefaultTransactionStatus status) { }

        /// <summary>
        /// Return the current transaction object.
        /// </summary>
        /// <returns>The current transaction object.</returns>
        protected override object DoGetTransaction() { return new object(); }

        /// <summary>Perform an actual rollback on the given transaction.</summary>
        /// <param name="status">The status representation of the transaction.</param>
        protected override void DoRollback(DefaultTransactionStatus status) { }
    }
}
