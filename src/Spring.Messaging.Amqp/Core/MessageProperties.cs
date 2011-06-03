
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

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Message Properties for an AMQP message.
    /// </summary>
    /// <author>Joe Fitzgerald</author>
    public class MessageProperties
    {
        public static readonly string CONTENT_TYPE_BYTES = @"application/octet-stream";

        public static readonly string CONTENT_TYPE_TEXT_PLAIN = @"text/plain";

        public static readonly string CONTENT_TYPE_SERIALIZED_OBJECT = @"application/x-java-serialized-object";

        public static readonly string CONTENT_TYPE_JSON = @"application/json";

        private static readonly string DEFAULT_CONTENT_TYPE = CONTENT_TYPE_BYTES;

        private static readonly MessageDeliveryMode DEFAULT_DELIVERY_MODE = MessageDeliveryMode.PERSISTENT;

        private static readonly int DEFAULT_PRIORITY = 0;

        private readonly IDictionary headers = new Hashtable();

        private DateTime timestamp;

        private string messageId;

        private string userId;

        private string appId;

        private string clusterId;

        private string type;

        private byte[] correlationId;

        private Address replyTo;

        private string contentType = DEFAULT_CONTENT_TYPE;

        private string contentEncoding;

        private long contentLength;

        private MessageDeliveryMode deliveryMode = DEFAULT_DELIVERY_MODE;

        private string expiration;

        private int priority = DEFAULT_PRIORITY;

        private bool redelivered;

        private string receivedExchange;

        private string receivedRoutingKey;

        private long deliveryTag;

        private int messageCount;

        /// <summary>
        /// Gets Headers.
        /// </summary>
        public IDictionary Headers
        {
            get { return this.headers; }
        }

        /// <summary>
        /// Gets or sets Timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get { return this.timestamp; }
            set { this.timestamp = value; }
        }

        /// <summary>
        /// Gets or sets MessageId.
        /// </summary>
        public string MessageId
        {
            get { return this.messageId; }
            set { this.messageId = value; }
        }

        /// <summary>
        /// Gets or sets UserId.
        /// </summary>
        public string UserId
        {
            get { return this.userId; }
            set { this.userId = value; }
        }

        /// <summary>
        /// Gets or sets AppId.
        /// </summary>
        public string AppId
        {
            get { return this.appId; }
            set { this.appId = value; }
        }

        /// <summary>
        /// Gets or sets ClusterId.
        /// </summary>
        public string ClusterId
        {
            get { return this.clusterId; }
            set { this.clusterId = value; }
        }

        /// <summary>
        /// Gets or sets Type.
        /// </summary>
        public string Type
        {
            get { return this.type; }
            set { this.type = value; }
        }

        /// <summary>
        /// Gets or sets CorrelationId.
        /// </summary>
        public byte[] CorrelationId
        {
            get { return this.correlationId; }
            set { this.correlationId = value; }
        }

        /// <summary>
        /// Gets or sets ReplyTo.
        /// </summary>
        public Address ReplyTo
        {
            get { return this.replyTo; }
            set { this.replyTo = value; }
        }

        /// <summary>
        /// Gets or sets ContentType.
        /// </summary>
        public string ContentType
        {
            get { return this.contentType; }
            set { this.contentType = value; }
        }

        /// <summary>
        /// Gets or sets ContentEncoding.
        /// </summary>
        public string ContentEncoding
        {
            get { return this.contentEncoding; }
            set { this.contentEncoding = value; }
        }

        /// <summary>
        /// Gets or sets ContentLength.
        /// </summary>
        public long ContentLength
        {
            get { return this.contentLength; }
            set { this.contentLength = value; }
        }

        /// <summary>
        /// Gets or sets DeliveryMode.
        /// </summary>
        public MessageDeliveryMode DeliveryMode
        {
            get { return this.deliveryMode; }
            set { this.deliveryMode = value; }
        }

        /// <summary>
        /// Gets or sets Expiration.
        /// </summary>
        public string Expiration
        {
            get { return this.expiration; }
            set { this.expiration = value; }
        }

        /// <summary>
        /// Gets or sets Priority.
        /// </summary>
        public int Priority
        {
            get { return this.priority; }
            set { this.priority = value; }
        }

        /// <summary>
        /// Gets or sets ReceivedExchange.
        /// </summary>
        public string ReceivedExchange
        {
            get { return this.receivedExchange; }
            set { this.receivedExchange = value; }
        }

        /// <summary>
        /// Gets or sets ReceivedRoutingKey.
        /// </summary>
        public string ReceivedRoutingKey
        {
            get { return this.receivedRoutingKey; }
            set { this.receivedRoutingKey = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Redelivered.
        /// </summary>
        public bool Redelivered
        {
            get { return this.redelivered; }
            set { this.redelivered = value; }
        }

        /// <summary>
        /// Gets or sets DeliveryTag.
        /// </summary>
        public long DeliveryTag
        {
            get { return this.deliveryTag; }
            set { this.deliveryTag = value; }
        }

        /// <summary>
        /// Gets or sets MessageCount.
        /// </summary>
        public int MessageCount
        {
            get { return this.messageCount; }
            set { this.messageCount = value; }
        }
    }
}