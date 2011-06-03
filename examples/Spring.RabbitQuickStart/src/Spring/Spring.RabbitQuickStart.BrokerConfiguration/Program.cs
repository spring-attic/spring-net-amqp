
using System;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;

namespace Spring.RabbitQuickStart.BrokerConfiguration
{
    class Program
    {
        static void Main(string[] args)
        {
            using (IConnectionFactory connectionFactory = new SingleConnectionFactory())
            {
                IAmqpAdmin amqpAdmin = new RabbitAdmin(connectionFactory);

                var marketDataQueue = new Queue("APP.STOCK.MARKETDATA");
                amqpAdmin.DeclareQueue(marketDataQueue);
                var binding = BindingBuilder.Bind(marketDataQueue).To(DirectExchange.DEFAULT).WithQueueName();
                amqpAdmin.DeclareBinding(binding);

                amqpAdmin.DeclareQueue(new Queue("APP.STOCK.REQUEST"));
                amqpAdmin.DeclareQueue(new Queue("APP.STOCK.JOE"));

                //Each queue is automatically bound to the default direct exchange.      

                Console.WriteLine("Queues and exchanges have been declared.");
                Console.WriteLine("Press 'enter' to exit");
                Console.ReadLine();
            }
        }
    }
}
