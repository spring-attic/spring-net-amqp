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