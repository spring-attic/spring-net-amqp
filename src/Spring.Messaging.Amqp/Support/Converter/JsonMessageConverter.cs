
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

using Common.Logging;

using Newtonsoft.Json;

using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Support.Converter
{
    /// <summary>
    /// A Json Message Converter.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class JsonMessageConverter : AbstractMessageConverter
    {
        /// <summary>
        /// The logger.
        /// </summary>
        public static ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The default charset.
        /// </summary>
        public static readonly string DEFAULT_CHARSET = "utf-8";

        /// <summary>
        /// The default charset.
        /// </summary>
        private volatile string defaultCharset = DEFAULT_CHARSET;

        /// <summary>
        /// The JSON Serializer
        /// </summary>
        private JsonSerializer jsonSerializer = new JsonSerializer();

        /// <summary>
        /// The ITypeMapper instance.
        /// </summary>
        private ITypeMapper typeMapper = new DefaultTypeMapper();

        public JsonMessageConverter()
        {
            this.InitializeJsonSerializer();
        }

        /// <summary>
        /// Sets the default charset.
        /// </summary>
        /// <value>The default charset.</value>
        public string DefaultCharset
        {
            set { this.defaultCharset = value; }
        }

        /// <summary>
        /// Sets TypeMapper.
        /// </summary>
        public ITypeMapper TypeMapper
        {
            set { this.typeMapper = value; }
        }

        /// <summary>
        /// Sets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        public JsonSerializer JsonSerializer
        {
            set { this.jsonSerializer = value; }
        }

        protected void InitializeJsonSerializer()
        {
            jsonSerializer.MissingMemberHandling = MissingMemberHandling.Ignore;
        }

        #region Implementation of IMessageConverter

        /// <summary>
        /// Convert from a Message to an object.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The object.
        /// </returns>
        /// <exception cref="MessageConversionException">
        /// </exception>
        public override object FromMessage(Message message)
        {
            object content = null;
            var properties = message.MessageProperties;

            if (properties != null)
            {
                var contentType = properties.ContentType;
                if (!string.IsNullOrEmpty(contentType) && contentType.Contains("json"))
                {
                    var encoding = properties.ContentEncoding ?? this.defaultCharset;

                    try
                    {
                        var targetType = this.typeMapper.ToType(message.MessageProperties);
                        content = this.ConvertBytesToObject(message.Body, encoding, targetType);
                    }
                    catch (Exception e)
                    {
                        throw new MessageConversionException("Failed to convert json-based Message content", e);
                    }
                }
            }

            return content ?? (content = message.Body);
        }

        /// <summary>
        /// Converts the bytes to object.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <returns>The requested object.</returns>
        private object ConvertBytesToObject(byte[] body, string encoding, Type targetType)
        {
            using (var ms = new MemoryStream(body))
            {
                var internalEncoding = Encoding.GetEncoding(encoding);

                using (TextReader reader = new StreamReader(ms, internalEncoding, false))
                {
                    using (var jsonTextReader = new JsonTextReader(reader))
                    {
                        var result = jsonSerializer.Deserialize(jsonTextReader, targetType);
                        return result;
                    }
                }
            }
	    }

        /// <summary>
        /// Overridden implementation of CreateMessage, to cater for Json serialization.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <param name="messageProperties">
        /// The message properties.
        /// </param>
        /// <returns>
        /// The Message.
        /// </returns>
        /// <exception cref="MessageConversionException">
        /// </exception>
        protected override Message CreateMessage(object obj, MessageProperties messageProperties)
        {
            byte[] bytes = null;
            try
            {
                var jsonString = string.Empty;
                var sb = new StringBuilder(128);
                var sw = new StringWriter(sb, CultureInfo.InvariantCulture);

                using (JsonWriter jsonWriter = new JsonTextWriter(sw))
                {
                    jsonSerializer.Serialize(jsonWriter, obj);
                    jsonString = sw.ToString();
                    var encoding = Encoding.GetEncoding(this.defaultCharset);
                    bytes = encoding.GetBytes(jsonString);
                }
            }
            catch (Exception e)
            {
                throw new MessageConversionException("Failed to convert Message content", e);
            }

            messageProperties.ContentType = ContentType.CONTENT_TYPE_JSON;
            messageProperties.ContentEncoding = this.defaultCharset;
            if (bytes != null)
            {
                messageProperties.ContentLength = bytes.Length;
            }

            this.typeMapper.FromType(obj.GetType(), messageProperties);
            return new Message(bytes, messageProperties);
        }

        #endregion
    }
}