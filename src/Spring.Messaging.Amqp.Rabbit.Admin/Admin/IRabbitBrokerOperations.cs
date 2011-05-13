
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
        // Queue operations

        /// <summary>
        /// Gets the queues.
        /// </summary>
        /// <returns>A list of queues.</returns>
        /// <remarks></remarks>
        List<QueueInfo> GetQueues();

        /// <summary>
        /// Gets the queues.
        /// </summary>
        /// <param name="virtualHost">The virtual host.</param>
        /// <returns>A list of queues.</returns>
        /// <remarks></remarks>
        List<QueueInfo> GetQueues(string virtualHost);
        
        // User management

        /// <summary>
        /// Adds the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <remarks></remarks>
        void AddUser(string username, string password);

        /// <summary>
        /// Deletes the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <remarks></remarks>
        void DeleteUser(string username);

        /// <summary>
        /// Changes the user password.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="newPassword">The new password.</param>
        /// <remarks></remarks>
        void ChangeUserPassword(string username, string newPassword);

        /// <summary>
        /// Lists the users.
        /// </summary>
        /// <returns>A list of users.</returns>
        /// <remarks></remarks>
        List<string> ListUsers();

        // VHost management

        /// <summary>
        /// Adds the vhost.
        /// </summary>
        /// <param name="vhostPath">The vhost path.</param>
        /// <returns>The value.</returns>
        /// <remarks></remarks>
        int AddVhost(string vhostPath);

        /// <summary>
        /// Deletes the vhost.
        /// </summary>
        /// <param name="vhostPath">The vhost path.</param>
        /// <returns>The value.</returns>
        /// <remarks></remarks>
        int DeleteVhost(string vhostPath);

        // permissions

        /// <summary>
        /// Sets the permissions.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="configure">The configure.</param>
        /// <param name="read">The read.</param>
        /// <param name="write">The write.</param>
        /// <remarks></remarks>
        void SetPermissions(string username, Regex configure, Regex read, Regex write);

        /// <summary>
        /// Sets the permissions.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="configure">The configure.</param>
        /// <param name="read">The read.</param>
        /// <param name="write">The write.</param>
        /// <param name="vhostPath">The vhost path.</param>
        /// <remarks></remarks>
        void SetPermissions(string username, Regex configure, Regex read, Regex write, string vhostPath);

        /// <summary>
        /// Clears the permissions.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <remarks></remarks>
        void ClearPermissions(string username);

        /// <summary>
        /// Clears the permissions.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="vhostPath">The vhost path.</param>
        /// <remarks></remarks>
        void ClearPermissions(string username, string vhostPath);

        /// <summary>
        /// Lists the permissions.
        /// </summary>
        /// <returns>A list of permissions.</returns>
        /// <remarks></remarks>
        IList<string> ListPermissions();

        /// <summary>
        /// Lists the permissions.
        /// </summary>
        /// <param name="vhostPath">The vhost path.</param>
        /// <returns>A list of permissions.</returns>
        /// <remarks></remarks>
        IList<string> ListPermissions(string vhostPath);

        /// <summary>
        /// Lists the user permissions.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>A list of user permissions.</returns>
        /// <remarks></remarks>
        IList<string> ListUserPermissions(string username);

        // Start/Stop/Reset broker

        /// <summary>
        /// Starts the broker application.
        /// </summary>
        /// Starts the RabbitMQ application on an already running node. This command is typically run after performing other
        /// management actions that required the RabbitMQ application to be stopped, e.g. reset.
        /// <remarks></remarks>
        void StartBrokerApplication();

        /// <summary>
        /// Stops the broker application.
        /// </summary>
        /// Stops the RabbitMQ application, leaving the Erlang node running.
        /// <remarks></remarks>
        void StopBrokerApplication();

        /// <summary>
        /// Starts the node.
        /// </summary>
        /// Starts the Erlang node where RabbitMQ is running by shelling out to the directory specified by RABBITMQ_HOME and
        /// executing the standard named start script. It spawns the shell command execution into its own thread.
        /// <remarks></remarks>
        void StartNode();

        /// <summary>
        /// Stops the node.
        /// </summary>
        /// Stops the halts the Erlang node on which RabbitMQ is running. To restart the node you will need to execute the
        /// start script from a command line or via other means.
        /// <remarks></remarks>
        void StopNode();

        /// <summary>
        /// Resets the node.
        /// Removes the node from any cluster it belongs to, removes all data from the management database, such as
        /// configured users and vhosts, and deletes all persistent messages.
        /// </summary>
        /// <remarks></remarks>
        void ResetNode();

        /// <summary>
        /// Forces the reset node.
        /// </summary>
        /// The forceResetNode command differs from {@link #resetNode} in that it resets the node unconditionally, regardless
        /// of the current management database state and cluster configuration. It should only be used as a last resort if
        /// the database or cluster configuration has been corrupted.
        /// <remarks></remarks>
        void ForceResetNode();

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <returns>The status of the node.</returns>
        /// Returns the status of the node.
        /// @return status of the node.
        /// <remarks></remarks>
        RabbitStatus GetStatus();
    }
}