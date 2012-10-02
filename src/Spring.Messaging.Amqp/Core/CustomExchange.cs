// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CustomExchange.cs" company="The original author or authors.">
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
#endregion

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Simple container collecting information to describe a custom exchange. Custom exchange types are allowed by the AMQP
    /// specification, and their names should start with "x-" (but this is not enforced here). Used in conjunction with
    /// administrative operations.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public class CustomExchange : AbstractExchange
    {
        /// <summary>
        /// The type.
        /// </summary>
        private readonly string type;

        /// <summary>Initializes a new instance of the <see cref="CustomExchange"/> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        public CustomExchange(string name, string type) : base(name) { this.type = type; }

        /// <summary>Initializes a new instance of the <see cref="CustomExchange"/> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="durable">The durable.</param>
        /// <param name="autoDelete">The auto delete.</param>
        public CustomExchange(string name, string type, bool durable, bool autoDelete) : base(name, durable, autoDelete) { this.type = type; }

        /// <summary>Initializes a new instance of the <see cref="CustomExchange"/> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="durable">The durable.</param>
        /// <param name="autoDelete">The auto delete.</param>
        /// <param name="arguments">The arguments.</param>
        public CustomExchange(string name, string type, bool durable, bool autoDelete, IDictionary arguments) : base(name, durable, autoDelete, arguments) { this.type = type; }

        #region Overrides of AbstractExchange

        /// <summary>
        /// Gets Type.
        /// </summary>
        public override string Type { get { return this.type; } }
        #endregion
    }
}
