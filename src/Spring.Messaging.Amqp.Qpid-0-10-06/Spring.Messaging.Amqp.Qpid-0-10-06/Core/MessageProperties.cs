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
using org.apache.qpid.transport;
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

        private Boolean redelivered;

        private ulong deliveryTag;

        private uint messageCount;

        private int messageId;

        private org.apache.qpid.transport.DeliveryProperties deliveryProperites;

        private org.apache.qpid.transport.MessageProperties messagePropreties;


        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public MessageProperties()
        {
            //TODO resumeId, resumeTTl
            deliveryProperites = new org.apache.qpid.transport.DeliveryProperties();
            messagePropreties = new org.apache.qpid.transport.MessageProperties();
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
            get { return deliveryProperites.GetExpiration().ToString(); }
            set { deliveryProperites.SetExpiration(long.Parse(value)); }
        }

        public IDictionary<string, object> Headers
        {
            get
            {
                return messagePropreties.GetApplicationHeaders();
            }
            set
            {                
                messagePropreties.SetApplicationHeaders(new Dictionary<string, object>(value));
            }
        }


        public uint MessageCount
        {
            get { return messageCount; }
            set { messageCount = value; }
        }

        public string MessageId
        {
            //TODO MessageId is stored in qpid Message class and is an int.
            get { return messageId.ToString(); }
            set { messageId = int.Parse(value); }
        }

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public int Priority
        {
            get
            {
                return (int) deliveryProperites.GetPriority();                
            }
            set
            {
                deliveryProperites.SetPriority((MessageDeliveryPriority) Enum.ToObject(typeof (MessageDeliveryPriority), value));
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
            get { return messagePropreties.GetReplyTo().GetRoutingKey(); }
            set
            {
                org.apache.qpid.transport.ReplyTo replyTo = new org.apache.qpid.transport.ReplyTo();
                replyTo.SetRoutingKey(value);
                messagePropreties.SetReplyTo(replyTo);
            }
        }

        public long Timestamp
        {
            get { return deliveryProperites.GetTimestamp(); }
            set { deliveryProperites.SetTimestamp(value); }
        }

        public string Type
        {
            get { return "not implemented"; }
            set { throw new NotImplementedException(); }
        }

        public string UserId
        {
            get { return Encoding.UTF8.GetString(messagePropreties.GetUserId()); }
            set { messagePropreties.SetUserId(Encoding.UTF8.GetBytes(value)); }
        }



        public string AppId
        {
            get { return Encoding.UTF8.GetString(messagePropreties.GetAppId()); }
            set { messagePropreties.SetAppId(Encoding.UTF8.GetBytes(value)); }
        }

        public string ClusterId
        {
            //TODO where is this property?  was it removed from the 1.0 spec?
            get { return "not implemented"; }
            set { throw new NotImplementedException(); }
        }

        public string ContentEncoding
        {
            get { return messagePropreties.GetContentEncoding(); }
            set { messagePropreties.SetContentEncoding(value); }
        }

        public long ContentLength
        {
            get { return messagePropreties.GetContentLength(); }
            set { messagePropreties.SetContentLength(value); }
        }

        public string ContentType
        {
            get { return messagePropreties.GetContentType(); }
            set { messagePropreties.SetContentType(value); }
        }

        public byte[] CorrelationId
        {
            get { return messagePropreties.GetCorrelationId(); }
            set { messagePropreties.SetCorrelationId(value); }
        }

        public Spring.Messaging.Amqp.Core.MessageDeliveryMode DeliveryMode
        {
            get
            {
                if (deliveryProperites.HasDeliveryMode())
                {
                    int idm = (int) deliveryProperites.GetDeliveryMode();
                    return
                        (Spring.Messaging.Amqp.Core.MessageDeliveryMode)
                        Enum.ToObject(typeof (Spring.Messaging.Amqp.Core.MessageDeliveryMode), idm);
                }
                return Spring.Messaging.Amqp.Core.MessageDeliveryMode.None;
            }

            set
            {
                if (value != Spring.Messaging.Amqp.Core.MessageDeliveryMode.None)
                {
                    switch (value)
                    {
                        case Spring.Messaging.Amqp.Core.MessageDeliveryMode.NON_PERSISTENT:
                            deliveryProperites.SetDeliveryMode(
                                org.apache.qpid.transport.MessageDeliveryMode.NON_PERSISTENT);
                            break;
                        case Spring.Messaging.Amqp.Core.MessageDeliveryMode.PERSISTENT:
                            deliveryProperites.SetDeliveryMode(
                                org.apache.qpid.transport.MessageDeliveryMode.PERSISTENT);
                            break;
                    }
                }
            }
        }
        #endregion

        public DeliveryProperties DeliveryProperites
        {
            get { return deliveryProperites; }
        }

        public org.apache.qpid.transport.MessageProperties MessagePropreties
        {
            get { return messagePropreties; }
        }
    }
}