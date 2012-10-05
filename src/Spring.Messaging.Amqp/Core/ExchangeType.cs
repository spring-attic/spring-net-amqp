// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeType.cs" company="The original author or authors.">
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

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Constants representing exchange types.
    /// </summary>
    /// <author>Mark Fisher</author>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public abstract class ExchangeTypes
    {
        /// <summary>
        /// Direct Exchange Type
        /// </summary>
        public const string Direct = "direct";

        /// <summary>
        /// Topic Exchange Type
        /// </summary>
        public const string Topic = "topic";

        /// <summary>
        /// Fanout Exchange Type
        /// </summary>
        public const string Fanout = "fanout";

        /// <summary>
        /// Headers Exchange Type
        /// </summary>
        public const string Headers = "headers";

        /// <summary>
        /// System Exchange Type
        /// </summary>
        public const string System = "system";

        /// <summary>
        /// Federated Exchange Type
        /// </summary>
        public const string Federated = "x-federation";
    }
}
