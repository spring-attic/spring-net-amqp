
using System;
using System.Text;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Connection;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Core
{
    /// <summary>
    /// A producer.
    /// </summary>
    /// <remarks></remarks>
    public class Producer
    {
        /// <summary>
        /// Mains the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <remarks></remarks>
        public static void Main(string[] args)
        {
            var connectionFactory = new SingleConnectionFactory("localhost");
            connectionFactory.UserName = "guest";
            connectionFactory.Password = "guest";

            var template = new RabbitTemplate();
            template.ConnectionFactory = connectionFactory;
            template.IsChannelTransacted = true;
            template.AfterPropertiesSet();

            var routingKey = TestConstants.ROUTING_KEY;
            QueueUtils.DeclareTestQueue(template, routingKey);

            // Send message
            SendMessages(template, TestConstants.EXCHANGE_NAME, routingKey, TestConstants.NUM_MESSAGES);
        }

        /// <summary>
        /// Sends the messages.
        /// </summary>
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
                properties.Headers.Add("float", (double)3.14);
                var message = new Message(bytes, properties);
                template.Send(exchange, routingKey, message);
                Console.WriteLine("sending " + i + "...");
            }
        }
    }
}
