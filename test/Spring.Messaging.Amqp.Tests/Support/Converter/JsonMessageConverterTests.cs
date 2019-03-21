// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JsonMessageConverterTests.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Support.Converter;
#endregion

namespace Spring.Messaging.Amqp.Tests.Support.Converter
{
    /// <summary>
    /// JSON Message Converter Tests
    /// </summary>
    public class JsonMessageConverterTests
    {
        /// <summary>The simple trade.</summary>
        [Test]
        public void SimpleTrade()
        {
            var trade = new SimpleTrade();
            trade.AccountName = "Acct1";
            trade.IsBuyRequest = true;
            trade.OrderType = "Market";
            trade.Price = new decimal(103.30);
            trade.Quantity = 100;
            trade.RequestId = "R123";
            trade.Ticker = "VMW";
            trade.UserName = "Joe Trader";
            var converter = new JsonMessageConverter();
            var message = converter.ToMessage(trade, new MessageProperties());

            var marshalledTrade = (SimpleTrade)converter.FromMessage(message);
            Assert.AreEqual(trade, marshalledTrade);
        }

        /// <summary>The simple trade override converter.</summary>
        [Test]
        public void SimpleTradeOverrideConverter()
        {
            var trade = new SimpleTrade();
            trade.AccountName = "Acct1";
            trade.IsBuyRequest = true;
            trade.OrderType = "Market";
            trade.Price = new decimal(103.30);
            trade.Quantity = 100;
            trade.RequestId = "R123";
            trade.Ticker = "VMW";
            trade.UserName = "Joe Trader";
            var converter = new JsonMessageConverter();
            var mapper = new JsonSerializer();
            converter.JsonSerializer = mapper;
            var message = converter.ToMessage(trade, new MessageProperties());

            var marshalledTrade = (SimpleTrade)converter.FromMessage(message);
            Assert.AreEqual(trade, marshalledTrade);
        }

        /// <summary>The nested object.</summary>
        [Test]
        public void NestedObject()
        {
            var bar = new Bar();
            bar.Foo.Name = "spam";
            var converter = new JsonMessageConverter();
            var message = converter.ToMessage(bar, new MessageProperties());

            var marshalled = (Bar)converter.FromMessage(message);
            Assert.AreEqual(bar, marshalled);
        }

        /// <summary>The hashtable.</summary>
        [Test]
        public void Hashtable()
        {
            var hashtable = new Dictionary<string, object>();
            hashtable.Add("TICKER", "VMW");
            hashtable.Add("PRICE", "103.2");
            var converter = new JsonMessageConverter();
            var message = converter.ToMessage(hashtable, new MessageProperties());
            var marhsalledHashtable = (Dictionary<string, object>)converter.FromMessage(message);
            Assert.AreEqual("VMW", marhsalledHashtable["TICKER"]);
            Assert.AreEqual("103.2", marhsalledHashtable["PRICE"]);
        }

        /// <summary>The foo.</summary>
        public class Foo
        {
            private string name = "foo";

            /// <summary>Gets or sets the name.</summary>
            public string Name { get { return this.name; } set { this.name = value; } }

            /// <summary>The get hash code.</summary>
            /// <returns>The System.Int32.</returns>
            public override int GetHashCode()
            {
                const int prime = 31;
                var result = 1;
                result = prime * result + ((this.name == null) ? 0 : this.name.GetHashCode());
                return result;
            }

            /// <summary>The equals.</summary>
            /// <param name="obj">The obj.</param>
            /// <returns>The System.Boolean.</returns>
            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (obj == null)
                {
                    return false;
                }

                if (typeof(Foo) != obj.GetType())
                {
                    return false;
                }

                var other = (Foo)obj;
                if (this.name == null)
                {
                    if (other.name != null)
                    {
                        return false;
                    }
                }
                else if (!this.name.Equals(other.name))
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>The bar.</summary>
        public class Bar
        {
            private string name = "bar";
            private Foo foo = new Foo();

            /// <summary>Gets or sets the foo.</summary>
            public Foo Foo { get { return this.foo; } set { this.foo = value; } }

            /// <summary>Gets or sets the name.</summary>
            public string Name { get { return this.name; } set { this.name = value; } }

            /// <summary>The get hash code.</summary>
            /// <returns>The System.Int32.</returns>
            public override int GetHashCode()
            {
                const int prime = 31;
                var result = 1;
                result = prime * result + ((this.foo == null) ? 0 : this.foo.GetHashCode());
                result = prime * result + ((this.name == null) ? 0 : this.name.GetHashCode());
                return result;
            }

            /// <summary>The equals.</summary>
            /// <param name="obj">The obj.</param>
            /// <returns>The System.Boolean.</returns>
            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (obj == null)
                {
                    return false;
                }

                if (typeof(Bar) != obj.GetType())
                {
                    return false;
                }

                var other = (Bar)obj;
                if (this.foo == null)
                {
                    if (other.foo != null)
                    {
                        return false;
                    }
                }
                else if (!this.foo.Equals(other.foo))
                {
                    return false;
                }

                if (this.name == null)
                {
                    if (other.name != null)
                    {
                        return false;
                    }
                }
                else if (!this.name.Equals(other.name))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
