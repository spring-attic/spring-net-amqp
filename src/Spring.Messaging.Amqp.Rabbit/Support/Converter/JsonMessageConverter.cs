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
using System.Collections;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core;

namespace Spring.Messaging.Amqp.Rabbit.Support.Converter
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class JsonMessageConverter : IMessageConverter
    {
        public static readonly string DEFAULT_CHARSET = "utf-8";

        private volatile string defaultCharset = DEFAULT_CHARSET;

        private ITypeMapper typeMapper;

        public ITypeMapper TypeMapper
        {
            set { typeMapper = value; }
        }

        #region Implementation of IMessageConverter

        public Message ToMessage(object obj, IModel channel)
        {
            byte[] bytes = null;
            IMessageProperties messageProperties = new MessageProperties(channel.CreateBasicProperties());

            //Just a one liner for now.
            string jsonString = JsonConvert.SerializeObject(obj);
            
            
            bytes = Encoding.GetEncoding(DEFAULT_CHARSET).GetBytes(jsonString);
            messageProperties.ContentType = MessageProperties.CONTENT_TYPE_JSON;
            messageProperties.ContentEncoding = this.defaultCharset;
            messageProperties.ContentLength = bytes.Length;            
            messageProperties.Headers[typeMapper.TypeIdFieldName] = typeMapper.FromType(obj.GetType());
            return new Message(bytes, messageProperties);

        }

        public object FromMessage(Message message)
        {
            object content = null;
            IMessageProperties properties = message.MessageProperties;
            if (properties != null)
            {
                string contentType = properties.ContentType;
                if (contentType != null && contentType.Contains("json"))
                {
                    string encoding = properties.ContentEncoding;
                    if (encoding == null)
                    {
                        encoding = this.defaultCharset;
                    }
                    try
                    {
                        object typeIdFieldNameValue = message.MessageProperties.Headers[typeMapper.TypeIdFieldName];
                        string typeId = null;
                        //TODO this is a string when the message has not yet been marshalled across the wire.
                        if (typeIdFieldNameValue is string)
                        {
                            typeId = (string) typeIdFieldNameValue;
                        }
                        if (typeIdFieldNameValue is byte[])
                        {
                            typeId = ConvertBytesToString((byte[])typeIdFieldNameValue, encoding);
                        }
                        if (typeId == null)
                        {
                            throw new MessageConversionException("Failed to convert json-based Message content. TypeIdFieldName not found in headers."); 
                        }
                            //string stringType = (string) message.MessageProperties.Headers[typeMapper.TypeIdFieldName];

                        Type targetType = typeMapper.ToType(typeId);
                        content = ConvertBytesToObject(message.Body, encoding, targetType);
                    } catch (Exception e)
                    {
                        throw new MessageConversionException("Failed to convert json-based Message content", e);
                    }
                }
            }
            if (content == null )
            {
                content = message.Body;
            }
            return content;
        }


        #endregion

        private string ConvertBytesToString(byte[] bytes, string encodingString)
        {
            MemoryStream ms = new MemoryStream(bytes);
            Encoding encoding = Encoding.GetEncoding(encodingString);
            //Last argument is to not autoDetectEncoding
            TextReader reader = new StreamReader(ms, encoding, false);
            string stringMessage = reader.ReadToEnd();
            return stringMessage;
        }

        private object ConvertBytesToObject(byte[] bytes, string encodingString, Type targetType)
        {
            MemoryStream ms = new MemoryStream(bytes);
            Encoding encoding = Encoding.GetEncoding(encodingString);
            //Last argument is to not autoDetectEncoding
            TextReader reader = new StreamReader(ms, encoding, false);
            //string jsonString = reader.ReadToEnd();
            Newtonsoft.Json.JsonTextReader jsonTextReader = new JsonTextReader(reader);
            JsonSerializer jsonSerializer = new JsonSerializer();
            object result = jsonSerializer.Deserialize(jsonTextReader, targetType);
            return result;
        }
    }

}