using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Qpid.Core
{
    public interface IQpidOperations : IAmqpTemplate
    {
        void Send(MessageCreatorDelegate messageCreator);

        void Send(string routingkey, MessageCreatorDelegate messageCreator);

        void Send(string exchange, string routingKey, MessageCreatorDelegate messageCreatorDelegate);


        T Execute<T>(IClientSessionCallback<T> action);

        T Execute<T>(ClientSessionCallbackDelegate<T> action);
    }
}