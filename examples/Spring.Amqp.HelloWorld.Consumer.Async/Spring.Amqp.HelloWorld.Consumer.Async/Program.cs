using System;
using System.Collections.Generic;
using System.Text;
using Spring.Context;
using Spring.Context.Support;

namespace Spring.Amqp.HelloWorld.Consumer.Async
{
    /// <summary>
    /// A sample asynchronous consumer.
    /// </summary>
    /// <remarks></remarks>
    class Program
    {
        /// <summary>
        /// Starts the program.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <remarks></remarks>
        public static void Main(string[] args)
        {
            var ctx = ContextRegistry.GetContext();
            
            Console.Out.WriteLine("Consumer listening...");
            Console.Out.WriteLine("--- Press <return> to quit ---");
            Console.ReadLine();
            Console.Out.WriteLine("--- return pressed ---");
            ctx.Dispose();
            
        }
    }
}
