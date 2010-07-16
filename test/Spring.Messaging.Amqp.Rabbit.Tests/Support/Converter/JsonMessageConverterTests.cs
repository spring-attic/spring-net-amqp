#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System.Collections;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Support.Converter;

namespace Spring.Messaging.Amqp.Rabbit.Support.Converter
{
    [TestFixture]
    public class JsonMessageConverterTests
    {
        private RabbitTemplate template;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {            
            IConnectionFactory connectionFactory = new CachingConnectionFactory();


            template = new RabbitTemplate();
            template.ConnectionFactory = connectionFactory;
            template.ChannelTransacted = true;
            template.AfterPropertiesSet();
        }
        [Test]
        public void SimpleTrade()
        {
            SimpleTrade trade = new SimpleTrade();
            trade.AccountName = "Acct1";
            trade.BuyRequest = true;
            trade.OrderType = "Market";
            trade.Price = new decimal(103.30);
            trade.Quantity = 100;
            trade.RequestId = "R123";
            trade.Ticker = "VMW";
            trade.UserName = "Joe Trader";

            JsonMessageConverter converter = CreateConverter();
            Message message = template.Execute(delegate(IModel channel)
                                                   {
                                                       return converter.ToMessage(trade, new RabbitMessagePropertiesFactory(channel));
                                                   });

            object typeIdHeaderObj = message.MessageProperties.Headers[TypeMapper.DEFAULT_TYPEID_FIELD_NAME];
            Assert.AreEqual(typeof(string), typeIdHeaderObj.GetType());

            

            SimpleTrade marshalledTrade = (SimpleTrade) converter.FromMessage(message);

            Assert.AreEqual(trade, marshalledTrade);


        }

        [Test]
        public void Dictionary()
        {
            Hashtable hashtable = new Hashtable();
            hashtable["TICKER"] = "VMW";
            hashtable["PRICE"] = "103.2";
            JsonMessageConverter converter = CreateConverter();
            Message message = template.Execute(delegate(IModel channel)
            {
                return converter.ToMessage(hashtable, new RabbitMessagePropertiesFactory(channel));
            });

            Hashtable marshalledHashtable = (Hashtable) converter.FromMessage(message);

            Assert.AreEqual("VMW", marshalledHashtable["TICKER"]);
            Assert.AreEqual("103.2", marshalledHashtable["PRICE"]);


        }


        private JsonMessageConverter CreateConverter()
        {
            JsonMessageConverter converter = new JsonMessageConverter();
            TypeMapper typeMapper = new TypeMapper();
            typeMapper.DefaultAssemblyName = "Spring.Messaging.Amqp.Rabbit.Tests";
            typeMapper.DefaultNamespace = "Spring.Messaging.Amqp.Rabbit.Support.Converter";
            converter.TypeMapper = typeMapper;
            return converter;
        }

    }
}