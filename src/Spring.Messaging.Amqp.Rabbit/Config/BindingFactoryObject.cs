// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BindingFactoryObject.cs" company="The original author or authors.">
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
using System.Collections;
using Common.Logging;
using Spring.Messaging.Amqp.Core;
using Spring.Objects.Factory;
using Queue = Spring.Messaging.Amqp.Core.Queue;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A binding factory object.
    /// </summary>
    public class BindingFactoryObject : IFactoryObject
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private IDictionary arguments;
        private string routingKey = string.Empty;
        private string exchange;
        private Queue destinationQueue;
        private IExchange destinationExchange;

        /// <summary>Initializes a new instance of the <see cref="BindingFactoryObject"/> class.</summary>
        public BindingFactoryObject() { Logger.Debug("Creating new BindingFactoryObject"); }

        /// <summary>
        /// Sets the arguments.
        /// </summary>
        /// <value>The arguments.</value>
        /// <author>Dave Syer</author>
        /// <author>Joe Fitzgerald (.NET)</author>
        public IDictionary Arguments { set { this.arguments = value; } }

        /// <summary>
        /// Sets the routing key.
        /// </summary>
        /// <value>The routing key.</value>
        public string RoutingKey { set { this.routingKey = value; } }

        /// <summary>
        /// Sets the exchange.
        /// </summary>
        /// <value>The exchange.</value>
        public string Exchange { set { this.exchange = value; } }

        /// <summary>
        /// Sets the destination queue.
        /// </summary>
        /// <value>The destination queue.</value>
        public Queue DestinationQueue { set { this.destinationQueue = value; } }

        /// <summary>
        /// Sets the destination exchange.
        /// </summary>
        /// <value>The destination exchange.</value>
        public IExchange DestinationExchange { set { this.destinationExchange = value; } }

        /// <summary>The get object.</summary>
        /// <returns>The System.Object.</returns>
        public object GetObject()
        {
            string destination;
            Binding.DestinationType destinationType;
            if (this.destinationQueue != null)
            {
                destination = this.destinationQueue.Name;
                destinationType = Binding.DestinationType.Queue;
            }
            else
            {
                destination = this.destinationExchange.Name;
                destinationType = Binding.DestinationType.Exchange;
            }

            return new Binding(destination, destinationType, this.exchange, this.routingKey, this.arguments);
        }

        /// <summary>Gets a value indicating whether is singleton.</summary>
        public bool IsSingleton { get { return true; } }

        /// <summary>Gets the object type.</summary>
        public Type ObjectType { get { return typeof(Binding); } }
    }
}
