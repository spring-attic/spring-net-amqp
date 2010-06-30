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

using RabbitMQ.Client;
using Spring.Messaging.Amqp.Rabbit.Connection;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class AbstractExample
    {
        protected CachingConnectionFactory connectionFactory;

        protected RabbitTemplate InitializeAndCreateTemplate()
        {
            ConnectionParameters parameters = new ConnectionParameters();
            parameters.UserName = ConnectionParameters.DefaultUser;
            parameters.Password = ConnectionParameters.DefaultPass;
            parameters.VirtualHost = ConnectionParameters.DefaultVHost;
            /* TODO - put in calls to determine protocol in CachingConnectionFactory, e.g.
            IProtocol protocol = Protocols.DefaultProtocol;
            IConnection conn = factory.CreateConnection(protocol, hostName, portNumber);
            */
            connectionFactory = new CachingConnectionFactory(parameters);


            RabbitTemplate template = new RabbitTemplate();
            template.ConnectionFactory = connectionFactory;
            template.ChannelTransacted = true;
            template.AfterPropertiesSet();

            //Declare queue and bind to a specific exchange.
            template.Execute<object>(delegate(IModel model)
                                         {
                                             model.QueueDeclare(TestConstants.QUEUE_NAME);
                                             //TODO Bind XSD needs to take into accout parameters nowait and 'Dictionary' args
                                             model.QueueBind(TestConstants.QUEUE_NAME, TestConstants.EXCHANGE_NAME, TestConstants.ROUTING_KEY, false, null);
                                             return null;
                                         });
            return template;
        }
    }

}