// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueueUtils.cs" company="The original author or authors.">
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
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Rabbit.Core;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core
{
    /// <summary>
    /// Queue Utilities
    /// </summary>
    public class QueueUtils
    {
        /// <summary>Declares the test queue.</summary>
        /// <param name="template">The template.</param>
        /// <param name="routingKey">The routing key.</param>
        public static void DeclareTestQueue(RabbitTemplate template, string routingKey)
        {
            // declare and bind queue
            template.Execute<string>(
                delegate(IModel channel)
                {
                    var queueName = channel.QueueDeclarePassive(TestConstants.QUEUE_NAME);

                    // String queueName = res.GetQueue();
                    Console.WriteLine("Queue Name = " + queueName);
                    channel.QueueBind(queueName, TestConstants.EXCHANGE_NAME, routingKey);
                    return queueName;
                });
        }
    }
}
