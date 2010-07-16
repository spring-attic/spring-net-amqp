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
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Erlang.Connection;
using Spring.Erlang.Core;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using IConnectionFactory=Spring.Messaging.Amqp.Rabbit.Connection.IConnectionFactory;

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

        private ASCIIEncoding encoding = new ASCIIEncoding();

        public RabbitBrokerAdmin(IConnectionFactory connectionFactory)
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

        public void RemoveBinding(Binding binding)
        {
            rabbitTemplate.Execute<object>(delegate(IModel model)
                                               {
                                                   model.QueueUnbind(binding.Queue, binding.Exchange, binding.RoutingKey,
                                                                     binding.Arguments);
                                                   return null;
                                               });
        }

        public RabbitStatus Status
        {
            get
            {
                return (RabbitStatus)erlangTemplate.ExecuteAndConvertRpc("rabbit", "status");
            }
        }

        public IList<QueueInfo> Queues
        {
            get {
                return
                    (IList<QueueInfo>)
                    erlangTemplate.ExecuteAndConvertRpc("rabbit_amqqueue", "info_all", encoding.GetBytes(virtualHost)); }
        }

        public void AddUser(string username, string password)
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit_access_control", "add_user", encoding.GetBytes(username), encoding.GetBytes(password));
        }

        public void DeleteUser(string username)
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit_access_control", "delete_user", encoding.GetBytes(username));
        }

        public void ChangeUserPassword(string username, string newPassword)
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit_access_control", "change_password", encoding.GetBytes(username),
                                                encoding.GetBytes(newPassword));		

        }

        public IList<string> ListUsers()
        {
            return (IList<string>)erlangTemplate.ExecuteAndConvertRpc("rabbit_access_control", "list_users");
        }

        #endregion

        public void DeclareExchange(IExchange exchange)
        {
            rabbitAdmin.DeclareExchange(exchange);
        }

        public void DeleteExchange(string exchangeName)
        {
            rabbitAdmin.DeleteExchange(exchangeName);
        }

        public Queue DeclareQueue()
        {
            return rabbitAdmin.DeclareQueue();
        }

        public void DeclareQueue(Queue queue)
        {
            rabbitAdmin.DeclareQueue(queue);
        }

        public void DeleteQueue(string queueName)
        {
            rabbitAdmin.DeleteQueue(queueName);
        }

        public void DeleteQueue(string queueName, bool unused, bool empty)
        {
            rabbitAdmin.DeleteQueue(queueName, unused, empty);
        }

        public void PurgeQueue(string queueName, bool noWait)
        {
            rabbitAdmin.PurgeQueue(queueName, noWait);
        }

        public void DeclareBinding(Binding binding)
        {
            rabbitAdmin.DeclareBinding(binding);
        }

        public void StartBrokerApplication()
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit", "start");
        }

        public void StopBrokerApplication()
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit", "stop");
        }

        /// <summary>
        /// Starts the node. NOT YET IMPLEMENTED!
        /// </summary>
        /// Starts the Erlang node where RabbitMQ is running by shelling out to the directory specified by RABBIT_HOME and
        /// executing the standard named start script.  It spawns the shell command execution into its own thread.
        public void StartNode()
        {
            throw new NotImplementedException();
        }

        public void StopNode()
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit", "stop_and_halt");
        }

        public void ResetNode()
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit_mnesia", "reset");
        }

        public void ForceResetNode()
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit_mnesia", "force_reset");
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public int AddVhost(string vhostPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public int DeleteVhost(string vhostPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public void SetPermissions(string username, Regex configure, Regex read, Regex write)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public void SetPermissions(string username, Regex configure, Regex read, Regex write, string vhostPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public void ClearPermissions(string username)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public void ClearPermissions(string username, string vhostPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public List<string> ListPermissions()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public List<string> ListPermissions(string vhostPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public List<string> ListUserPermissions(string username)
        {
            throw new NotImplementedException();
        }
    }

}