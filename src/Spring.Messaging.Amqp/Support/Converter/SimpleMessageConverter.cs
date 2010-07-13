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
using System.IO;
using System.Text;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Support.Converter
{
    /// <summary>
    /// Supports converting String and byte[] object types.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class SimpleMessageConverter : IMessageConverter
    {
        //TODO is this case sensitive?
        public static readonly string DEFAULT_CHARSET = "utf-8";

        private volatile string defaultCharset = DEFAULT_CHARSET;

        #region Implementation of IMessageConverter

        public Message ToMessage(object obj, IMessagePropertiesFactory messagePropertiesFactory)
        {
            byte[] bytes = null;
            IMessageProperties messageProperties = messagePropertiesFactory.Create();
            if (obj is string)
            {
                bytes = Encoding.GetEncoding(DEFAULT_CHARSET).GetBytes((string) obj);
                messageProperties.ContentType = ContentType.CONTENT_TYPE_TEXT_PLAIN;
                messageProperties.ContentEncoding = this.defaultCharset;

            } else if (obj is byte[])
            {
                bytes = (byte[]) obj;
                messageProperties.ContentType = ContentType.CONTENT_TYPE_BYTES;
            }
            if (bytes != null)
            {
                messageProperties.ContentLength = bytes.Length;
            }
            return new Message(bytes, messageProperties);

        }

        public object FromMessage(Message message)
        {
            object content = null;
            IMessageProperties properties = message.MessageProperties;
            if (properties != null)
            {
                string contentType = properties.ContentType;
                if (contentType != null && contentType.StartsWith("text"))
                {
                    string encoding = properties.ContentEncoding;
                    if (encoding == null)
                    {
                        encoding = this.defaultCharset;
                    }
                    try
                    {
                        content = ConvertToString(message.Body, encoding);
                    } catch (Exception e)
                    {
                        throw new MessageConversionException("Failed to convert text-based Message content", e);
                    }
                }
            }
            if (content == null)
            {
                content = message.Body;
            }
            return content;
            
        }

        #endregion


        private string ConvertToString(byte[] bytes, string encodingString)
        {
            MemoryStream ms = new MemoryStream(bytes);
            Encoding encoding = Encoding.GetEncoding(encodingString);
            //Last argument is to not autoDetectEncoding
            TextReader reader = new StreamReader(ms, encoding, false);
            string stringMessage = reader.ReadToEnd();
            return stringMessage;
        }
    }
}