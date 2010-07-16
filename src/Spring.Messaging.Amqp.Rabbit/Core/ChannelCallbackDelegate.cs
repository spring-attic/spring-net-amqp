using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    public delegate T ChannelCallbackDelegate<T>(IModel channel);
}