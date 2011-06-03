
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Rabbit.Retry
{
    /// <summary>
    /// A message key generator interface.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public interface IMessageKeyGenerator
    {
        /// <summary>
        /// Generate a unique key for the message that is repeatable on redelivery. Implementations should be very careful
        /// about assuming uniqueness of any element of the message, especially considering the requirement that it be
        /// repeatable. A message id is ideal, but may not be present (AMQP does not mandate it), and the message body is a
        /// byte array whose contents might be repeatable, but its object value is not.
        /// </summary>
        /// <param name="message">
        /// The message to generate a key for.
        /// </param>
        /// <returns>
        /// A unique key for this message.
        /// </returns>
        object GetKey(Message message);
    }
}
