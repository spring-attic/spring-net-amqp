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


using NUnit.Framework;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Tests.Core
{
    [TestFixture]
    public class AddressTests
    {

        [Test]
        public void ToString()
        {
            Address address = new Address(ExchangeType.Direct, "my-exchange", "routing-key");
            string replyToUri = "direct://my-exchange/routing-key";
            Assert.AreEqual(replyToUri, address.ToString());
        }
        [Test]
        public void Parse()
        {
            string replyToUri = "direct://my-exchange/routing-key";
            Address address = new Address(replyToUri);
            Assert.AreEqual(address.ExchangeType, ExchangeType.Direct);
            Assert.AreEqual(address.ExchangeName, "my-exchange");
            Assert.AreEqual(address.RoutingKey, "routing-key");
        }

        [Test]
        public void UnstructuredWithRoutingKeyOnly() 
        {
            Address address = new Address("my-routing-key");
            Assert.AreEqual("my-routing-key", address.RoutingKey);
            Assert.AreEqual("direct:///my-routing-key", address.ToString());
        }  

        [Test]
        public void WithoutRoutingKey()
        {
            Address address = new Address("fanout://my-exchange");
            Assert.AreEqual(ExchangeType.Fanout, address.ExchangeType);
            Assert.AreEqual("my-exchange", address.ExchangeName);
            Assert.AreEqual("", address.RoutingKey);
            Assert.AreEqual("fanout://my-exchange/", address.ToString());
        }

        [Test]
        public void WithDefaultExchangeAndRoutingKey()
        {
            Address address = new Address("direct:///routing-key");
            Assert.AreEqual(ExchangeType.Direct, address.ExchangeType);
            Assert.AreEqual("", address.ExchangeName);
            Assert.AreEqual("routing-key", address.RoutingKey);
            Assert.AreEqual("direct:///routing-key", address.ToString());
        }


    }
}   