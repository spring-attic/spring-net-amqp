using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using Spring.Context.Support;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.RabbitQuickStart.Server.Gateways;

namespace Spring.RabbitQuickStart.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Using Spring's IoC container
                ContextRegistry.GetContext(); // Force Spring to load configuration
                Console.Out.WriteLine("Server listening...");

                InitializeRabbitQueues();
                IMarketDataService marketDataService =
                    ContextRegistry.GetContext().GetObject("MarketDataGateway") as MarketDataServiceGateway;
                ThreadStart job = new ThreadStart(marketDataService.SendMarketData);
                Thread thread = new Thread(job);
                thread.Start();
                Console.Out.WriteLine("--- Press <return> to quit ---");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
                Console.Out.WriteLine("--- Press <return> to quit ---");
                Console.ReadLine();
            }
        }

        private static void InitializeRabbitQueues()
        {
            RabbitTemplate template = ContextRegistry.GetContext().GetObject("RabbitTemplate") as RabbitTemplate;
            template.Execute<object>(delegate(IModel model)
            {
                model.QueueDeclare("APP.STOCK.MARKETDATA");                

                model.QueueBind("APP.STOCK.MARKETDATA", "", "", false, null);

                //We don't need to define any binding for the stock request queue, since it's relying
                //on the default (no-name) direct exchange to which every queue is implicitly bound.
                model.QueueDeclare("APP.STOCK.REQUEST");

                //This queue does not need a binding, since it relies on the default exchange.
                model.QueueDeclare("APP.STOCK.JOE");
                return null;
            });
        }
    }
}
