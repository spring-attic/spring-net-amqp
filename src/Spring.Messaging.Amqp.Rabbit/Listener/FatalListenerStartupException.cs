
using System;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    /// Exception to be thrown when the execution of a listener method failed on startup.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public class FatalListenerStartupException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FatalListenerStartupException"/> class.
        /// </summary>
        /// <param name="msg">
        /// The msg.
        /// </param>
        /// <param name="cause">
        /// The cause.
        /// </param>
        public FatalListenerStartupException(string msg, Exception cause) : base(msg, cause)
        {
        }
    }
}
