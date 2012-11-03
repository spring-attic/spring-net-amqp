// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlockingQueueConsumerIntegrationTests.cs" company="The original author or authors.">
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
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class BlockingQueueConsumerIntegrationTests : AbstractRabbitIntegrationTest
    {
        private static readonly Queue queue = new Queue("test.queue");

        /// <summary>The test transactional low level.</summary>
        [Test]
        public void TestTransactionalLowLevel()
        {
            var template = new RabbitTemplate();
            var connectionFactory = new CachingConnectionFactory();
            connectionFactory.Port = BrokerTestUtils.GetPort();
            template.ConnectionFactory = connectionFactory;

            var blockingQueueConsumer = new BlockingQueueConsumer(connectionFactory, new DefaultMessagePropertiesConverter(), new ActiveObjectCounter<BlockingQueueConsumer>(), AcknowledgeModeUtils.AcknowledgeMode.Auto, true, 1, queue.Name);
            blockingQueueConsumer.Start();
            connectionFactory.Dispose();

            // TODO: make this into a proper assertion. An exception can be thrown here by the Rabbit client and printed to
            // stderr without being rethrown (so hard to make a test fail).
            blockingQueueConsumer.Stop();
            Assert.IsNull(template.ReceiveAndConvert(queue.Name));
        }

        public override void BeforeFixtureSetUp() { this.brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue); }

        public override void BeforeFixtureTearDown()
        {
        }

        public override void AfterFixtureSetUp()
        {
        }

        public override void AfterFixtureTearDown()
        {
        }
    }
}
