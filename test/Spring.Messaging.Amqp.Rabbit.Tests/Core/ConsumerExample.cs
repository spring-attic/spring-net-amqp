#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Listener.Adapter;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    [TestFixture]
    [Ignore] // This is an integratio test, can't be run via the test runner yet.
    public class ConsumerExample : AbstractExample
    {
        [Test]
        public void Consumer()
        {
            RabbitTemplate template = InitializeAndCreateTemplate();
            //ReceiveSync(template, TestConstants.NUM_MESSAGES);
            ReceiveAsync();
        }

        private void ReceiveAsync()
        {
            SimpleMessageListenerContainer container = new SimpleMessageListenerContainer();
            container.ConnectionFactory = this.connectionFactory;
            container.QueueName = TestConstants.QUEUE_NAME;
            container.ConcurrentConsumers = 5;

            MessageListenerAdapter adapter = new MessageListenerAdapter();
            adapter.HandlerObject = new PocoHandler();
            container.MessageListener = adapter;
            container.AfterPropertiesSet();
            container.Start();
            Console.WriteLine("Main execution thread sleeping...");
            Thread.Sleep(30000);
            Console.WriteLine("Main execution thread exiting.");
            container.Stop();
            container.Shutdown();           
        }


        private void ReceiveSync(RabbitTemplate template, int numMessages)
        {
             for (int i = 0; i < numMessages; i++)
             {
                 string msg = (string) template.ReceiveAndConvert("foo");
                 if (msg == null)
                 {
                     Console.WriteLine("Thread [" + Thread.CurrentThread.ManagedThreadId + "] " + "Recieved null message!");
                 }
                 else
                 {
                     Console.WriteLine("Thread [" + Thread.CurrentThread.ManagedThreadId + "] " + msg);
                 }                 
             }           
        }
    }

    public class PocoHandler
    {
        private int msgCount;

        public void HandleMessage(string textMessage)
        {
            lock(this)
            {
                ++msgCount;
            }
            Console.WriteLine("Thread [" + Thread.CurrentThread.ManagedThreadId + "] " + textMessage);
        }
    }
}
