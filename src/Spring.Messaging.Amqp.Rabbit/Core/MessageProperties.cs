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

#region Using Statements

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;

#endregion

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// Rabbit implementation that stores much of the message property information in Rabbit's 
    /// <see cref="IBasicProperties"/> class.  Also contains static strings for various content types.
    /// </summary>
    /// <note>Headers will be created on demand if they are null in the underlying IBasicProperties instance.</note>
    /// <author>Mark Pollack</author>
    public class MessageProperties : IMessageProperties
    {
        private static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;

        public static readonly string CONTENT_TYPE_BYTES = "application/octet-stream";
	    public static readonly string CONTENT_TYPE_TEXT_PLAIN = "text/plain";
	    public static readonly string CONTENT_TYPE_SERIALIZED_OBJECT = "application/x-dotnet-serialized-object";
        public static readonly string CONTENT_TYPE_JSON = "appication/json";


        private volatile Encoding defaultEncoding = DEFAULT_ENCODING;

        private string receivedExchange;

        private string receivedRoutingKey;

        private Boolean redelivered;

        private ulong deliveryTag;

        private uint messageCount;

        private IBasicProperties basicProperties;


        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public MessageProperties(IBasicProperties basicProperties)
        {
            this.basicProperties = basicProperties;
            InitializeHeadersIfNecessary(basicProperties);
        }

        public MessageProperties(IBasicProperties basicProperties,
                                 string receivedExchange,
                                 string receivedRoutingKey,
                                 bool redelivered,
                                 ulong deliveryTag,
                                 uint messageCount)
        {
            this.receivedExchange = receivedExchange;
            this.receivedRoutingKey = receivedRoutingKey;
            this.redelivered = redelivered;
            this.deliveryTag = deliveryTag;
            this.messageCount = messageCount;
            this.basicProperties = basicProperties;
            InitializeHeadersIfNecessary(basicProperties);
        }

        public IBasicProperties BasicProperties
        {
            get { return basicProperties; }
        }


        private void InitializeHeadersIfNecessary(IBasicProperties basicProperties)
        {
            if (basicProperties.Headers == null)
            {
                basicProperties.Headers = new Dictionary<string, object>();
            }
        }

        #region Implementation of IMessageProperties

        public string AppId
        {
            get { return basicProperties.AppId; }
            set { basicProperties.AppId = value; }
        }

        public string ClusterId
        {
            get { return basicProperties.ClusterId; }
            set { basicProperties.ClusterId = value; }
        }

        public string ContentEncoding
        {
            get { return basicProperties.ContentEncoding; }
            set { basicProperties.ContentEncoding = value; }
        }

        public long ContentLength
        {
            //TODO what to do about content length?
            get { return 0; }
            set { }
        }

        public string ContentType
        {
            get { return basicProperties.ContentType; }
            set { basicProperties.ContentType = value; }
        }

        public byte[] CorrelationId
        {
            get
            {
                string sId = basicProperties.CorrelationId;
                return Encoding.UTF8.GetBytes(sId);
            }
            set
            {                
                basicProperties.CorrelationId = Encoding.UTF8.GetString(value);
            }
        }

        public MessageDeliveryMode DeliveryMode
        {
            get
            {
                if (basicProperties.IsDeliveryModePresent())
                {
                    return (MessageDeliveryMode) Enum.ToObject(typeof (MessageDeliveryMode), basicProperties.DeliveryMode);                    
                }
                return MessageDeliveryMode.None;
            }
            set
            {
                if (value != MessageDeliveryMode.None)
                {
                    switch (value)
                    {
                        case MessageDeliveryMode.NON_PERSISTENT:
                            basicProperties.DeliveryMode = 1;
                            break;
                        case MessageDeliveryMode.PERSISTENT:
                            basicProperties.DeliveryMode = 2;
                            break;
                    }
                }
            }
        }

        public ulong DeliveryTag
        {
            get { return this.deliveryTag; }
        }

        public string Expiration
        {
            get { return basicProperties.Expiration; }
            set { basicProperties.Expiration = value; }
        }

        public IDictionary<string,object> Headers
        {
            get
            {
               
                Dictionary<string, object> dictionary = basicProperties.Headers as Dictionary<string, object>;
                //this is not efficient of course...
                //Might need to fallbcak to just direct access of IDictionary but then qpid impl would suffer...
                if (dictionary == null)
                {
                    dictionary = new Dictionary<string, object>();
                    foreach (DictionaryEntry dictionaryEntry in basicProperties.Headers)
                    {
                        dictionary.Add(dictionaryEntry.Key.ToString(), dictionaryEntry.Value);                            
                    }
                }
                return dictionary;
            }
            set
            {
                //TODO convert IDictionary<string,objecxt> to Dictionary explicitly if cast fails
                IDictionary dict = new Hashtable();
                foreach (KeyValuePair<string, object> o in value)
                {
                    dict.Add(o.Key, o.Value);
                }
            }
        }

        public uint MessageCount
        {
            get { return messageCount; }
            set { messageCount = value; }
        }

        public string MessageId
        {
            get { return basicProperties.MessageId; }
            set { basicProperties.MessageId = value; }
        }

        public int Priority
        {
            get
            {
                return basicProperties.Priority;                
            }
            set
            {
                basicProperties.Priority = (byte)value;               
            }
        }

        public string ReceivedExchange
        {
            get { return receivedExchange; }
            set { receivedExchange = value; }
        }

        public string ReceivedRoutingKey
        {
            get { return receivedRoutingKey; }
            set { receivedRoutingKey = value; }
        }

        public bool Redelivered
        {
            get { return redelivered; }
            set { redelivered = value; }
        }

        public string ReplyTo
        {
            get { return basicProperties.ReplyTo; }
            set { basicProperties.ReplyTo = value; }
        }

        public string UserId
        {
            get { return basicProperties.UserId; }
            set { basicProperties.UserId = value; }
        }


        public long Timestamp
        {
            get { return basicProperties.Timestamp.UnixTime; }
            set { basicProperties.Timestamp = new AmqpTimestamp(value); }
        }

        public string Type
        {
            get { return basicProperties.Type; }
            set { basicProperties.Type = value; }
        }

        #endregion
    }
}