
using System;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    /// Exception to be thrown when the execution of a listener method failed unrecoverably.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public class FatalListenerExecutionException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FatalListenerExecutionException"/> class.
        /// </summary>
        /// <param name="msg">
        /// The msg.
        /// </param>
        /// <param name="cause">
        /// The cause.
        /// </param>
        public FatalListenerExecutionException(string msg, Exception cause) : base(msg, cause)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FatalListenerExecutionException"/> class.
        /// </summary>
        /// <param name="msg">
        /// The msg.
        /// </param>
        public FatalListenerExecutionException(string msg) : base(msg)
        {
        }
    }
}
