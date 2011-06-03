
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Rabbit.Retry
{
    /// <summary>
    /// An optimization for stateful retry of message processing. If a message is known to be "new", i.e. never consumed
    /// before by this or any other client, then there are potential optimizations for managing the state associated with
    /// tracking the processing of a message (e.g. there is no need to check a cache for a hit).
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public interface INewMessageIdentifier
    {
        /// <summary>
        /// Query a message to see if it has been seen before. Usually it is only possible to know if it has definitely not
        /// been seen before (e.g. through the redelivered flag, which would be used by default). Clients can customize the</summary>
        /// retry behaviour for failed messages by implementing this method.
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// True if the message is known to not have been consumed before.
        /// </returns>
        bool IsNew(Message message);
    }
}
