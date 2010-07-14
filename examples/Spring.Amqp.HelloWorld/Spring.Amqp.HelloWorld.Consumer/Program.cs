#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using Common.Logging;
using Spring.Context;
using Spring.Context.Support;
using Spring.Messaging.Amqp.Core;

namespace Spring.Amqp.HelloWorld.Consumer
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            using (IApplicationContext ctx = ContextRegistry.GetContext())
            {
                IAmqpTemplate amqpTemplate = (IAmqpTemplate)ctx.GetObject("RabbitTemplate");
                log.Info("Synchronous pull");
                String message = (String) amqpTemplate.ReceiveAndConvert();
                if (message == null)
                {
                    log.Info("[No message present on queue to receive.]");
                }
                else
                {
                    log.Info("Received: " + message);
                }
            }

            Console.WriteLine("Press 'enter' to exit.");
            Console.ReadLine();
        }
    }
}