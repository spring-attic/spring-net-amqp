using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Common.Logging;
using Spring.Context;
using Spring.Context.Support;
using Spring.Messaging.Amqp.Core;

namespace Spring.Amqp.HelloWorld.Producer.Async
{
    /// <summary>
    /// A sample asynchronous producer.
    /// </summary>
    /// <remarks></remarks>
    class Program
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// Starts the program.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <remarks></remarks>
        public static void Main(string[] args)
        {
            using (var ctx = ContextRegistry.GetContext())
            {
                var amqpTemplate = ctx.GetObject<IAmqpTemplate>();
                int i = 0;
                while (true)
                {
                    amqpTemplate.ConvertAndSend("Hello World " + i++);
                    Logger.Info("Hello world message sent.");
                    Thread.Sleep(3000);
                }
            }            
        }
    }
}