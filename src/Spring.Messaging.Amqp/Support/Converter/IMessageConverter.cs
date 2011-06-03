
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

using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Support.Converter
{
    /// <summary>
    /// Interface for message conversion.
    /// </summary>
    /// <author>Joe Fitzgerald</author>
    public interface IMessageConverter
    {
        /// <summary>
        /// Convert a .NET object to an AMQP Message.
        /// </summary>
        /// <param name="obj">
        /// The object to convert.
        /// </param>
        /// <param name="messageProperties">
        /// The message properties.
        /// </param>
        /// <returns>
        /// The AMQP Message.
        /// </returns>
        Message ToMessage(object obj, MessageProperties messageProperties);

        /// <summary>
        /// Convert from an AMQP Message to a .NET object.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The .NET object.
        /// </returns>
        object FromMessage(Message message);
    }
}