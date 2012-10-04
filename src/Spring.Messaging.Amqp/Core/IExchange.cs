// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IExchange.cs" company="The original author or authors.">
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
    /// Common properties that describe all exchange types.  
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are typically used with administrative 
    /// operations that declare an exchange.
    /// </remarks>
    /// <author>Mark Fisher</author>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public interface IExchange
    {
        /// <summary>
        /// The name of the exchange.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The type of the exchange. See <see cref="ExchangeTypes"/> for some well-known examples.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// A durable exchange will survive a server restart.
        /// </summary>
        bool Durable { get; }

        /// <summary>
        /// True if the server should delete the exchange when it is no longer in use (if all bindings are deleted).
        /// </summary>
        bool AutoDelete { get; }

        /// <summary>
        /// A dictionary of arguments used to declare the exchange. These are stored by the broker, but do not necessarily have any
        /// meaning to the broker (depending on the exchange type).
        /// </summary>
        IDictionary Arguments { get; }
    }
}
