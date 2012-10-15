// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BrokerRunning.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using Common.Logging;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Test
{
    /// <summary>The broker running.</summary>
    public class BrokerRunning
    {
        /// <summary>
        /// The default queue name.
        /// </summary>
        private static readonly string DEFAULT_QUEUE_NAME = typeof(BrokerRunning).Name;

        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        // The broker online flag. Static so that we only test once on failure: speeds up test suite.
        private static readonly IDictionary<int, bool> brokerOnline = new Dictionary<int, bool>();

        /// <summary>
        /// The broker offline flag. Static so that we only test once on failure.
        /// </summary>
        private static readonly IDictionary<int, bool> brokerOffline = new Dictionary<int, bool>();

        /// <summary>
        /// The assume online flag.
        /// </summary>
        private readonly bool assumeOnline;

        /// <summary>
        /// The purge flag.
        /// </summary>
        private readonly bool purge;

        /// <summary>
        /// The queues.
        /// </summary>
        private readonly Queue[] queues;

        /// <summary>
        /// The default port.
        /// </summary>
        private readonly int DEFAULT_PORT = BrokerTestUtils.GetPort();

        /// <summary>
        /// The port.
        /// </summary>
        private int port;

        /// <summary>
        /// The host name.
        /// </summary>
        private string hostName;

        /// <summary>Determines whether [is running with empty queues] [the specified names].</summary>
        /// <param name="names">The names.</param>
        /// Ensure the broker is running and has an empty queue with the specified name in the default exchange.
        /// @return a new rule that assumes an existing running broker
        /// <returns>The Spring.Messaging.Amqp.Rabbit.Tests.Test.BrokerRunning.</returns>
        public static BrokerRunning IsRunningWithEmptyQueues(params string[] names)
        {
            var queues = new Queue[names.Length];
            for (var i = 0; i < queues.Length; i++)
            {
                queues[i] = new Queue(names[i]);
            }

            return new BrokerRunning(true, true, queues);
        }

        /// <summary>Determines whether [is running with empty queues] [the specified queues].</summary>
        /// <param name="queues">The queues.</param>
        /// Ensure the broker is running and has an empty queue (which can be addressed via the default exchange).
        /// @return a new rule that assumes an existing running broker
        /// <returns>The Spring.Messaging.Amqp.Rabbit.Tests.Test.BrokerRunning.</returns>
        public static BrokerRunning IsRunningWithEmptyQueues(params Queue[] queues) { return new BrokerRunning(true, true, queues); }

        /// <summary>Determines whether this instance is running.</summary>
        /// @return a new rule that assumes an existing running broker
        /// <returns>The Spring.Messaging.Amqp.Rabbit.Tests.Test.BrokerRunning.</returns>
        public static BrokerRunning IsRunning() { return new BrokerRunning(true); }

        /// <summary>Determines whether [is not running].</summary>
        /// @return a new rule that assumes there is no existing broker
        /// <returns>The Spring.Messaging.Amqp.Rabbit.Tests.Test.BrokerRunning.</returns>
        public static BrokerRunning IsNotRunning() { return new BrokerRunning(false); }

        /// <summary>Initializes a new instance of the <see cref="BrokerRunning"/> class. 
        /// Prevents a default instance of the <see cref="BrokerRunning"/> class from being created.</summary>
        /// <param name="assumeOnline">if set to <c>true</c> [assume online].</param>
        /// <param name="purge">if set to <c>true</c> [purge].</param>
        /// <param name="queues">The queues.</param>
        private BrokerRunning(bool assumeOnline, bool purge, params Queue[] queues)
        {
            this.assumeOnline = assumeOnline;
            this.queues = queues;
            this.purge = purge;
            this.Port = this.DEFAULT_PORT;
        }

        /// <summary>Initializes a new instance of the <see cref="BrokerRunning"/> class. Prevents a default instance of the <see cref="BrokerRunning"/> class from being created.</summary>
        /// <param name="assumeOnline">if set to <c>true</c> [assume online].</param>
        /// <param name="queues">The queues.</param>
        private BrokerRunning(bool assumeOnline, params Queue[] queues) : this(assumeOnline, false, queues) { }

        /// <summary>Initializes a new instance of the <see cref="BrokerRunning"/> class. Prevents a default instance of the <see cref="BrokerRunning"/> class from being created.</summary>
        /// <param name="assumeOnline">if set to <c>true</c> [assume online].</param>
        private BrokerRunning(bool assumeOnline) : this(assumeOnline, new Queue(DEFAULT_QUEUE_NAME)) { }

        /// <summary>
        /// Sets a value indicating whether this <see cref="BrokerRunning"/> is port.
        /// </summary>
        /// <value><c>true</c> if port; otherwise, <c>false</c>.</value>
        /// @param port the port to set
        public int Port
        {
            set
            {
                this.port = value;
                if (!brokerOffline.ContainsKey(this.port))
                {
                    brokerOffline.Add(this.port, true);
                }

                if (!brokerOnline.ContainsKey(this.port))
                {
                    brokerOnline.Add(this.port, true);
                }
            }
        }

        /// <summary>
        /// Sets the name of the host.
        /// </summary>
        /// <value>The name of the host.</value>
        /// @param hostName the hostName to set
        public string HostName { set { this.hostName = value; } }

        /// <summary>
        /// Applies this instance.
        /// </summary>
        /// <returns>Something here.</returns>
        public bool Apply()
        {
            // Check at the beginning, so this can be used as a static field
            if (this.assumeOnline)
            {
                Assume.That(brokerOnline[this.port]);
            }
            else
            {
                Assume.That(brokerOffline[this.port]);
            }

            var connectionFactory = new CachingConnectionFactory();

            try
            {
                connectionFactory.Port = this.port;
                if (!string.IsNullOrWhiteSpace(this.hostName))
                {
                    connectionFactory.Host = this.hostName;
                }

                var admin = new RabbitAdmin(connectionFactory);

                foreach (var queue in this.queues)
                {
                    var queueName = queue.Name;

                    if (this.purge)
                    {
                        Logger.Debug("Deleting queue: " + queueName);

                        // Delete completely - gets rid of consumers and bindings as well
                        admin.DeleteQueue(queueName);
                    }

                    if (this.IsDefaultQueue(queueName))
                    {
                        // Just for test probe.
                        admin.DeleteQueue(queueName);
                    }
                    else
                    {
                        admin.DeclareQueue(queue);
                    }
                }

                if (brokerOffline.ContainsKey(this.port))
                {
                    brokerOffline[this.port] = false;
                }
                else
                {
                    brokerOffline.Add(this.port, false);
                }

                if (!this.assumeOnline)
                {
                    Assume.That(brokerOffline[this.port]);
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Not executing tests because basic connectivity test failed", e);
                if (brokerOnline.ContainsKey(this.port))
                {
                    brokerOnline[this.port] = false;
                }
                else
                {
                    brokerOnline.Add(this.port, false);
                }

                if (this.assumeOnline)
                {
                    return false;

                    // Assume.That(!(e is Exception));
                }
            }
            finally
            {
                connectionFactory.Dispose();
            }

            return true;

            // return base.Apply(base, method, target);
        }

        /// <summary>Determines whether [is default queue] [the specified queue].</summary>
        /// <param name="queue">The queue.</param>
        /// <returns><c>true</c> if [is default queue] [the specified queue]; otherwise, <c>false</c>.</returns>
        private bool IsDefaultQueue(string queue) { return DEFAULT_QUEUE_NAME == queue; }
    }
}
