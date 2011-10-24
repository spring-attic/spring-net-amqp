
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Common.Logging;

using NUnit.Framework;

using RabbitMQ.Client;

using Spring.Context;
using Spring.Context.Config;
using Spring.Context.Support;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory.Parsing;
using Spring.Objects.Factory.Xml;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{

    /// <summary>
    /// AdminParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class AdminParserTests
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        // Specifies if test case expects context to be valid or not: true - context expects to be valid.
        private bool validContext = true;

        // Index of context file used by this test case. Context file name has such template:
        // <class-name>-<contextIndex>-context.xml.
        private int contextIndex;

        private bool expectedAutoStartup;

        private string adminBeanName;

        private bool initialisedWithTemplate;

        [Test]
        public void TestInvalid()
        {
            contextIndex = 1;
            validContext = false;
            this.DoTest();
        }

        [Test]
        public void TestValid()
        {
            contextIndex = 2;
            validContext = true;
            this.DoTest();
        }

        private void DoTest()
        {
            // Create context
            XmlObjectFactory beanFactory = LoadContext();
            if (beanFactory == null)
            {
                // Context was invalid
                return;
            }

            // Validate values
            RabbitAdmin admin;
            if (StringUtils.HasText(adminBeanName))
            {
                admin = beanFactory.GetObject(adminBeanName, typeof(RabbitAdmin)) as RabbitAdmin;
            }
            else
            {
                admin = beanFactory.GetObject(typeof(RabbitAdmin).Name) as RabbitAdmin;
            }
            Assert.AreEqual(expectedAutoStartup, admin.AutoStartup);
            Assert.AreEqual(
                beanFactory.GetObject(typeof(ConnectionFactory).Name), admin.RabbitTemplate.ConnectionFactory);

            if (initialisedWithTemplate)
            {
                Assert.AreEqual(beanFactory.GetObject(typeof(RabbitTemplate).Name), admin.RabbitTemplate);
            }

        }

        private XmlObjectFactory LoadContext()
        {
            XmlObjectFactory beanFactory = null;
            try
            {
                var resourceNames = typeof(AdminParserTests).Assembly.GetManifestResourceNames();
                foreach (var resourceNameString in resourceNames)
                {
                    var info = typeof(AdminParserTests).Assembly.GetManifestResourceInfo(resourceNameString);
                    Logger.Info(resourceNameString);
                }
                // Resource file name template: <class-name>-<contextIndex>-context.xml
                var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/" + typeof(AdminParserTests).Name + "-" + contextIndex + "-context.xml";
                Logger.Info("Resource Name: " + resourceName);
                var resource = new AssemblyResource(resourceName);
                beanFactory = new XmlObjectFactory(resource);
                if (!validContext)
                {
                    Assert.Fail("Context " + resource + " suppose to fail");
                }
            }
            catch (ObjectDefinitionParsingException e)
            {
                if (validContext)
                {
                    // Context expected to be valid - throw an exception up
                    throw e;
                }

                Logger.Warn("Failure was expected", e);
            }
            return beanFactory;
        }
    }
}
