
using System;

namespace Spring.Messaging.Amqp
{
    /// <summary>
    /// An immediate acknowledge AMQP Exception.
    /// </summary>
    /// <remarks></remarks>
    public class ImmediateAcknowledgeAmqpException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <remarks></remarks>
        public ImmediateAcknowledgeAmqpException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateAcknowledgeAmqpException"/> class.
        /// </summary>
        /// <param name="cause">The cause.</param>
        /// <remarks></remarks>
        public ImmediateAcknowledgeAmqpException(Exception cause) : base(cause)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateAcknowledgeAmqpException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cause">The cause.</param>
        /// <remarks></remarks>
        public ImmediateAcknowledgeAmqpException(string message, Exception cause) : base(message, cause)
        {
        }
    }
}
