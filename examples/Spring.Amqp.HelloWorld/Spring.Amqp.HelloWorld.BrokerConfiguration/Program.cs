#region

using System;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;

#endregion

namespace Spring.Amqp.HelloWorld.BrokerConfiguration
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using (IConnectionFactory connectionFactory = new SingleConnectionFactory())
            {
                IAmqpAdmin amqpAdmin = new RabbitAdmin(connectionFactory);

                var helloWorldQueue = new Queue("hello.world.queue");

                amqpAdmin.DeclareQueue(helloWorldQueue);

                //Each queue is automatically bound to the default direct exchange.      
   
                Console.WriteLine("Queue [hello.world.queue] has been declared.");
                Console.WriteLine("Press 'enter' to exit");
                Console.ReadLine();
            }
        }
    }
}