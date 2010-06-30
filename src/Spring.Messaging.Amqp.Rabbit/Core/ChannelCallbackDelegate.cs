using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
   /*   public interface IChannelCallback<T>
    {
        T DoInRabbit(IModel model);
    }
    */
    //   public delegate IMessage MessagePostProcessorDelegate(IMessage message);
    public delegate T ChannelCallbackDelegate<T>(IModel channel);
}