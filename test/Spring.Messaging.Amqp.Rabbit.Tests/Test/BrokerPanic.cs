// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BrokerPanic.cs" company="The original author or authors.">
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
using Common.Logging;
using Spring.Messaging.Amqp.Rabbit.Admin;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Test
{
    /// <summary>
    /// A broker panic rule.
    /// </summary>
    public class BrokerPanic
    {
        private readonly ILog logger = LogManager.GetCurrentClassLogger();

        private RabbitBrokerAdmin brokerAdmin;

        /// <summary>
        /// Sets the broker admin.
        /// </summary>
        /// <value>The broker admin.</value>
        public RabbitBrokerAdmin BrokerAdmin { set { this.brokerAdmin = value; } }

        /// <summary>
        /// Applies this instance.
        /// </summary>
        public void Apply()
        {
            if (this.brokerAdmin != null)
            {
                try
                {
                    this.brokerAdmin.StopNode();
                }
                catch (Exception e)
                {
                    // don't hide original error (so ignored)
                    this.logger.Error("Error occurred stopping node", e);
                }
            }
        }
    }
}
