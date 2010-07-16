using System;
using System.Text;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    [TestFixture]
    [Ignore] // This is an integration test - can't be run by the test runner yet.
    public class ProducerExample : AbstractExample
    {
        [Test]
        public void RawApi()
        {
            RabbitTemplate template = InitializeAndCreateTemplate();
            IConnection connection = connectionFactory.CreateConnection();
            IModel model = connection.CreateModel();
            model.BasicPublish(TestConstants.EXCHANGE_NAME, TestConstants.ROUTING_KEY, null, Encoding.UTF8.GetBytes("testing"));
        }

        [Test]
        public void Producer()
        {
            RabbitTemplate template = InitializeAndCreateTemplate();
            
            SendMessages(template, TestConstants.EXCHANGE_NAME, TestConstants.ROUTING_KEY, TestConstants.NUM_MESSAGES);
        }

        private void SendMessages(RabbitTemplate template, string exchange, string routingKey, int numMessages)
        {
            for (int i = 1; i <= numMessages; i++)
            {
                Console.WriteLine("sending...");
                template.ConvertAndSend(exchange, routingKey, "test-" + i);
                /*
                template.Send(exchange, routingKey, delegate
                                                        {                                                            
                                                            return new Message(Encoding.UTF8.GetBytes("testing"),
                                                                                 template.CreateMessageProperties());
                                                        });
                 */
            }
;
        }

        private void OldSendMessages(RabbitTemplate template)
        {
            IBasicProperties basicProperties = template.Execute<IBasicProperties>(delegate(IModel model)
                                                                                      {
                                                                                          return model.CreateBasicProperties();
                                                                                      });

            
            /*
             * System.ArgumentNullException: String reference not set to an instance of a String.
                Parameter name: s
                    at System.Text.Encoding.GetBytes(String s)
                    at RabbitMQ.Client.Impl.WireFormatting.WriteShortstr(NetworkBinaryWriter writer, String val)
             */
            IMessageProperties messageProperties = new MessageProperties(basicProperties);


            //write all short props
            messageProperties.ContentType = "text/plain";
            messageProperties.ContentEncoding = "UTF-8";
            messageProperties.CorrelationId = Encoding.UTF8.GetBytes("corr1");


            messageProperties.DeliveryMode = MessageDeliveryMode.PERSISTENT;
            messageProperties.Priority = 0;
  

            
            byte[] byteMessage = Encoding.UTF8.GetBytes("testing");
            template.Send("amq.direct", "foo", delegate
                                                   {
                                                       Message msg = new Message(byteMessage, messageProperties);                                                    
                                                       Console.WriteLine("sending...");
                                                       return msg;
                                                   });

            //template.Send("amq.direct", "foo", channel => new Message(Encoding.UTF8.GetBytes("testing"), messageProperties));
        }
    }



}