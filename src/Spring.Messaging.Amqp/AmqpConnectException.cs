using System;

namespace Spring.Messaging.Amqp
{
    /**
  * RuntimeException wrapper for an {@link ConnectException} which can be commonly thrown from AMQP operations if the
  * remote process dies or there is a network issue.
  * 
  * @author Dave Syer
  */

    /// <summary>
    /// SystemException wrapper for an {@link ConnectException} which can be commonly thrown from AMQP operations if the remote process dies or there is a network issue.
    /// </summary>
    public class AmqpConnectException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpConnectException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="cause">
        /// The cause.
        /// </param>
        public AmqpConnectException(string message, Exception cause) : base(message, cause)
        {
        }
    }
}
