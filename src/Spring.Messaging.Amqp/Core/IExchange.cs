
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
    /// Common properties that describe all exchange types.  
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are typically used with administrative 
    /// operations that declare an exchange.
    /// </remarks>
    /// <author>Mark Pollack</author>
    public interface IExchange
    {
        /// <summary>
        /// Gets Name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets ExchangeType.
        /// </summary>
        string ExchangeType { get; }

        /// <summary>
        /// Gets a value indicating whether Durable.
        /// </summary>
        bool Durable { get; }

        /// <summary>
        /// Gets a value indicating whether AutoDelete.
        /// </summary>
        bool AutoDelete { get;  }

        /// <summary>
        /// Gets Arguments.
        /// </summary>
        IDictionary Arguments { get;  }

    }

}