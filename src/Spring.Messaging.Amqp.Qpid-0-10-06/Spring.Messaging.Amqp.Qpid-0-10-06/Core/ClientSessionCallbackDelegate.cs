
using org.apache.qpid.client;

namespace Spring.Messaging.Amqp.Qpid.Core
{
    public delegate T ClientSessionCallbackDelegate<T>(IClientSession session);
}