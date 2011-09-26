
using RabbitMQ.Client;
using RabbitMQ.Client.Impl;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// Strategy interface for converting between Spring AMQP {@link MessageProperties}
    /// and RabbitMQ BasicProperties.
    /// </summary>
    /// <author>Mark Fisher</author>
    /// <author>Joe Fitzgerald</author>
    public interface IMessagePropertiesConverter
    {
        /// <summary>
        /// Toes the message properties.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="envelope">The envelope.</param>
        /// <param name="charset">The charset.</param>
        /// <returns>The message properties.</returns>
        MessageProperties ToMessageProperties(IBasicProperties source, BasicGetResult envelope, string charset);

        /// <summary>
        /// Froms the message properties.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="source">The source.</param>
        /// <param name="charset">The charset.</param>
        /// <returns>The basic properties.</returns>
        IBasicProperties FromMessageProperties(IModel channel, MessageProperties source, string charset);
    }
}
