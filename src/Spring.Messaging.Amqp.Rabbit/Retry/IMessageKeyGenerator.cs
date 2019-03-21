// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMessageKeyGenerator.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Core;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Retry
{
    /// <summary>
    /// A message key generator interface.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public interface IMessageKeyGenerator
    {
        /// <summary>Generate a unique key for the message that is repeatable on redelivery. Implementations should be very careful
        /// about assuming uniqueness of any element of the message, especially considering the requirement that it be
        /// repeatable. A message id is ideal, but may not be present (AMQP does not mandate it), and the message body is a
        /// byte array whose contents might be repeatable, but its object value is not.</summary>
        /// <param name="message">The message to generate a key for.</param>
        /// <returns>A unique key for this message.</returns>
        object GetKey(Message message);
    }
}
