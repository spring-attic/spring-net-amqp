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
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using org.apache.qpid.client;
using org.apache.qpid.transport;

namespace Spring.Messaging.Amqp.Qpid.Tests.Core
{
    [TestFixture]
    public class ConsumerExample
    {
        [Test]
        public void RawApiConsumer()
        {
            string host = "localhost";
            int port = 5672;
            org.apache.qpid.client.Client client = new org.apache.qpid.client.Client();
            try
            {
                client.Connect(host, port, "dev-only", "guest", "guest");
                IClientSession session = client.CreateSession(50000);

                //

                session.QueueDeclare("message_queue");
                session.ExchangeBind("message_queue", "amq.direct", "routing_key");
                IClientSession session2 = session;
                lock (session2)
                {
                    IMessageListener listener = new MessageListener(session);
                    session.AttachMessageListener(listener, "message_queue");
                    session.MessageSubscribe("message_queue");
                    Monitor.Wait(session);
                }


                //
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: \n" + e.StackTrace);
            }
        }
    }

    public class MessageListener : IMessageListener
    {
        // Fields
        private readonly RangeSet _range = new RangeSet();
        private readonly IClientSession _session;

        // Methods
        public MessageListener(IClientSession session)
        {
            this._session = session;
        }

        public void MessageTransfer(IMessage m)
        {
            BinaryReader reader = new BinaryReader(m.Body, Encoding.UTF8);
            byte[] buffer = new byte[m.Body.Length - m.Body.Position];
            reader.Read(buffer, 0, buffer.Length);
            string str = new ASCIIEncoding().GetString(buffer);
            Console.WriteLine("Message: " + str);
            this._range.Add(m.Id);
            if (str.Equals("That's all, folks!"))
            {
                this._session.MessageAccept(this._range, new Option[0]);
                IClientSession session = this._session;
                lock (session)
                {
                    Monitor.Pulse(this._session);
                }
            }
        }
    }

 

}