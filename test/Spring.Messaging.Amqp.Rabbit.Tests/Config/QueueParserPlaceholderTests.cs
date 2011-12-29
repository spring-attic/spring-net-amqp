
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using Spring.Context;
using Spring.Context.Support;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{

    /// <summary>
    /// QueueParserPlaceholder Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
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


        [Test]
        public void PropertyPlaceHolderConfigurerCanConfigPropertyOnNonRabbitObject()
        {
            var obj = beanFactory.GetObject<PlaceholderSanityCheckTestObject>("placeholder-sanity-check");
            Assert.That(obj.Name, Is.EqualTo("foo"),"PropertyConfiguration infrastructure is not working as expected.");
        }

        [Test]
        public void CanGetRabbitQueue()
        {
            var obj = beanFactory.GetObject<Queue>("arguments");
            Assert.That(obj.Arguments["foo"], Is.EqualTo("bar"));
            Assert.That(obj.Arguments["bar"], Is.EqualTo("baz"));
        }




        [TestFixtureTearDown]
	    public void closeBeanFactory() 
        {
		    if (beanFactory != null) 
            {
			    ((IConfigurableApplicationContext)beanFactory).Dispose();
            }
	    }
    }


    public class PlaceholderSanityCheckTestObject
    {
        public string Name { get; set; }
    }
}
