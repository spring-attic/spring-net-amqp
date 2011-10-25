
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Spring.Context;
using Spring.Context.Support;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{

    /// <summary>
    /// QueueParserPlaceholder Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    [Ignore("Need to fix...")]
    public class QueueParserPlaceholderTests : QueueParserTests
    {
        /// <summary>
        /// Setups this instance.
        /// </summary>
        [TestFixtureSetUp]
        public override void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
            var resourceName =
                @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/"
                + typeof(QueueParserPlaceholderTests).Name + "-context.xml";
            //var resource = new AssemblyResource(resourceName);
            beanFactory = new XmlApplicationContext(resourceName);
        }

        [TestFixtureTearDown]
	    public void closeBeanFactory() 
        {
		    if (beanFactory != null) 
            {
			    ((IConfigurableApplicationContext)beanFactory).Dispose();;
		    }
	    }
    }
}
