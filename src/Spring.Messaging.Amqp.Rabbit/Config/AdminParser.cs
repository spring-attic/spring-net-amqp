// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdminParser.cs" company="The original author or authors.">
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
using System.Xml;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A admin parser.
    /// </summary>
    /// <author>tomas.lukosius@opencredo.com</author>
    /// <author>Joe Fitzgerald</author>
    public class AdminParser : AbstractSingleObjectDefinitionParser
    {
        private static readonly string CONNECTION_FACTORY_ATTRIBUTE = "connection-factory";

        private static readonly string AUTO_STARTUP_ATTRIBUTE = "auto-startup";

        /// <summary>The get object type name.</summary>
        /// <param name="element">The element.</param>
        /// <returns>The System.String.</returns>
        protected override string GetObjectTypeName(XmlElement element) { return typeof(RabbitAdmin).FullName; }

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
            var connectionFactoryRef = element.GetAttribute(CONNECTION_FACTORY_ATTRIBUTE);

            // At least one of 'templateRef' or 'connectionFactoryRef' attribute must be set.
            if (string.IsNullOrWhiteSpace(connectionFactoryRef))
            {
                parserContext.ReaderContext.ReportFatalException(element, "A '" + CONNECTION_FACTORY_ATTRIBUTE + "' attribute must be set.");
            }

            if (!string.IsNullOrWhiteSpace(connectionFactoryRef))
            {
                // Use constructor with connectionFactory parameter
                builder.AddConstructorArgReference(connectionFactoryRef);
            }

            var attributeValue = element.GetAttribute(AUTO_STARTUP_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(attributeValue))
            {
                builder.AddPropertyValue("AutoStartup", attributeValue);
            }
        }
    }
}
