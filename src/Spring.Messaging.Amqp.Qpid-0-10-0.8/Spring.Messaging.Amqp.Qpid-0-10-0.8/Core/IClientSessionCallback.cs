using org.apache.qpid.client;

namespace Spring.Messaging.Amqp.Qpid.Core
{
    public interface IClientSessionCallback<T>
    {
        T DoInSession(IClientSession model);
    }
}