#region License

/*
 * Copyright 2002-2009 the original author or authors.
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

namespace Spring.Amqp.HelloWorld.Producer
{
    class Program
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            using (IApplicationContext ctx = ContextRegistry.GetContext())
            {
                var amqpTemplate = (IAmqpTemplate) ctx.GetObject("RabbitTemplate");
                log.Info("Sending hello world message.");
                amqpTemplate.ConvertAndSend("Hello World");
                log.Info("Hello world message sent.");                
            }

            Console.WriteLine("Press 'enter' to exit.");
            Console.ReadLine();
        }
    }
}