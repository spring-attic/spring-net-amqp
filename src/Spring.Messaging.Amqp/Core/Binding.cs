
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

using System.Collections;

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Simple container collecting information to describe a queue binding. Takes Queue and Exchange
    /// class as arguments to facilitate wiring using [Definition] based configuration.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class Binding
    {
        /// <summary>
        /// The queue.
        /// </summary>
        private readonly string destination;

        /// <summary>
        /// The exchange.
        /// </summary>
        private readonly string exchange;

        /// <summary>
        /// The routing key.
        /// </summary>
        private readonly string routingKey;

        /// <summary>
        /// The arguments.
        /// </summary>
        private readonly IDictionary arguments;

        /// <summary>
        /// The destination type.
        /// </summary>
        private readonly DestinationType destinationType;


        /// <summary>
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        /// <param name="destination">
        /// The destination.
        /// </param>
        /// <param name="destinationType">
        /// The destination type.
        /// </param>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="arguments">
        /// The arguments.
        /// </param>
        public Binding(string destination, DestinationType destinationType, string exchange, string routingKey, IDictionary arguments)
        {
            this.destination = destination;
            this.destinationType = destinationType;
            this.exchange = exchange;
            this.routingKey = routingKey;
            this.arguments = arguments;
        }

        /// <summary>
        /// Binding destination types.
        /// </summary>
        public enum DestinationType
        {
            /// <summary>
            /// Queue destination type.
            /// </summary>
            Queue,

            /// <summary>
            /// Exchange destination type.
            /// </summary>
            Exchange
        }

        /// <summary>
        /// Gets the destination.
        /// </summary>
        public string Destination
        {
            get { return this.destination; }
        }

        /// <summary>
        /// Gets Exchange.
        /// </summary>
        public string Exchange
        {
            get { return this.exchange; }
        }

        /// <summary>
        /// Gets RoutingKey.
        /// </summary>
        public string RoutingKey
        {
            get { return this.routingKey; }
        }

        /// <summary>
        /// Gets Arguments.
        /// </summary>
        public IDictionary Arguments
        {
            get { return this.arguments; }
        }

        /// <summary>
        /// Gets DestinationType.
        /// </summary>
        public DestinationType BindingDestinationType
        {
            get { return this.destinationType; }
        }

        /// <summary>
        /// Is the destination a Queue (as compared to an Exchange as specified by the DestinationType enumeration)
        /// </summary>
        /// <returns>true if the destination is a Queue, false otherwise</returns>
        public bool IsDestinationQueue()
        {
            return DestinationType.Queue == this.destinationType;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        /// <remarks></remarks>
        public override string ToString()
        {
            return "Binding [destination=" + this.destination + ", exchange=" + this.exchange + ", routingKey=" + this.routingKey + "]";
        }
    }
}