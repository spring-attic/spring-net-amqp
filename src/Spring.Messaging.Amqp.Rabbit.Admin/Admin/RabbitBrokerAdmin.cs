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
using Spring.Erlang.Connection;
using Spring.Erlang.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>
    /// Rabbit broker administration implementation
    /// </summary>
    /// <author>Mark Pollack</author>
    public class RabbitBrokerAdmin : IRabbitBrokerOperations
    {
        #region Logging Definition

        private static readonly ILog LOG = LogManager.GetLogger(typeof(RabbitBrokerAdmin));

        #endregion

        private string virtualHost;

        private RabbitTemplate rabbitTemplate;

        private RabbitAdmin rabbitAdmin;

        private ErlangTemplate erlangTemplate;

        public RabbitBrokerAdmin(CachingConnectionFactory connectionFactory)
        {
            this.virtualHost = connectionFactory.VirtualHost;
            this.rabbitTemplate = new RabbitTemplate(connectionFactory);
            this.rabbitAdmin = new RabbitAdmin(rabbitTemplate);
            InitializeDefaultErlangTemplate(rabbitTemplate);
        }

        private void InitializeDefaultErlangTemplate(RabbitTemplate template)
        {
            string peerNodeName = "rabbit@" + template.ConnectionFactory.Host;
            LOG.Debug("Creating Erlang.NET connection with peerNodeName = [" + peerNodeName + "]");
            SimpleConnectionFactory otpCf = new SimpleConnectionFactory("rabbit-spring-monitor-net", peerNodeName);
            otpCf.AfterPropertiesSet();
            CreateErlangTemplate(otpCf);
        }

        private void CreateErlangTemplate(SimpleConnectionFactory factory)
        {
            erlangTemplate = new ErlangTemplate(factory);
            erlangTemplate.ErlangConverter = new RabbitControlErlangConverter();
            erlangTemplate.AfterPropertiesSet();
        }

        #region Implementation of IRabbitBrokerOperations

        public RabbitStatus Status
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }

}