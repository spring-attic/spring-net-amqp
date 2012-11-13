// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdminParserTests.cs" company="The original author or authors.">
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
using System;
using Common.Logging;
using NUnit.Framework;
using Spring.Core.IO;
using Spring.Messaging.Amqp.Rabbit.Config;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Parsing;
using Spring.Objects.Factory.Xml;
using Spring.Util;
#endregion

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

        /// <summary>The setup.</summary>
        [TestFixtureSetUp]
        public void Setup() { NamespaceParserRegistry.RegisterParser(typeof(RabbitNamespaceHandler)); }

        /// <summary>The test invalid.</summary>
        [Test]
        public void TestInvalid()
        {
            this.contextIndex = 1;
            this.validContext = false;
            this.DoTest();
        }

        /// <summary>The test valid.</summary>
        [Test]
        public void TestValid()
        {
            this.contextIndex = 2;
            this.validContext = true;
            this.DoTest();
        }

        private void DoTest()
        {
            // Create context
            XmlObjectFactory objectFactory = this.LoadContext();
            if (objectFactory == null)
            {
                // Context was invalid
                return;
            }

            // Validate values
            RabbitAdmin admin;
            if (StringUtils.HasText(this.adminObjectName))
            {
                admin = objectFactory.GetObject<RabbitAdmin>(this.adminObjectName);
            }
            else
            {
                admin = objectFactory.GetObject<RabbitAdmin>();
            }

            Assert.AreEqual(this.expectedAutoStartup, admin.AutoStartup);
            Assert.AreEqual(objectFactory.GetObject<IConnectionFactory>(), admin.RabbitTemplate.ConnectionFactory);

            if (this.initialisedWithTemplate)
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
                var resourceName = @"assembly://Spring.Messaging.Amqp.Rabbit.Tests/Spring.Messaging.Amqp.Rabbit.Tests.Config/" + typeof(AdminParserTests).Name + "-" + this.contextIndex + "-context.xml";
                Logger.Info("Resource Name: " + resourceName);
                var resource = new AssemblyResource(resourceName);
                objectFactory = new XmlObjectFactory(resource);
                if (!this.validContext)
                {
                    Assert.Fail("Context " + resource + " suppose to fail");
                }
            }
            catch (Exception e)
            {
                if (e is ObjectDefinitionParsingException || e is ObjectDefinitionStoreException)
                {
                    if (this.validContext)
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
