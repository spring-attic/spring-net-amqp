
using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A connection listener.
    /// </summary>
    public interface IConnectionListener
    {
        /// <summary>
        /// OnCreate will be called then a connection is created.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        void OnCreate(IConnection connection);

        /// <summary>
        /// OnClose will be called when a connection is closed.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        void OnClose(IConnection connection);
    }
}
