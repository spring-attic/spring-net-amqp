using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    public interface IRabbitOperations : IAmqpTemplate
    {
        //TODO consider abstracting out IModel/IClientSession so as to move these methods up.
        void Send(MessageCreatorDelegate messageCreator);

        void Send(string routingkey, MessageCreatorDelegate messageCreator);

        void Send(string exchange, string routingKey, MessageCreatorDelegate messageCreatorDelegate);

        T Execute<T>(IChannelCallback<T> action);

        T Execute<T>(ChannelCallbackDelegate<T> action);
    }
}