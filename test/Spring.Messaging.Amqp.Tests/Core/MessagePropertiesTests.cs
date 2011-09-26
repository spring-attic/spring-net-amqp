
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Tests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Message properties tests.
    /// </summary>
    public class MessagePropertiesTests
    {
        /// <summary>
        /// Tests the reply to.
        /// </summary>
        [Test]
        public void TestReplyTo()
        {
            var properties = new MessageProperties();
            properties.ReplyTo = "fanout://foo/bar";
            Assert.AreEqual("bar", properties.ReplyToAddress.RoutingKey);
        }

        /// <summary>
        /// Tests the reply to null by default.
        /// </summary>
        [Test]
        public void TestReplyToNullByDefault()
        {
            var properties = new MessageProperties();
            Assert.AreEqual(null, properties.ReplyTo);
            Assert.AreEqual(null, properties.ReplyToAddress);
        }
    }
}
