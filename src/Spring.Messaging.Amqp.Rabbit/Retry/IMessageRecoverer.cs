// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMessageRecoverer.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Core;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Retry
{
    /// <summary>
    /// A message recoverer interface.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public interface IMessageRecoverer
    {
        /// <summary>Callback for message that was consumed but failed all retry attempts.</summary>
        /// <param name="message">The message to recover.</param>
        /// <param name="cause">The cause of the error.</param>
        void Recover(Message message, Exception cause);
    }
}
