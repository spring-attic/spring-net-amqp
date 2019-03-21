// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FatalListenerStartupException.cs" company="The original author or authors.">
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
using System;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    /// Exception to be thrown when the execution of a listener method failed on startup.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class FatalListenerStartupException : AmqpException
    {
        /// <summary>Initializes a new instance of the <see cref="FatalListenerStartupException"/> class.</summary>
        /// <param name="msg">The msg.</param>
        /// <param name="cause">The cause.</param>
        public FatalListenerStartupException(string msg, Exception cause) : base(msg, cause) { }
    }
}
