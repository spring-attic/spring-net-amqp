using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Support.Converter;

namespace Spring.Messaging.Amqp.Tests.Support.Converter
{
    /// <summary>
    /// JSON Message Converter Tests
    /// </summary>
    public class JsonMessageConverterTests
    {
        [Test]
	    public void SimpleTrade() {
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
            var mapper = new Newtonsoft.Json.JsonSerializer();
            converter.JsonSerializer = mapper;
            var message = converter.ToMessage(trade, new MessageProperties());

            var marshalledTrade = (SimpleTrade)converter.FromMessage(message);
            Assert.AreEqual(trade, marshalledTrade);
        }

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

        [Test]
        // TODO: Test for Java / .NET Interop
        public void Hashtable()
        {
            var hashtable = new Dictionary<String, Object>();
            hashtable.Add("TICKER", "VMW");
            hashtable.Add("PRICE", "103.2");
            var converter = new JsonMessageConverter();
            var message = converter.ToMessage(hashtable, new MessageProperties());
            var marhsalledHashtable = (Dictionary<String, Object>)converter.FromMessage(message);
            Assert.AreEqual("VMW", marhsalledHashtable["TICKER"]);
            Assert.AreEqual("103.2", marhsalledHashtable["PRICE"]);
        }

        public class Foo 
        {
		    private string name = "foo";

		    public string Name
            {
                get { return this.name; }
                set { this.name = value; }
		    }

		    public override int GetHashCode() {
			    const int prime = 31;
			    var result = 1;
			    result = prime * result + ((name == null) ? 0 : name.GetHashCode());
			    return result;
		    }

		    public override bool Equals(Object obj) {
			    if (this == obj)
				    return true;
			    if (obj == null)
				    return false;
			    if (typeof(Foo) != obj.GetType())
				    return false;
			    var other = (Foo)obj;
			    if (name == null) {
				    if (other.name != null)
					    return false;
			    } else if (!name.Equals(other.name))
				    return false;
			    return true;
		    }   
	    }

        public class Bar 
        {
		    private string name = "bar";
		    private Foo foo = new Foo();

		    public Foo Foo
            {
                get { return this.foo; }
                set { this.foo = value; }
		    }

            public string Name
            {
                get { return this.name; }
                set { this.name = value; }
            }

		    public override int GetHashCode() {
			    const int prime = 31;
			    var result = 1;
			    result = prime * result + ((foo == null) ? 0 : foo.GetHashCode());
			    result = prime * result + ((name == null) ? 0 : name.GetHashCode());
			    return result;
		    }

		    public override bool Equals(Object obj) {
			    if (this == obj)
				    return true;
			    if (obj == null)
				    return false;
			    if (typeof(Bar) != obj.GetType())
				    return false;
			    var other = (Bar)obj;
			    if (foo == null) {
				    if (other.foo != null)
					    return false;
			    } else if (!foo.Equals(other.foo))
				    return false;
			    if (name == null) {
				    if (other.name != null)
					    return false;
			    } else if (!name.Equals(other.name))
				    return false;
			    return true;
		    }
	    }
    }
}
