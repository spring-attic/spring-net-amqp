
using System.IO;

namespace Spring.Messaging.Amqp
{
    /**
 * RuntimeException wrapper for an {@link IOException} which
 * can be commonly thrown from AMQP operations.
 * 
 * @author Mark Pollack
 */

    /// <summary>
    /// SystemException wrapper for an {@link IOException} which can be commonly thrown from AMQP operations.
    /// </summary>
    public class AmqpIOException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpIOException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="cause">
        /// The cause.
        /// </param>
        public AmqpIOException(string message, IOException cause) : base(message, cause)
        {
        }

    }
}
