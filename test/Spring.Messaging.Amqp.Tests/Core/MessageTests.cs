// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageTests.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Text;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Tests.Core
{
    /// <summary>
    /// Message Tests.
    /// </summary>
    /// <author>Mark Fisher</author>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class MessageTests
    {
        /// <summary>
        /// Toes the string for empty message body.
        /// </summary>
        [Test]
        public void ToStringForEmptyMessageBody()
        {
            var message = new Message(new byte[0], new MessageProperties());
            Assert.NotNull(message.ToString());
        }

        /// <summary>
        /// Toes the string for null message properties.
        /// </summary>
        [Test]
        public void ToStringForNullMessageProperties()
        {
            var message = new Message(new byte[0], null);
            Assert.NotNull(message.ToString());
        }

        /// <summary>
        /// Toes the string for non string message body.
        /// </summary>
        [Test]
        public void ToStringForNonStringMessageBody()
        {
            var message = new Message(SerializationUtils.SerializeObject(DateTime.UtcNow), null);
            Assert.NotNull(message.ToString());
        }

        /// <summary>
        /// Toes the string for serializable message body.
        /// </summary>
        [Test]
        public void ToStringForSerializableMessageBody()
        {
            var messageProperties = new MessageProperties();
            messageProperties.ContentType = MessageProperties.CONTENT_TYPE_SERIALIZED_OBJECT;
            var message = new Message(SerializationUtils.SerializeObject(DateTime.UtcNow), messageProperties);
            Assert.NotNull(message.ToString());
        }

        /// <summary>
        /// Toes the string for non serializable message body.
        /// </summary>
        [Test]
        public void ToStringForNonSerializableMessageBody()
        {
            var messageProperties = new MessageProperties();
            messageProperties.ContentType = MessageProperties.CONTENT_TYPE_SERIALIZED_OBJECT;
            var message = new Message(Encoding.UTF8.GetBytes("foo"), messageProperties);

            // System.err.println(message);
            Assert.NotNull(message.ToString());
        }
    }
}
