
using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// A channel callback delegate.
    /// </summary>
    /// <typeparam name="T">Type T</typeparam>
    /// <param name="channel">The channel.</param>
    /// <returns>Object of Type T.</returns>
    /// <remarks></remarks>
    public delegate T ChannelCallbackDelegate<T>(IModel channel);
}