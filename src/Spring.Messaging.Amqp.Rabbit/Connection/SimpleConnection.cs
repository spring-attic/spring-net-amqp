
using System;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A Simple Connection implementation.
    /// </summary>
    public class SimpleConnection : IConnection
    {
        /// <summary>
        /// The connection delegate.
        /// </summary>
        private readonly RabbitMQ.Client.IConnection connectionDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleConnection"/> class.
        /// </summary>
        /// <param name="connectionDelegate">
        /// The connection delegate.
        /// </param>
        public SimpleConnection(RabbitMQ.Client.IConnection connectionDelegate)
        {
            this.connectionDelegate = connectionDelegate;
        }

        /// <summary>
        /// Create a channel, given a flag indicating whether it should be transactional or not.
        /// </summary>
        /// <param name="transactional">
        /// The transactional.
        /// </param>
        /// <returns>
        /// The channel.
        /// </returns>
        public RabbitMQ.Client.IModel CreateChannel(bool transactional)
        {
            try
            {
                var channel = this.connectionDelegate.CreateModel();
                if (transactional)
                {
                    // Just created so we want to start the transaction
                    channel.TxSelect();
                }
                return channel;
            }
            catch (Exception e)
            {
                throw RabbitUtils.ConvertRabbitAccessException(e);
            }
        }

        /// <summary>
        /// Close the channel.
        /// </summary>
        public void Close()
        {
            try
            {
                this.connectionDelegate.Close();
            }
            catch (Exception e)
            {
                throw RabbitUtils.ConvertRabbitAccessException(e);
            }
        }

        /// <summary>
        /// Determine if the channel is open.
        /// </summary>
        /// <returns>
        /// True if open, else false.
        /// </returns>
        public bool IsOpen()
        {
            return this.connectionDelegate != null && this.connectionDelegate.IsOpen;
        }
    }
}
