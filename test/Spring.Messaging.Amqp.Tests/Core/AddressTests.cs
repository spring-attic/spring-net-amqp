// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddressTests.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Tests.Core
{
    /// <summary>
    /// Address tests.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Mark Fisher</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class AddressTests
    {
        /// <summary>
        /// Toes the string.
        /// </summary>
        [Test]
        public void ToStringCheck()
        {
            var address = new Address(ExchangeTypes.Direct, "my-exchange", "routing-key");
            var replyToUri = "direct://my-exchange/routing-key";
            Assert.AreEqual(replyToUri, address.ToString());
        }

        /// <summary>
        /// Parses this instance.
        /// </summary>
        [Test]
        public void Parse()
        {
            var replyToUri = "direct://my-exchange/routing-key";
            var address = new Address(replyToUri);
            Assert.AreEqual(address.ExchangeType, ExchangeTypes.Direct);
            Assert.AreEqual(address.ExchangeName, "my-exchange");
            Assert.AreEqual(address.RoutingKey, "routing-key");
        }

        /// <summary>
        /// Unstructureds the with routing key only.
        /// </summary>
        [Test]
        public void UnstructuredWithRoutingKeyOnly()
        {
            var address = new Address("my-routing-key");
            Assert.AreEqual("my-routing-key", address.RoutingKey);
            Assert.AreEqual("direct:///my-routing-key", address.ToString());
        }

        /// <summary>
        /// Withouts the routing key.
        /// </summary>
        [Test]
        public void WithoutRoutingKey()
        {
            var address = new Address("fanout://my-exchange");
            Assert.AreEqual(ExchangeTypes.Fanout, address.ExchangeType);
            Assert.AreEqual("my-exchange", address.ExchangeName);
            Assert.AreEqual(string.Empty, address.RoutingKey);
            Assert.AreEqual("fanout://my-exchange/", address.ToString());
        }

        /// <summary>
        /// Withes the default exchange and routing key.
        /// </summary>
        [Test]
        public void WithDefaultExchangeAndRoutingKey()
        {
            var address = new Address("direct:///routing-key");
            Assert.AreEqual(ExchangeTypes.Direct, address.ExchangeType);
            Assert.AreEqual(string.Empty, address.ExchangeName);
            Assert.AreEqual("routing-key", address.RoutingKey);
            Assert.AreEqual("direct:///routing-key", address.ToString());
        }
    }
}
