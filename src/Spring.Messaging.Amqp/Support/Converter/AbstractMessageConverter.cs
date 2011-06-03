
using System;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Support.Converter
{
    /// <summary>
    /// Convenient base class for IMessageConverter implementations.
    /// </summary>
    /// <author>Joe Fitzgerald</author>
    public abstract class AbstractMessageConverter : IMessageConverter
    {
        private bool createMessageIds = false;

        /// <summary>
        /// Gets or sets a value indicating whether new messages should have unique identifiers added to their properties before sending. Default is false.
        /// </summary>
        public bool CreateMessageIds
        {
            get { return this.createMessageIds; }
            set { this.createMessageIds = value; }
        }

        /// <summary>
        /// Create a message from the object with properties.
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
        public virtual Message ToMessage(object obj, MessageProperties messageProperties)
        {
            if (messageProperties == null)
            {
                messageProperties = new MessageProperties();
            }

            Message message = this.CreateMessage(obj, messageProperties);
            messageProperties = message.MessageProperties;
            if (this.createMessageIds && messageProperties.MessageId == null)
            {
                messageProperties.MessageId = Guid.NewGuid().ToString();
            }

            return message;
        }

        /// <summary>
        /// Create an object from the message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The object.
        /// </returns>
        public abstract object FromMessage(Message message);

        /// <summary>
        /// Crate a message from the payload object and message properties provided. The message id will be added to the
        /// properties if necessary later.
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
        protected abstract Message CreateMessage(object obj, MessageProperties messageProperties);
    }
}
