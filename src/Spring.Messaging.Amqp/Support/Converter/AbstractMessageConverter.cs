// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AbstractMessageConverter.cs" company="The original author or authors.">
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

namespace Spring.Messaging.Amqp.Support.Converter
{
    /// <summary>
    /// Convenient base class for IMessageConverter implementations.
    /// </summary>
    /// <author>Joe Fitzgerald</author>
    public abstract class AbstractMessageConverter : IMessageConverter
    {
        private bool createMessageIds;

        /// <summary>
        /// Gets or sets a value indicating whether new messages should have unique identifiers added to their properties before sending. Default is false.
        /// </summary>
        public bool CreateMessageIds { get { return this.createMessageIds; } set { this.createMessageIds = value; } }

        /// <summary>Create a message from the object with properties.</summary>
        /// <param name="obj">The obj.</param>
        /// <param name="messageProperties">The message properties.</param>
        /// <returns>The Message.</returns>
        public Message ToMessage(object obj, MessageProperties messageProperties)
        {
            if (messageProperties == null)
            {
                messageProperties = new MessageProperties();
            }

            var message = this.CreateMessage(obj, messageProperties);
            messageProperties = message.MessageProperties;
            if (this.createMessageIds && messageProperties.MessageId == null)
            {
                messageProperties.MessageId = Guid.NewGuid().ToString();
            }

            return message;
        }

        /// <summary>Create an object from the message.</summary>
        /// <param name="message">The message.</param>
        /// <returns>The object.</returns>
        public abstract object FromMessage(Message message);

        /// <summary>Crate a message from the payload object and message properties provided. The message id will be added to the
        /// properties if necessary later.</summary>
        /// <param name="obj">The obj.</param>
        /// <param name="messageProperties">The message properties.</param>
        /// <returns>The Message.</returns>
        protected abstract Message CreateMessage(object obj, MessageProperties messageProperties);
    }
}
