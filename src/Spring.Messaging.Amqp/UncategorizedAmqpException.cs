
using System;

namespace Spring.Messaging.Amqp
{
    /// <summary>
    /// A "catch-all" exception type within the AmqpException hierarchy when no more specific cause is known.
    /// </summary>
    public class UncategorizedAmqpException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UncategorizedAmqpException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="cause">
        /// The cause.
        /// </param>
        public UncategorizedAmqpException(string message, Exception cause) : base(message, cause)
        {
        }
    }
}
