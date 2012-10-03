// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FederatedExchange.cs" company="The original author or authors.">
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
using System.Collections;
using System.Collections.Generic;
#endregion

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// <see cref="IAmqpAdmin"/>
    /// </summary>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald</author>
    public class FederatedExchange : AbstractExchange
    {
        public static readonly FederatedExchange DEFAULT = new FederatedExchange(string.Empty);

        private static readonly string BACKING_TYPE_ARG = "type";

        private static readonly string UPSTREAM_SET_ARG = "upstream-set";

        /// <summary>Initializes a new instance of the <see cref="FederatedExchange"/> class.</summary>
        /// <param name="name">The name.</param>
        public FederatedExchange(string name) : base(name) { }

        /// <summary>Initializes a new instance of the <see cref="FederatedExchange"/> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="durable">The durable.</param>
        /// <param name="autoDelete">The auto delete.</param>
        public FederatedExchange(string name, bool durable, bool autoDelete) : base(name, durable, autoDelete) { }

        /// <summary>Initializes a new instance of the <see cref="FederatedExchange"/> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="durable">The durable.</param>
        /// <param name="autoDelete">The auto delete.</param>
        /// <param name="arguments">The arguments.</param>
        public FederatedExchange(string name, bool durable, bool autoDelete, IDictionary<string, object> arguments) : base(name, durable, autoDelete, arguments) { }

        /// <summary>Sets the backing type.</summary>
        public string BackingType { set { this.AddArgument(BACKING_TYPE_ARG, value); } }

        /// <summary>Sets the upstream set.</summary>
        public string UpstreamSet { set { this.AddArgument(UPSTREAM_SET_ARG, value); } }

        /// <summary>Gets the exchange type.</summary>
        public override string Type { get { return ExchangeTypes.Federated; } }
    }
}
