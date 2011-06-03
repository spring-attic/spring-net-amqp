
using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A channel proxy interface.
    /// </summary>
    public interface IChannelProxy : IModel 
    {
        /// <summary>
        /// Return the target Channel (Model) of this proxy. This will typically be the native provider Channel (Model).
        /// </summary>
        /// <returns>
        /// The channel.
        /// </returns>
        IModel GetTargetChannel();
    }
}
