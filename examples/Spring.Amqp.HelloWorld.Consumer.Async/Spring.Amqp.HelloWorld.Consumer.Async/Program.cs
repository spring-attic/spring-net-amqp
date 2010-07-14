using System;
using System.Collections.Generic;
using System.Text;
using Spring.Context;
using Spring.Context.Support;

namespace Spring.Amqp.HelloWorld.Consumer.Async
{
    class Program
    {
        static void Main(string[] args)
        {
            IApplicationContext ctx = ContextRegistry.GetContext();
            
            Console.Out.WriteLine("Consumer listening...");
            Console.Out.WriteLine("--- Press <return> to quit ---");
            Console.ReadLine();
            Console.Out.WriteLine("--- return pressed ---");
            ctx.Dispose();
            
        }
    }
}
