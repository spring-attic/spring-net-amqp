// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TopicExchange.cs" company="The original author or authors.">
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
using System.Collections.Generic;
#endregion

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Simple container collecting information to describe a topic exchange.
    /// </summary>
    /// <remarks>
    /// Used in conjunction with administrative operations.
    /// </remarks>
    /// <author>Mark Pollack</author>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    /// <see cref="IAmqpAdmin"/>
    public class TopicExchange : AbstractExchange
    {
        /// <summary>Initializes a new instance of the <see cref="TopicExchange"/> class.</summary>
        /// <param name="name">The name.</param>
        public TopicExchange(string name) : base(name) { }

        /// <summary>Initializes a new instance of the <see cref="TopicExchange"/> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="durable">The durable.</param>
        /// <param name="autoDelete">The auto delete.</param>
        public TopicExchange(string name, bool durable, bool autoDelete) : base(name, durable, autoDelete) { }

        /// <summary>Initializes a new instance of the <see cref="TopicExchange"/> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="durable">The durable.</param>
        /// <param name="autoDelete">The auto delete.</param>
        /// <param name="arguments">The arguments.</param>
        public TopicExchange(string name, bool durable, bool autoDelete, IDictionary<string, object> arguments) : base(name, durable, autoDelete, arguments) { }

        /// <summary>
        /// Gets Type.
        /// </summary>
        public override string Type { get { return ExchangeTypes.Topic; } }
    }
}
