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

#region

using System;
using System.Collections.Generic;
using System.Text;
using Apache.Qpid.Messaging;
using Spring.Messaging.Amqp.Core;

#endregion

namespace Spring.Messaging.Amqp.Qpid.Core
{
    /// <summary>
    ///  
    /// </summary>
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

        private ulong deliveryTag;

        private uint messageCount;

        private long contentLength;

        #region the following properties are on the QPID IMessage class

        private string appId;

        private string clusterId;
       
        private string contentEncoding;
        private string contentType;
        private string correlationId;
        private byte[] correlationIdAsBytes;
        private Apache.Qpid.Messaging.DeliveryMode deliveryMode;
        private long expiration;
        //private IHeaders headers;
        private HeadersDictionary headersDictionary;
        private string messageId;
        private byte priority;
        private Boolean redelivered;
        private string replyToExchangeName;
        private string replyToRoutingKey;
        private long timestamp;
        private string messageType;//type;
        private string userId;

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public MessageProperties()
        {            
        }

        #region Implementation of IMessageProperties

        public ulong DeliveryTag
        {
            get { return this.deliveryTag; }
        }

        public string Expiration
        {
            //TODO in 0-8 the definition was ambiguous and format not clearly defined.  This has probably since been corrected.
            //https://dev.rabbitmq.com/wiki/FrequentlyAskedQuestions
            get { return this.expiration.ToString(); }
            set { expiration = long.Parse(value); }
        }

        public IDictionary<string, object> Headers
        {
            get
            {
                return headersDictionary;
            }
            set
            {
                
            }
        }

        public uint MessageCount
        {
            get { return messageCount; }
            set { messageCount = value; }
        }

        public string MessageId
        {
            //TODO MessageId is stored in qpid 1.0 Message class and is an int.
            get { return messageId; }
            set { messageId = value; }
        }

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public int Priority
        {
            get
            {
                return priority;
            }
            set
            {
                priority = (byte) value;                
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
            //TODO - come up with convention for this.
            get { return this.replyToRoutingKey; }
            set
            {
                this.replyToRoutingKey = value;
            }
        }

        public long Timestamp
        {
            get { return this.timestamp; }
            set { this.timestamp = value; }
        }

        public string Type
        {
            get { return messageType; }
            set { this.messageType = value; }
        }

        public string UserId
        {
            get { return this.userId; }
            set { this.userId = value; }
        }



        public string AppId
        {
            get { return this.appId; }
            set { this.appId = value; }
        }

        public string ClusterId
        {
            //TODO where is this property?  was it removed from the 1.0 spec?
            get { return this.clusterId; }
            set { this.clusterId = value; }
        }

        public string ContentEncoding
        {
            get { return this.contentEncoding; }
            set { this.contentEncoding = value; }
        }

        public long ContentLength
        {
            get { return contentLength; }
            set { this.contentLength = value; }
        }

        public string ContentType
        {
            get { return this.contentType; }
            set { this.contentType = value; }
        }

        public byte[] CorrelationId
        {
            get { return this.correlationIdAsBytes; }
            set { this.correlationIdAsBytes = value; }
        }

        public Spring.Messaging.Amqp.Core.MessageDeliveryMode DeliveryMode
        {
            get
            {
                
                  int idm = (int) this.deliveryMode;
                    return
                        (Spring.Messaging.Amqp.Core.MessageDeliveryMode)
                        Enum.ToObject(typeof (Spring.Messaging.Amqp.Core.MessageDeliveryMode), idm);
                
            }

            set
            {
                if (value != Spring.Messaging.Amqp.Core.MessageDeliveryMode.None)
                {
                    switch (value)
                    {
                        case Spring.Messaging.Amqp.Core.MessageDeliveryMode.NON_PERSISTENT:
                            this.deliveryMode = Apache.Qpid.Messaging.DeliveryMode.NonPersistent;                            
                            break;
                        case Spring.Messaging.Amqp.Core.MessageDeliveryMode.PERSISTENT:
                            this.deliveryMode = Apache.Qpid.Messaging.DeliveryMode.Persistent;       
                            break;
                    }
                }
            }
        }
        #endregion

    }
}