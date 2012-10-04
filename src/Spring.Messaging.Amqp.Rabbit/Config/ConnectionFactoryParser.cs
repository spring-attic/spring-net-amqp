// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionFactoryParser.cs" company="The original author or authors.">
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
using System.Xml;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A connection factory parser.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class ConnectionFactoryParser : AbstractSingleObjectDefinitionParser
    {
        private static readonly string CONNECTION_FACTORY_ATTRIBUTE = "connection-factory";

        private static readonly string CHANNEL_CACHE_SIZE_ATTRIBUTE = "channel-cache-size";

        private static readonly string HOST_ATTRIBUTE = "host";

        private static readonly string PORT_ATTRIBUTE = "port";

        private static readonly string ADDRESSES = "addresses";

        private static readonly string VIRTUAL_HOST_ATTRIBUTE = "virtual-host";

        private static readonly string USER_ATTRIBUTE = "username";

        private static readonly string PASSWORD_ATTRIBUTE = "password";

        private static readonly string EXECUTOR_ATTRIBUTE = "executor";

        private static readonly string PUBLISHER_CONFIRMS = "publisher-confirms";

        private static readonly string PUBLISHER_RETURNS = "publisher-returns";

        /// <summary>The get object type.</summary>
        /// <param name="element">The element.</param>
        /// <returns>The System.Type.</returns>
        protected override Type GetObjectType(XmlElement element) { return typeof(CachingConnectionFactory); }

        /// <summary>Gets a value indicating whether should generate id.</summary>
        protected override bool ShouldGenerateId { get { return false; } }

        /// <summary>Gets a value indicating whether should generate id as fallback.</summary>
        protected override bool ShouldGenerateIdAsFallback { get { return true; } }

        /// <summary>The do parse.</summary>
        /// <param name="element">The element.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <param name="builder">The builder.</param>
        protected override void DoParse(XmlElement element, ParserContext parserContext, ObjectDefinitionBuilder builder)
        {
            if (element.HasAttribute(HOST_ATTRIBUTE) || element.HasAttribute(PORT_ATTRIBUTE))
            {
                parserContext.ReaderContext.ReportFatalException(element, "If the 'addresses' attribute is provided, a connection factory can not have 'host' or 'port' attributes.");
            }

            NamespaceUtils.AddConstructorArgParentRefIfAttributeDefined(builder, element, CONNECTION_FACTORY_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, CHANNEL_CACHE_SIZE_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, HOST_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, PORT_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, USER_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, PASSWORD_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, VIRTUAL_HOST_ATTRIBUTE);
            NamespaceUtils.SetReferenceIfAttributeDefined(builder, element, EXECUTOR_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, ADDRESSES);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, PUBLISHER_CONFIRMS);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, PUBLISHER_RETURNS);
        }
    }
}
