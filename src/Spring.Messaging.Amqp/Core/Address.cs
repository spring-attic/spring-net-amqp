// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Address.cs" company="The original author or authors.">
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
using System.Text;
using System.Text.RegularExpressions;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Represents an address for publication of an AMQP message. The AMQP 0-8 and
    /// 0-9 specifications have an unstructured string that is used as a "reply to"
    /// address. There are however conventions in use and this class makes it easier
    /// to follow these conventions.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Mark Fisher</author>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class Address
    {
        public static readonly Regex pattern = new Regex("^([^:]+)://([^/]*)/?(.*)$");

        private readonly string exchangeType;

        private readonly string exchangeName;

        private readonly string routingKey;

        /// <summary>Initializes a new instance of the <see cref="Address"/> class from an unstructured string</summary>
        /// <param name="address">The unstructured address.</param>
        public Address(string address)
        {
            if (address == null)
            {
                this.exchangeType = ExchangeTypes.Direct;
                this.exchangeName = string.Empty;
                this.routingKey = string.Empty;
            }
            else
            {
                var match = pattern.Match(address);
                if (match.Success)
                {
                    this.exchangeType = match.Groups[1].Value;
                    this.exchangeName = match.Groups[2].Value;
                    this.routingKey = match.Groups[3].Value;
                }
                else
                {
                    this.exchangeType = ExchangeTypes.Direct;
                    this.exchangeName = string.Empty;
                    this.routingKey = address;
                }
            }
        }

        /// <summary>Initializes a new instance of the <see cref="Address"/> class given the exchange type,
        ///  exchange name and routing key. This will set the exchange type, name and the routing key explicitly.</summary>
        /// <param name="exchangeType">Type of the exchange.</param>
        /// <param name="exchangeName">Name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        public Address(string exchangeType, string exchangeName, string routingKey)
        {
            this.exchangeType = exchangeType;
            this.exchangeName = exchangeName;
            this.routingKey = routingKey;
        }

        /// <summary>Gets the exchange type.</summary>
        public string ExchangeType { get { return this.exchangeType; } }

        /// <summary>Gets the exchange name.</summary>
        public string ExchangeName { get { return this.exchangeName; } }

        /// <summary>Gets the routing key.</summary>
        public string RoutingKey { get { return this.routingKey; } }

        /// <summary>The to string.</summary>
        /// <returns>The System.String.</returns>
        public override string ToString()
        {
            return string.Format("{0}://{1}/{2}", this.exchangeType.ToLower(), this.exchangeName, string.IsNullOrWhiteSpace(this.routingKey) ? string.Empty : this.routingKey);
        }
    }
}
