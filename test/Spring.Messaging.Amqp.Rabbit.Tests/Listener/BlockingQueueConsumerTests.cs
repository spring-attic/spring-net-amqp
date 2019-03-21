// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlockingQueueConsumerTests.cs" company="The original author or authors.">
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
using System;
using System.Collections.Generic;
using System.Reflection;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// Blocking Queue Consumer Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class BlockingQueueConsumerTests
    {
        /// <summary>The test requeue.</summary>
        [Test]
        public void TestRequeue()
        {
            var ex = new Exception();
            this.TestRequeueOrNot(ex, true);
        }

        /// <summary>The test requeue null exception.</summary>
        [Test]
        public void TestRequeueNullException()
        {
            Exception ex = null;
            this.TestRequeueOrNot(ex, true);
        }

        /// <summary>The test dont requeue.</summary>
        [Test]
        public void TestDontRequeue()
        {
            Exception ex = new AmqpRejectAndDontRequeueException("fail");
            this.TestRequeueOrNot(ex, false);
        }

        /// <summary>The test dont requeue nested.</summary>
        [Test]
        public void TestDontRequeueNested()
        {
            var ex = new Exception("fail", new Exception("fail", new AmqpRejectAndDontRequeueException("fail")));
            this.TestRequeueOrNot(ex, false);
        }

        /// <summary>The test requeue default not.</summary>
        [Test]
        public void TestRequeueDefaultNot()
        {
            var ex = new Exception();
            this.TestRequeueOrNotDefaultNot(ex, false);
        }

        /// <summary>The test requeue null exception default not.</summary>
        [Test]
        public void TestRequeueNullExceptionDefaultNot()
        {
            Exception ex = null;
            this.TestRequeueOrNotDefaultNot(ex, false);
        }

        /// <summary>The test dont requeue default not.</summary>
        [Test]
        public void TestDontRequeueDefaultNot()
        {
            Exception ex = new AmqpRejectAndDontRequeueException("fail");
            this.TestRequeueOrNotDefaultNot(ex, false);
        }

        /// <summary>The test dont requeue nested default not.</summary>
        [Test]
        public void TestDontRequeueNestedDefaultNot()
        {
            var ex = new Exception("fail", new Exception("fail", new AmqpRejectAndDontRequeueException("fail")));
            this.TestRequeueOrNotDefaultNot(ex, false);
        }

        private void TestRequeueOrNot(Exception ex, bool requeue)
        {
            var connectionFactory = new Mock<IConnectionFactory>();
            var channel = new Mock<IModel>();
            var blockingQueueConsumer = new BlockingQueueConsumer(
                connectionFactory.Object, new DefaultMessagePropertiesConverter(), new ActiveObjectCounter<BlockingQueueConsumer>(), AcknowledgeModeUtils.AcknowledgeMode.Auto, true, 1, "testQ");
            this.TestRequeueOrNotGuts(ex, requeue, channel, blockingQueueConsumer);
        }

        private void TestRequeueOrNotDefaultNot(Exception ex, bool requeue)
        {
            var connectionFactory = new Mock<IConnectionFactory>();
            var channel = new Mock<IModel>();
            var blockingQueueConsumer = new BlockingQueueConsumer(
                connectionFactory.Object, 
                new DefaultMessagePropertiesConverter(), 
                new ActiveObjectCounter<BlockingQueueConsumer>(), 
                AcknowledgeModeUtils.AcknowledgeMode.Auto, 
                true, 
                1, 
                false, 
                "testQ");
            this.TestRequeueOrNotGuts(ex, requeue, channel, blockingQueueConsumer);
        }

        private void TestRequeueOrNotGuts(Exception ex, bool requeue, Mock<IModel> channel, BlockingQueueConsumer blockingQueueConsumer)
        {
            var channelField = typeof(BlockingQueueConsumer).GetField("channel", BindingFlags.NonPublic | BindingFlags.Instance);
            channelField.SetValue(blockingQueueConsumer, channel.Object);

            var deliveryTags = new LinkedList<long>();
            deliveryTags.AddOrUpdate(1L);
            var deliveryTagsField = typeof(BlockingQueueConsumer).GetField("deliveryTags", BindingFlags.NonPublic | BindingFlags.Instance);
            deliveryTagsField.SetValue(blockingQueueConsumer, deliveryTags);
            blockingQueueConsumer.RollbackOnExceptionIfNecessary(ex);
            channel.Verify(m => m.BasicReject(1L, requeue), Times.Once());
        }
    }
}
