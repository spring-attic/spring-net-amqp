
using System;

namespace Spring.Messaging.Amqp
{
    /// <summary>
    /// An AMQP Illegal State Exception.
    /// </summary>
    /// Equivalent of an IllegalStateException but within the AmqpException hierarchy.
    /// @author Mark Pollack
    /// <remarks></remarks>
    public class AmqpIllegalStateException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <remarks></remarks>
        public AmqpIllegalStateException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpIllegalStateException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cause">The cause.</param>
        /// <remarks></remarks>
        public AmqpIllegalStateException(string message, Exception cause) : base(message, cause)
        {
        }
    }
}
