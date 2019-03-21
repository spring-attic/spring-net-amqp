#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.Text;
using NUnit.Framework;
//using org.apache.qpid.client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Qpid.Core;
using Spring.Messaging.Amqp.Rabbit.Core;

namespace Spring.Messaging.Amqp.Qpid.Tests.Core
{
    [TestFixture]
    public class ProducerExample : AbstractExample
    {
        [Test]
        public void RawApi()
        {
            string host = "localhost";
            int port = 5672;
            org.apache.qpid.client.Client client = new org.apache.qpid.client.Client();
            try
            {                
                client.Connect(host, port, "dev-only", "guest", "guest");
                org.apache.qpid.client.IClientSession session = client.CreateSession(50000);

                //
                org.apache.qpid.client.IMessage message = new org.apache.qpid.client.Message();
                message.DeliveryProperties.SetRoutingKey("routing_key");
                for (int i = 0; i < 10; i++)
                {
                    message.ClearData();
                    message.AppendData(Encoding.UTF8.GetBytes("Message " + i));
                    session.MessageTransfer("amq.direct", message);
                }
                message.ClearData();
                message.AppendData(Encoding.UTF8.GetBytes("That's all, folks!"));
                session.MessageTransfer("amq.direct", "routing_key", message);
                session.Sync();
                

                //
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: \n" + e.StackTrace);
            }

        }

        [Test]
        public void SendWithTemplate()
        {
            QpidTemplate template = InitializeAndCreateTemplate();

            IMessageProperties messageProperties = new MessageProperties();


            //write all short props
            messageProperties.ContentType = "text/plain";
            messageProperties.ContentEncoding = "UTF-8";
            messageProperties.CorrelationId = Encoding.UTF8.GetBytes("corr1");


            messageProperties.DeliveryMode = MessageDeliveryMode.PERSISTENT;
            messageProperties.Priority = 0;


            for (int i = 0; i < 10; i++)
            {
                template.Send("amq.direct", "routing_key", delegate
                {
                    Message msg = new Message(Encoding.UTF8.GetBytes("Message " + i), messageProperties);
                    Console.WriteLine("sending...");
                    return msg;
                });
            }
            template.Send("amq.direct", "routing_key", delegate
            {
                Message msg = new Message(Encoding.UTF8.GetBytes("That's all, folks!"), messageProperties);
                Console.WriteLine("sending...");
                return msg;
            });
            
        }
    }
}