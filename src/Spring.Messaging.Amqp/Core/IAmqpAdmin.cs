
namespace Spring.Messaging.Amqp.Core
{

    /// <summary>
    /// Specifies a basic set of portable AMQP administrative operations for AMQP > 0.8
    /// </summary>
    /// <author>Mark Pollack</author>
    public interface IAmqpAdmin
    {
        #region Exchange Operations

        /// <summary>
        /// Declares the exchange.
        /// </summary>
        /// <param name="exchange">The exchange.</param>
        void DeclareExchange(IExchange exchange);

        /// <summary>
        /// Deletes the exchange.
        /// </summary>
        /// <remarks>
        /// Look at implementation specific subclass for implementation specific behavior, for example
        /// for RabbitMQ this will delete the exchange without regard for whether it is in use or not.
        /// </remarks>
        /// <param name="exchangeName">Name of the exchange.</param>
        /// <returns>true if the exchange existed and was deleted</returns>
        bool DeleteExchange(string exchangeName);

        #endregion

        #region Queue Operations

        /// <summary>
        /// Declares a queue whose name is automatically named.  It is created with
        /// exclusive = true, autoDelete=true, and durable = false.
        /// </summary>
        /// <returns>The queue.</returns>
        Queue DeclareQueue();

        /// <summary>
        /// Declares the given queue.
        /// </summary>
        /// <param name="queue">The queue to declare.</param>
        void DeclareQueue(Queue queue);

        /// <summary>
        /// Deletes the queue, without regard for whether it is in use or has messages on it 
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <returns>true if the queue existed and was deleted.</returns>
        bool DeleteQueue(string queueName);

        /// <summary>
        /// Deletes the queue.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="unused">if set to <c>true</c> the queue should be deleted only if not in use.</param>
        /// <param name="empty">if set to <c>true</c> the queue should be deleted only if empty.</param>
        void DeleteQueue(string queueName, bool unused, bool empty);

        /// <summary>
        /// Purges the queue.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="noWait">if set to <c>true</c> [no wait].</param>
        void PurgeQueue(string queueName, bool noWait);

        #endregion

        #region Binding operations

        /// <summary>
        /// Declare a binding of a queue to an exchange.
        /// </summary>
        /// <param name="binding">Binding to declare.</param>
        void DeclareBinding(Binding binding);

        /// <summary>
        /// Remove a binding of a queue to an exchange. Note unbindQueue/removeBinding was not introduced until 0.9 of the
        /// specification.
        /// </summary>
        /// <param name="binding">Binding to remove.</param>
        void RemoveBinding(Binding binding);

        #endregion
    }
}