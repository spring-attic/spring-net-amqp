
using System;

namespace Spring.Messaging.Amqp
{
    /// <summary>
    /// Base SystemException for errors that occur when executing AMQP operations.
    /// </summary>
    public class AmqpException : SystemException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public AmqpException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpException"/> class.
        /// </summary>
        /// <param name="cause">
        /// The cause.
        /// </param>
        public AmqpException(Exception cause) : this(string.Empty, cause)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="cause">
        /// The cause.
        /// </param>
        public AmqpException(string message, Exception cause) : base(message, cause)
        {
        }

    }
}
