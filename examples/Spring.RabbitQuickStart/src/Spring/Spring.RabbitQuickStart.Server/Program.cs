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
                ContextRegistry.GetContext(); 
                Console.Out.WriteLine("Server listening...");
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
    }
}
