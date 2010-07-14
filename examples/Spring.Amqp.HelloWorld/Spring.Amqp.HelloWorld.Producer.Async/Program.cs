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
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            using (IApplicationContext ctx = ContextRegistry.GetContext())
            {
                IAmqpTemplate amqpTemplate = (IAmqpTemplate)ctx.GetObject("RabbitTemplate");
                int i = 0;
                while (true)
                {
                    amqpTemplate.ConvertAndSend("Hello World " + i++);
                    log.Info("Hello world message sent.");
                    Thread.Sleep(3000);
                }
            }            
        }
    }
}