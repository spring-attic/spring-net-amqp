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

using org.apache.qpid.client;
using Spring.Messaging.Amqp.Qpid.Client;
using Spring.Messaging.Amqp.Qpid.Core;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class AbstractExample
    {
        protected SimpleClientFactory clientFactory;

        protected QpidTemplate InitializeAndCreateTemplate()
        {
            string host = "localhost";
            int port = 5672;
            ConnectionParameters parameters = new ConnectionParameters(host, port, "dev-only", "guest", "guest");
            
            clientFactory = new SimpleClientFactory(parameters);


            QpidTemplate template = new QpidTemplate();
            template.ClientFactory = clientFactory;
            template.ChannelTransacted = true;
            template.AfterPropertiesSet();

            //Declare queue and bind to a specific exchange.
            template.Execute<object>(delegate(IClientSession session)
                                         {
                                             session.QueueDeclare(TestConstants.QUEUE_NAME);
                                             //TODO Bind XSD needs to take into accout parameters nowait and 'Dictionary' args
                                             session.ExchangeBind("message_queue", "amq.direct", "routing_key");
                                             //model.QueueBind(TestConstants.QUEUE_NAME, TestConstants.EXCHANGE_NAME, TestConstants.ROUTING_KEY, false, null);
                                             return null;
                                         });
            return template;
        }
    }

}