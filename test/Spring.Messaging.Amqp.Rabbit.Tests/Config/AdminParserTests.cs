
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Common.Logging;

using NUnit.Framework;

using RabbitMQ.Client;

using Spring.Context;
//using Spring.Context.Config;
using Spring.Context.Support;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Parsing;
using Spring.Objects.Factory.Xml;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{

    /// <summary>
    /// AdminParser Tests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class AdminParserTests
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        // Specifies if test case expects context to be valid or not: true - context expects to be valid.
        private bool validContext = true;

        // Index of context file used by this test case. Context file name has such template:
        // <class-name>-<contextIndex>-context.xml.
        private int contextIndex;

        private bool expectedAutoStartup;

        private string adminObjectName;

        private bool initialisedWithTemplate;
        
        [TestFixtureSetUp]
        public void Setup()
        {
            NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler));
        }

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
            XmlObjectFactory objectFactory = LoadContext();
            if (objectFactory == null)
            {
                // Context was invalid
                return;
            }

            // Validate values
            RabbitAdmin admin;
            if (StringUtils.HasText(adminObjectName))
            {
                admin = objectFactory.GetObject<RabbitAdmin>(adminObjectName);
            }
            else
            {
                admin = objectFactory.GetObject<RabbitAdmin>();
            }
            Assert.AreEqual(expectedAutoStartup, admin.AutoStartup);
            Assert.AreEqual(objectFactory.GetObject<IConnectionFactory>(), admin.RabbitTemplate.ConnectionFactory);

            if (initialisedWithTemplate)
            {
                Assert.AreEqual(objectFactory.GetObject<RabbitTemplate>(), admin.RabbitTemplate);
            }

        }

        private XmlObjectFactory LoadContext()
        {
            XmlObjectFactory objectFactory = null;
            try
            {
                // Resource file name template: <class-name>-<contextIndex>-context.xml
                var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/" + typeof(AdminParserTests).Name + "-" + contextIndex + "-context.xml";
                Logger.Info("Resource Name: " + resourceName);
                var resource = new AssemblyResource(resourceName);
                objectFactory = new XmlObjectFactory(resource);
                if (!validContext)
                {
                    Assert.Fail("Context " + resource + " suppose to fail");
                }
            }
            catch (Exception e)
            {
                if (e is ObjectDefinitionParsingException || e is ObjectDefinitionStoreException)
                {
                    if (validContext)
                    {
                        // Context expected to be valid - throw an exception up
                        throw e;
                    }

                    Logger.Warn("Failure was expected", e);
                }
                else
                {
                    throw;
                }
            }
            return objectFactory;
        }
    }
}
