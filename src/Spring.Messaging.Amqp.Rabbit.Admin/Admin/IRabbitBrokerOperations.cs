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

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>
    /// Performs administration tasks for RabbitMQ broker administration.   
    /// </summary>
    /// <remarks>
    /// Goal is to support full CRUD of Exchanges, Queues, Bindings, User, VHosts, etc.
    /// </remarks>
    /// <author>Mark Pollack</author>
    public interface IRabbitBrokerOperations : IAmqpAdmin
    {

        void RemoveBinding(Binding binding);

        RabbitStatus Status { get; }

        IList<QueueInfo> Queues { get; }

        // User management

        void AddUser(string username, string password);

        void DeleteUser(string username);

        void ChangeUserPassword(string username, string newPassword);

        IList<string> ListUsers();

        /// <summary>
        /// Starts the broker application.
        /// </summary>
        /// Starts the RabbitMQ application on an already running node. This command is typically run after performing other
        /// management actions that required the RabbitMQ application to be stopped, e.g. reset.
        void StartBrokerApplication();

        /// <summary>
        /// Stops the broker application.
        /// </summary>
        /// Stops the RabbitMQ application, leaving the Erlang node running.
        void StopBrokerApplication();

        /// <summary>
        /// Starts the node. NOT YET IMPLEMENTED!
        /// </summary>
        /// Starts the Erlang node where RabbitMQ is running by shelling out to the directory specified by RABBIT_HOME and
        /// executing the standard named start script.  It spawns the shell command execution into its own thread.
        void StartNode();

        /// <summary>
        /// Stops the node.
        /// </summary>
        /// Stops the halts the Erlang node on which RabbitMQ is running.  To restart the node you will need to execute
        /// the start script from a command line or via other means.
        void StopNode();

        /// <summary>
        /// Resets the node.
        /// </summary>
        /// Removes the node from any cluster it belongs to, removes all data from the management database,
        /// such as configured users and vhosts, and deletes all persistent messages.
        /// <p>
        /// For <see cref="ResetNode"/> and <see cref="forceResetNode"/>to succeed the RabbitMQ application must have
        /// been stopped, e.g. <see cref="StopBrokerApplication"/></p>        
        void ResetNode();

        /// <summary>
        /// Forces the reset node.
        /// </summary>
        /// The forceResetNode command differs from <see cref="ResetNode"/> in that it resets the node unconditionally,
        /// regardless of the current management database state and cluster configuration. It should only be
        /// used as a last resort if the database or cluster configuration has been corrupted.
        /// <p>
        /// For <see cref="ResetNode"/> and <see cref="ForceResetNode"/> to succeed the RabbitMQ application must have
        /// been stopped, e.g. <see cref="StopBrokerApplication"/> </p>
        void ForceResetNode();


        // VHost management

        /// <summary>
        /// Not yet implemented
        /// </summary>
        int AddVhost(string vhostPath);

        /// <summary>
        /// Not yet implemented
        /// </summary>
        int DeleteVhost(string vhostPath);

        // permissions

        /// <summary>
        /// Not yet implemented
        /// </summary>
        void SetPermissions(string username, Regex configure, Regex read, Regex write);

        /// <summary>
        /// Not yet implemented
        /// </summary>
        void SetPermissions(string username, Regex configure, Regex read, Regex write, string vhostPath);

        /// <summary>
        /// Not yet implemented
        /// </summary>
        void ClearPermissions(string username);

        /// <summary>
        /// Not yet implemented
        /// </summary>
        void ClearPermissions(string username, string vhostPath);

        /// <summary>
        /// Not yet implemented
        /// </summary>
        List<string> ListPermissions();

        /// <summary>
        /// Not yet implemented
        /// </summary>
        List<string> ListPermissions(string vhostPath);

        /// <summary>
        /// Not yet implemented
        /// </summary>
        List<string> ListUserPermissions(string username);
    }

}