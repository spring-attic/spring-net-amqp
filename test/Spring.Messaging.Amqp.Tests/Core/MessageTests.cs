using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Tests.Core
{
    /// <summary>
    /// Message Tests.
    /// </summary>
    /// <remarks></remarks>
    public class MessageTests
    {
        /// <summary>
        /// Toes the string for empty message body.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void ToStringForEmptyMessageBody()
        {
            var message = new Message(new byte[0], new MessageProperties());
            Assert.NotNull(message.ToString());
        }

        /// <summary>
        /// Toes the string for null message properties.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void ToStringForNullMessageProperties()
        {
            var message = new Message(new byte[0], null);
            Assert.NotNull(message.ToString());
        }

        /// <summary>
        /// Toes the string for non string message body.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void ToStringForNonStringMessageBody()
        {
            var message = new Message(SerializationUtils.SerializeObject(DateTime.Now), null);
            Assert.NotNull(message.ToString());
        }

        /// <summary>
        /// Toes the string for serializable message body.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void ToStringForSerializableMessageBody()
        {
            var messageProperties = new MessageProperties();
            messageProperties.ContentType = MessageProperties.CONTENT_TYPE_SERIALIZED_OBJECT;
            var message = new Message(SerializationUtils.SerializeObject(DateTime.Now), messageProperties);
            Assert.NotNull(message.ToString());
        }

        /// <summary>
        /// Toes the string for non serializable message body.
        /// </summary>
        /// <remarks></remarks>
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
