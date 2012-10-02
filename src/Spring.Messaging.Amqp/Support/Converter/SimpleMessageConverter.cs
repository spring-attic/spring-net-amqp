// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleMessageConverter.cs" company="The original author or authors.">
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
    /// Implementation of MessageConverter that can work with Strings, Serializable instances,
    /// or byte arrays. The ToMessage method simply checks the type of the provided instance while 
    /// the FromMessage method relies upon the content-type of the provided Message.
    /// </summary>
    /// <author>Mark Fisher</author>
    /// <author>Joe Fitzgerald</author>
    public class SimpleMessageConverter : AbstractMessageConverter
    {
        public static readonly string DEFAULT_CHARSET = "UTF-8";

        private volatile string defaultCharset = DEFAULT_CHARSET;

        private string codebaseUrl;

        /// <summary>
        /// Gets or sets the codebase URL to download classes from if not found locally. Can consists of multiple URLs, separated by
        /// spaces.
        /// </summary>
        public string CodebaseUrl { get { return this.codebaseUrl; } set { this.codebaseUrl = value; } }

        /// <summary>
        /// Gets or sets the default charset to use when converting to or from text-based
        /// Message body content. If not specified, the charset will be "UTF-8".
        /// </summary>
        public string DefaultCharset { get { return this.defaultCharset; } set { this.defaultCharset = value ?? DEFAULT_CHARSET; } }

        /// <summary>Converts from a AMQP Message to an Object.</summary>
        /// <param name="message">The message.</param>
        /// <returns>The object.</returns>
        /// <exception cref="MessageConversionException"></exception>
        public override object FromMessage(Message message)
        {
            object content = null;
            var properties = message.MessageProperties;

            if (properties != null)
            {
                var contentType = properties.ContentType;
                if (contentType != null && contentType.StartsWith("text"))
                {
                    string encoding = properties.ContentEncoding ?? this.defaultCharset;
                    try
                    {
                        content = SerializationUtils.DeserializeString(message.Body, encoding);
                    }
                    catch (Exception e)
                    {
                        throw new MessageConversionException("failed to convert text-based Message content", e);
                    }
                }
                else if (contentType != null && contentType == MessageProperties.CONTENT_TYPE_SERIALIZED_OBJECT)
                {
                    try
                    {
                        content = SerializationUtils.DeserializeObject(message.Body);
                    }
                    catch (Exception e)
                    {
                        throw new MessageConversionException("failed to convert serialized Message content", e);
                    }
                }
            }

            content = content ?? (content = message.Body);
            return content;
        }

        /// <summary>Creates an AMQP Message from the provided Object.</summary>
        /// <param name="obj">The obj.</param>
        /// <param name="messageProperties">The message properties.</param>
        /// <returns>The message.</returns>
        /// <exception cref="MessageConversionException"></exception>
        protected override Message CreateMessage(object obj, MessageProperties messageProperties)
        {
            byte[] bytes = null;

            if (obj is byte[])
            {
                bytes = (byte[])obj;
                messageProperties.ContentType = MessageProperties.CONTENT_TYPE_BYTES;
            }
            else if (obj is string)
            {
                try
                {
                    bytes = SerializationUtils.SerializeString((string)obj, this.defaultCharset);
                }
                catch (Exception e)
                {
                    throw new MessageConversionException("failed to convert to Message content", e);
                }

                messageProperties.ContentType = MessageProperties.CONTENT_TYPE_TEXT_PLAIN;
                messageProperties.ContentEncoding = this.defaultCharset;
            }
            else if (obj.GetType().IsSerializable || (obj != null && obj.GetType().IsSerializable))
            {
                try
                {
                    bytes = SerializationUtils.SerializeObject(obj);
                }
                catch (Exception e)
                {
                    throw new MessageConversionException("failed to convert to serialized Message content", e);
                }

                messageProperties.ContentType = MessageProperties.CONTENT_TYPE_SERIALIZED_OBJECT;
            }

            if (bytes != null)
            {
                messageProperties.ContentLength = bytes.Length;
            }

            return new Message(bytes, messageProperties);
        }
    }
}
