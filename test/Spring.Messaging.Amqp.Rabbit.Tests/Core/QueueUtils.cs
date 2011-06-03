using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.Impl.v0_9_1;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// Queue Utilities
    /// </summary>
    /// <remarks></remarks>
    public class QueueUtils
    {
        /// <summary>
        /// Declares the test queue.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <remarks></remarks>
        public static void DeclareTestQueue(RabbitTemplate template, string routingKey)
        {
            // declare and bind queue
            template.Execute<string>(delegate(IModel channel)
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
