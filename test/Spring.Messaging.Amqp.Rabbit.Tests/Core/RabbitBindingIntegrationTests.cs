
using System;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Test;
using Spring.Messaging.Amqp.Support.Converter;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// Rabbit Binding Integration Tests
    /// </summary>
    /// <remarks></remarks>
    public class RabbitBindingIntegrationTests
    {
        private static Queue queue = new Queue("test.queue");

        private IConnectionFactory connectionFactory = new CachingConnectionFactory();

        private RabbitTemplate template;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        /// <remarks></remarks>
        public RabbitBindingIntegrationTests()
        {
            this.template = new RabbitTemplate(this.connectionFactory);
        }

        /* @Rule  */
        public BrokerRunning brokerIsRunning = BrokerRunning.IsRunningWithEmptyQueues(queue);

        /// <summary>
        /// Tests the send and receive with topic single callback.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveWithTopicSingleCallback()
        {
            var admin = new RabbitAdmin(this.connectionFactory);
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);
            this.template.Exchange = exchange.Name;

            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange).With("*.end"));

            this.template.Execute<object>(delegate(IModel channel)
                                         {
                                             var consumer = this.CreateConsumer(this.template);
                                             var tag = consumer.ConsumerTag;
                                             Assert.IsNotNull(tag);

                                             this.template.ConvertAndSend("foo", "message");

                                             try
                                             {
                                                 var result = this.GetResult(consumer);
                                                 Assert.AreEqual(null, result);

                                                 this.template.ConvertAndSend("foo.end", "message");
                                                 result = this.GetResult(consumer);
                                                 Assert.AreEqual("message", result);

                                             }
                                             finally
                                             {
                                                 try
                                                 {
                                                     channel.BasicCancel(tag);
                                                 }
                                                 catch (Exception e)
                                                 {
                                                     // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                                                     Console.WriteLine(e.Message);
                                                 }
                                             }

                                             return null;
                                         });
        }

        /// <summary>
        /// Tests the send and receive with non default exchange.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveWithNonDefaultExchange()
        {
            var admin = new RabbitAdmin(this.connectionFactory);
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);

            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange).With("*.end"));

            this.template.Execute<object>(delegate(IModel channel)
            {
                var consumer = this.CreateConsumer(this.template);
                var tag = consumer.ConsumerTag;
                Assert.IsNotNull(tag);

                this.template.ConvertAndSend("topic", "foo", "message");

                try
                {

                    var result = this.GetResult(consumer);
                    Assert.AreEqual(null, result);

                    this.template.ConvertAndSend("topic", "foo.end", "message");
                    result = this.GetResult(consumer);
                    Assert.AreEqual("message", result);

                }
                finally
                {
                    try
                    {
                        channel.BasicCancel(tag);
                    }
                    catch (Exception e)
                    {
                        // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                        Console.WriteLine(e.Message);
                    }
                }

                return null;
            });
        }

        /// <summary>
        /// Tests the send and receive with topic consume in background.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveWithTopicConsumeInBackground()
        {
            var admin = new RabbitAdmin(this.connectionFactory);
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);
            this.template.Exchange = exchange.Name;

            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange).With("*.end"));

            var template = new RabbitTemplate(new CachingConnectionFactory());
            template.Exchange = exchange.Name;

            var consumer = this.template.Execute<BlockingQueueConsumer>(delegate(IModel channel)
            {
                var consumerinside = this.CreateConsumer(template);
                var tag = consumerinside.ConsumerTag;
                Assert.IsNotNull(tag);

                return consumerinside;
            });

            template.ConvertAndSend("foo", "message");
            var result = this.GetResult(consumer);
            Assert.AreEqual(null, result);

            this.template.ConvertAndSend("foo.end", "message");
            result = this.GetResult(consumer);
            Assert.AreEqual("message", result);

            try
            {
                consumer.Model.BasicCancel(consumer.ConsumerTag);
            }
            catch (Exception e)
            {
                // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Tests the send and receive with topic two callbacks.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveWithTopicTwoCallbacks()
        {
            var admin = new RabbitAdmin(this.connectionFactory);
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);
            this.template.Exchange = exchange.Name;

            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange).With("*.end"));

            this.template.Execute<object>(delegate(IModel channel)
                                         {
                                             var consumer = this.CreateConsumer(this.template);
                                             var tag = consumer.ConsumerTag;
                                             Assert.IsNotNull(tag);

                                             try
                                             {
                                                 this.template.ConvertAndSend("foo", "message");
                                                 var result = this.GetResult(consumer);
                                                 Assert.AreEqual(null, result);
                                             }
                                             finally
                                             {
                                                 try
                                                 {
                                                     channel.BasicCancel(tag);
                                                 }
                                                 catch (Exception e)
                                                 {
                                                     // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                                                     Console.WriteLine(e.Message);
                                                 }
                                             }

                                             return null;
                                         });

            this.template.Execute<object>(delegate(IModel channel)
                                         {
                                             var consumer = this.CreateConsumer(this.template);
                                             var tag = consumer.ConsumerTag;
                                             Assert.IsNotNull(tag);

                                             try
                                             {
                                                 // TODO: Bug here somewhere...
                                                 this.template.ConvertAndSend("foo.end", "message");
                                                 var result = this.GetResult(consumer);
                                                 Assert.AreEqual("message", result);
                                             }
                                             finally
                                             {
                                                 try
                                                 {
                                                     channel.BasicCancel(tag);
                                                 }
                                                 catch (Exception e)
                                                 {
                                                     // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                                                     Console.WriteLine(e.Message);
                                                 }
                                             }

                                             return null;
                                         });
        }

        /// <summary>
        /// Tests the send and receive with fanout.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TestSendAndReceiveWithFanout()
        {
            var admin = new RabbitAdmin(this.connectionFactory);
            var exchange = new FanoutExchange("fanout");
            admin.DeclareExchange(exchange);
            this.template.Exchange = exchange.Name;

            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange));

            this.template.Execute<object>(delegate(IModel channel)
                                         {
                                             var consumer = this.CreateConsumer(this.template);
                                             var tag = consumer.ConsumerTag;
                                             Assert.IsNotNull(tag);

                                             try
                                             {
                                                 this.template.ConvertAndSend("message");
                                                 var result = this.GetResult(consumer);
                                                 Assert.AreEqual("message", result);
                                             }
                                             finally
                                             {
                                                 try
                                                 {
                                                     channel.BasicCancel(tag);
                                                 }
                                                 catch (Exception e)
                                                 {
                                                     // TODO: this doesn't make sense. Looks like there is a bug in the rabbitmq.client code here: http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/2f12b3b4d6bd/projects/client/RabbitMQ.Client/src/client/impl/ModelBase.cs#l1018
                                                     Console.WriteLine(e.Message);
                                                 }
                                             }

                                             return null;
                                         });
        }

        /// <summary>
        /// Creates the consumer.
        /// </summary>
        /// <param name="accessor">The accessor.</param>
        /// <returns>The consumer.</returns>
        /// <remarks></remarks>
        private BlockingQueueConsumer CreateConsumer(RabbitAccessor accessor)
        {
            var consumer = new BlockingQueueConsumer(accessor.ConnectionFactory, new ActiveObjectCounter<BlockingQueueConsumer>(), AcknowledgeModeUtils.AcknowledgeMode.AUTO, true, 1, 0, queue.Name);
            consumer.Start();
            return consumer;
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <param name="consumer">The consumer.</param>
        /// <returns>The result.</returns>
        /// <remarks></remarks>
        private string GetResult(BlockingQueueConsumer consumer)
        {
            var response = consumer.NextMessage(new TimeSpan(0, 0, 0, 20));
            if (response == null)
            {
                return null;
            }

            return (string)new SimpleMessageConverter().FromMessage(response);
        }
    }
}
