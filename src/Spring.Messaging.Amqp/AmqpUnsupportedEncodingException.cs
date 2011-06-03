
using System;

namespace Spring.Messaging.Amqp
{
    /// <summary>
    /// SystemException for unsupported encoding in an AMQP operation. 
    /// </summary>
    public class AmqpUnsupportedEncodingException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpUnsupportedEncodingException"/> class.
        /// </summary>
        /// <param name="cause">
        /// The cause.
        /// </param>
        public AmqpUnsupportedEncodingException(Exception cause) : base(cause)
        {
        }
    }
}
