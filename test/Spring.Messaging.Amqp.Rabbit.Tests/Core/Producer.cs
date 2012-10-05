// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Producer.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Connection;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core
{
    /// <summary>
    /// A producer.
    /// </summary>
    /// <remarks></remarks>
    public class Producer
    {
        /// <summary>Mains the specified args.</summary>
        /// <param name="args">The args.</param>
        /// <remarks></remarks>
        public static void Main(string[] args)
        {
            var connectionFactory = new SingleConnectionFactory("localhost");
            connectionFactory.UserName = "guest";
            connectionFactory.Password = "guest";

            var template = new RabbitTemplate();
            template.ConnectionFactory = connectionFactory;
            template.ChannelTransacted = true;
            template.AfterPropertiesSet();

            var routingKey = TestConstants.ROUTING_KEY;
            QueueUtils.DeclareTestQueue(template, routingKey);

            // Send message
            SendMessages(template, TestConstants.EXCHANGE_NAME, routingKey, TestConstants.NUM_MESSAGES);
        }

        /// <summary>Sends the messages.</summary>
        /// <param name="template">The template.</param>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="numMessages">The num messages.</param>
        /// <remarks></remarks>
        private static void SendMessages(RabbitTemplate template, string exchange, string routingKey, int numMessages)
        {
            for (int i = 1; i <= numMessages; i++)
            {
                byte[] bytes = Encoding.UTF8.GetBytes("testing");
                var properties = new MessageProperties();
                properties.Headers.Add("float", 3.14);
                var message = new Message(bytes, properties);
                template.Send(exchange, routingKey, message);
                Console.WriteLine("sending " + i + "...");
            }
        }
    }
}
