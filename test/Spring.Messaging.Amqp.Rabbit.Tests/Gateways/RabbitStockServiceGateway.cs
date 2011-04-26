


using System;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core.Support;
using Spring.RabbitQuickStart.Client.Gateways;
using Spring.RabbitQuickStart.Common.Data;

namespace Spring.RabbitQuickStart.Client.Gateways
{
    public class RabbitStockServiceGateway : RabbitGatewaySupport, IStockService
    {
        private string defaultReplyToQueue;
        
        public string DefaultReplyToQueue
        {
            set { defaultReplyToQueue = value; }
        }

        public void Send(TradeRequest tradeRequest)
        {   
            RabbitTemplate.ConvertAndSend(tradeRequest, delegate(Message message)
                                                            {
                                                                message.MessageProperties.ReplyTo = defaultReplyToQueue;
                                                                message.MessageProperties.CorrelationId =
                                                                    new Guid().ToByteArray();
                                                                return message;

                                                            });           
        }        
    }
}