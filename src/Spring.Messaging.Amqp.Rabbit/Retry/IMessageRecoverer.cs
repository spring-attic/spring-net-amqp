
using System;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Rabbit.Retry
{
    /// <summary>
    /// A message recoverer interface.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public interface IMessageRecoverer
    {
        /// <summary>
        /// Callback for message that was consumed but failed all retry attempts.
        /// </summary>
        /// <param name="message">
        /// The message to recover.
        /// </param>
        /// <param name="cause">
        /// The cause of the error.
        /// </param>
        void Recover(Message message, Exception cause);
    }
}
