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

using Spring.Messaging.Amqp.Core;

#endregion

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class Message : IMessage
    {
        private readonly IMessageProperties messageProperties;

        private readonly byte[] body;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public Message(byte[] body, IMessageProperties messageProperties)
        {
            this.body = body;
            this.messageProperties = messageProperties;
        }

        /*
        public IBasicProperties BasicProperties
        {
            get { return CreateBasicProperties(); }
        }

        private IBasicProperties CreateBasicProperties()
        {
            BasicProperties bp = new BasicProperties();
            if (messageProperties.AppId != null)
            {
                bp.AppId = messageProperties.AppId;
            }
            if (messageProperties.ContentEncoding != null)
            {
                bp.ContentEncoding = messageProperties.ContentEncoding;
            }
            if (messageProperties.ContentType != null)
            {
                bp.ContentType = messageProperties.ContentType;
            }
            if (messageProperties.CorrelationId != null)
            {
                bp.CorrelationId = messageProperties.CorrelationId;
            }
            if (messageProperties.DeliveryMode != null)
            {
                bp.DeliveryMode = messageProperties.DeliveryMode.GetValueOrDefault();
            }
            if (messageProperties.Expiration != null)
            {
                bp.Expiration = messageProperties.Expiration;
            }
            if (messageProperties.Headers != null)
            {
                bp.Headers = messageProperties.Headers;
            }
            if (messageProperties.Id != null)
            {
                bp.MessageId = messageProperties.Id;
            }
            if (messageProperties.Priority != null)
            {
                bp.Priority = messageProperties.Priority.GetValueOrDefault();
            }
            if (messageProperties.ReplyTo != null)
            {
                bp.ReplyTo = messageProperties.ReplyTo;
            }

            if (messageProperties.UserId != null)
            {
                bp.UserId = messageProperties.UserId;
            }

            //TODO - copy in ClusterId and Type properties

            
            //StringBuilder sb = new StringBuilder();
            //bp.AppendPropertyDebugStringTo(sb);
            //Console.WriteLine(sb.ToString());
            return bp;
        }
        */

        #region Implementation of IMessage

        public byte[] Body
        {
            get { return body; }
        }

        public IMessageProperties MessageProperties
        {
            get { return messageProperties; }           
        }

        #endregion
    }
}