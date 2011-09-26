
using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{


    /// <summary>
    /// A channel listener interface.
    /// </summary>
    public interface IChannelListener
    {
        /// <summary>
        /// Called when [create].
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="transactional">if set to <c>true</c> [transactional].</param>
        void OnCreate(IModel channel, bool transactional);
    }
}
