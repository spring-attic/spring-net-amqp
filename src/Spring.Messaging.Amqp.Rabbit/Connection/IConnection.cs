
using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// An interface for connections.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public interface IConnection 
    {
        /// <summary>
        /// Create a new channel, using an internally allocated channel number.
        /// </summary>
        /// <param name="transactional">
        /// Transactional true if the channel should support transactions.
        /// </param>
        /// <returns>
        /// A new channel descriptor, or null if none is available.
        /// </returns>
        IModel CreateChannel(bool transactional);
        
        /// <summary>
        /// Close this connection and all its channels with the {@link com.rabbitmq.client.AMQP#REPLY_SUCCESS} close code and message 'OK'.
        /// Waits for all the close operations to complete.
        /// </summary>
        void Close();
    
        /// <summary>
        /// Flag to indicate the status of the connection.
        /// </summary>
        /// <returns>
        /// True if the connection is open
        /// </returns>
        bool IsOpen();
    }
}
