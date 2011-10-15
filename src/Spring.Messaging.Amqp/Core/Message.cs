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

using System;
using System.Runtime.InteropServices;
using System.Text;
using Spring.Messaging.Amqp.Support.Converter;

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// The 0-8 and 0-9-1 AMQP specifications do not define an Message class or interface. Instead, when performing an operation such as 
    /// basicPublish the content is passed as a byte-array argument and additional properties are passed in as separate arguments. 
    /// Spring AMQP defines a Message class as part of a more general AMQP domain model representation. 
    /// The purpose of the Message class is to simply encapsulate the body and properties within a single 
    /// instance so that the rest of the AMQP API can in turn be simpler.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class Message 
    {
        private readonly string ENCODING = "utf-8";

        private readonly MessageProperties messageProperties;

        private readonly byte[] body;

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class. 
        /// </summary>
        /// <param name="body">
        /// The body.
        /// </param>
        /// <param name="messageProperties">
        /// The message Properties.
        /// </param>
        public Message(byte[] body, MessageProperties messageProperties)
        {
            this.body = body;
            this.messageProperties = messageProperties;
        }

        #region Implementation of IMessage

        /// <summary>
        /// Gets Body.
        /// </summary>
        public byte[] Body
        {
            get { return this.body; }
        }

        /// <summary>
        /// Gets MessageProperties.
        /// </summary>
        public MessageProperties MessageProperties
        {
            get { return this.messageProperties; }           
        }

        #endregion

        /// <summary>
        /// Format the message as a string.
        /// </summary>
        /// <returns>
        /// The string representation of the message.
        /// </returns>
        public new string ToString()
        {
            var buffer = new StringBuilder();
            buffer.Append("(");
            buffer.Append("Body:'" + this.GetBodyContentAsString() + "'");

            if (this.messageProperties != null)
            {
                buffer.Append("; ID:" + this.messageProperties.MessageId);
                buffer.Append("; Content:" + this.messageProperties.ContentType);
                buffer.Append("; Headers:" + this.messageProperties.Headers);
                buffer.Append("; Exchange:" + this.messageProperties.ReceivedExchange);
                buffer.Append("; RoutingKey:" + this.messageProperties.ReceivedRoutingKey);
                buffer.Append("; Reply:" + this.messageProperties.ReplyTo);
                buffer.Append("; DeliveryMode:" + this.messageProperties.DeliveryMode);
                buffer.Append("; DeliveryTag:" + this.messageProperties.DeliveryTag);
            }
            buffer.Append(")");

            return buffer.ToString();
        }

        /// <summary>
        /// Format the body content as a string.
        /// </summary>
        /// <returns>
        /// The string representation of the body content.
        /// </returns>
        private string GetBodyContentAsString()
        {
            if (this.body == null)
            {
                return null;
            }

            try
            {
                var contentType = (this.messageProperties != null) ? this.messageProperties.ContentType : string.Empty;
                if (MessageProperties.CONTENT_TYPE_SERIALIZED_OBJECT.Equals(contentType))
                {
                    return SerializationUtils.DeserializeObject(this.body).ToString();
                }

                if (MessageProperties.CONTENT_TYPE_TEXT_PLAIN.Equals(contentType))
                {
                    return SerializationUtils.DeserializeString(this.body, this.ENCODING);
                }

                if (MessageProperties.CONTENT_TYPE_JSON.Equals(contentType))
                {
                    return SerializationUtils.DeserializeJsonAsString(this.body, this.ENCODING);
                }
            }
            catch
            {
                // ignore
            }

            return this.body.ToString() + "(byte[" + this.body.Length + "])"; // Comes out as '[B@....b' (so harmless)
        }
    }
}