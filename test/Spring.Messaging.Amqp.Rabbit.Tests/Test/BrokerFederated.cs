// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BrokerFederated.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Rabbit.Support;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Test
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class BrokerFederated
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        // Static so that we only test once on failure: speeds up test suite
        private static readonly IDictionary<int, bool> BrokerOnline = new Dictionary<int, bool>();

        // Static so that we only test once on failure
        private static readonly IDictionary<int, bool> BrokerOffline = new Dictionary<int, bool>();

        private readonly bool assumeOnline;

        private readonly int DEFAULT_PORT = BrokerTestUtils.GetPort();

        private int port;

        private string hostName = string.Empty;

        /**
         * @return a new rule that assumes an existing running broker
         */

        /// <summary>The is running.</summary>
        /// <returns>The Spring.Messaging.Amqp.Rabbit.Tests.Test.BrokerFederated.</returns>
        public static BrokerFederated IsRunning() { return new BrokerFederated(); }

        private BrokerFederated()
        {
            this.assumeOnline = true;
            this.Port = this.DEFAULT_PORT;
        }

        /**
         * @param port the port to set
         */

        /// <summary>Sets the port.</summary>
        public int Port
        {
            set
            {
                this.port = value;
                if (!BrokerOffline.ContainsKey(this.port))
                {
                    BrokerOffline.AddOrUpdate(this.port, true);
                }

                if (!BrokerOnline.ContainsKey(this.port))
                {
                    BrokerOnline.AddOrUpdate(this.port, true);
                }
            }
        }

        /**
         * @param hostName the hostName to set
         */

        /// <summary>Sets the host name.</summary>
        public string HostName { set { this.hostName = value; } }

        /// <summary>The apply.</summary>
        /// <returns>The System.Boolean.</returns>
        public bool Apply()
        {
            // Check at the beginning, so this can be used as a static field
            if (this.assumeOnline)
            {
                Assume.That(BrokerOnline.Get(this.port));
            }
            else
            {
                Assume.That(BrokerOffline.Get(this.port));
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
                var exchange = new FederatedExchange("fedDirectRuleTest");
                exchange.BackingType = "direct";
                exchange.UpstreamSet = "upstream-set";
                admin.DeclareExchange(exchange);
                admin.DeleteExchange("fedDirectRuleTest");

                BrokerOffline.AddOrUpdate(this.port, false);

                if (!this.assumeOnline)
                {
                    Assume.That(BrokerOffline.Get(this.port));
                }
            }
            catch (Exception e)
            {
                Logger.Warn(m => m("Not executing tests because federated connectivity test failed"), e);
                BrokerOnline.AddOrUpdate(this.port, false);
                if (this.assumeOnline)
                {
                    Assume.That(e == null, "An exception occurred.");
                    return false;
                }
            }
            finally
            {
                connectionFactory.Dispose();
            }

            return true;

            // return super.apply(base, method, target);
        }
    }
}
