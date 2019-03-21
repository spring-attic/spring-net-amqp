// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BrokerTestUtils.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System.Net;
using Spring.Messaging.Amqp.Rabbit.Admin;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Test
{
    /// <summary>
    /// Global convenience class for all integration tests, carrying constants and other utilities for broker set up.
    /// </summary>
    /// <author>Dave Syer</author>
    public class BrokerTestUtils
    {
        /// <summary>
        /// The default port.
        /// </summary>
        public static readonly int DEFAULT_PORT = 5672;

        /// <summary>
        /// The tracer port.
        /// </summary>
        public static readonly int TRACER_PORT = 5673;

        /// <summary>
        /// The admin node name.
        /// </summary>
        // public static readonly string ADMIN_NODE_NAME = @"rabbit@localhost";
        public static readonly string ADMIN_NODE_NAME = GetAdminNodeName();

        /// <summary>
        /// Gets the name of the admin node.
        /// </summary>
        /// <returns>The admin node name.</returns>
        public static string GetAdminNodeName()
        {
            var hostName = Dns.GetHostName();
            hostName = string.IsNullOrEmpty(hostName) ? "LOCALHOST" : hostName.Trim();
            return "rabbit@" + hostName;
        }

        /// <summary>Gets the port.</summary>
        /// <returns>The System.Int32.</returns>
        /// The port that the broker is listening on (e.g. as input for a {@link ConnectionFactory}).
        /// @return a port number
        public static int GetPort() { return DEFAULT_PORT; }

        /// <summary>Gets the tracer port.</summary>
        /// <returns>The System.Int32.</returns>
        /// The port that the tracer is listening on (e.g. as input for a {@link ConnectionFactory}).
        /// @return a port number
        public static int GetTracerPort() { return TRACER_PORT; }

        /// <summary>Gets the admin port.</summary>
        /// <returns>The System.Int32.</returns>
        /// An alternative port number than can safely be used to stop and start a broker, even when one is already running
        /// on the standard port as a privileged user. Useful for tests involving {@link RabbitBrokerAdmin} on UN*X.
        /// @return a port number
        public static int GetAdminPort()
        {
            // This *used* to be some magic value of '15672' but this would require special config to *ever* work on any system
            // as noted here http://lists.rabbitmq.com/pipermail/rabbitmq-discuss/2009-November/005501.html
            // so am returning it to the default port for now to get tests to pass (reliably!)
            return DEFAULT_PORT;
        }

        /// <summary>Gets the rabbit broker admin.</summary>
        /// <returns>The Spring.Messaging.Amqp.Rabbit.Admin.RabbitBrokerAdmin.</returns>
        /// Convenience factory for a {@link RabbitBrokerAdmin} instance that will usually start and stop cleanly on all
        /// systems.
        /// @return a {@link RabbitBrokerAdmin} instance
        public static RabbitBrokerAdmin GetRabbitBrokerAdmin() { return GetRabbitBrokerAdmin(GetAdminNodeName(), GetPort()); }

        /// <summary>Gets the rabbit broker admin.</summary>
        /// <param name="nodeName">Name of the node.</param>
        /// <returns>The Spring.Messaging.Amqp.Rabbit.Admin.RabbitBrokerAdmin.</returns>
        /// Convenience factory for a {@link RabbitBrokerAdmin} instance that will usually start and stop cleanly on all
        /// systems.
        /// @param nodeName the name of the node
        /// @return a {@link RabbitBrokerAdmin} instance
        public static RabbitBrokerAdmin GetRabbitBrokerAdmin(string nodeName) { return GetRabbitBrokerAdmin(nodeName, GetPort()); }

        /// <summary>Gets the rabbit broker admin.</summary>
        /// <param name="nodeName">Name of the node.</param>
        /// <param name="port">The port.</param>
        /// <returns>The Spring.Messaging.Amqp.Rabbit.Admin.RabbitBrokerAdmin.</returns>
        /// Convenience factory for a {@link RabbitBrokerAdmin} instance that will usually start and stop cleanly on all
        /// systems.
        /// @param nodeName the name of the node
        /// @param port the port to listen on
        /// @return a {@link RabbitBrokerAdmin} instance
        public static RabbitBrokerAdmin GetRabbitBrokerAdmin(string nodeName, int port)
        {
            var brokerAdmin = new RabbitBrokerAdmin(nodeName, port);
            brokerAdmin.RabbitLogBaseDirectory = "target/rabbitmq/log";
            brokerAdmin.RabbitMnesiaBaseDirectory = "target/rabbitmq/mnesia";
            brokerAdmin.StartupTimeout = 10000L;
            return brokerAdmin;
        }
    }
}
