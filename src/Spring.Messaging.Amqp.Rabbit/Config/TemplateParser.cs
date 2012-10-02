// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TemplateParser.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A template parser.
    /// </summary>
    public class TemplateParser : AbstractSingleObjectDefinitionParser
    {
        private static readonly string CONNECTION_FACTORY_ATTRIBUTE = "connection-factory";

        private static readonly string EXCHANGE_ATTRIBUTE = "exchange";

        private static readonly string QUEUE_ATTRIBUTE = "queue";

        private static readonly string ROUTING_KEY_ATTRIBUTE = "routing-key";

        private static readonly string REPLY_TIMEOUT_ATTRIBUTE = "reply-timeout";

        private static readonly string MESSAGE_CONVERTER_ATTRIBUTE = "message-converter";

        private static readonly string ENCODING_ATTRIBUTE = "encoding";

        private static readonly string CHANNEL_TRANSACTED_ATTRIBUTE = "channel-transacted";

        /// <summary>The get object type.</summary>
        /// <param name="element">The element.</param>
        /// <returns>The System.Type.</returns>
        protected override Type GetObjectType(XmlElement element) { return typeof(RabbitTemplate); }

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

            if (!StringUtils.HasText(connectionFactoryRef))
            {
                parserContext.ReaderContext.ReportFatalException(element, "A '" + CONNECTION_FACTORY_ATTRIBUTE + "' attribute must be set.");
            }

            if (StringUtils.HasText(connectionFactoryRef))
            {
                // Use constructor with connectionFactory parameter
                builder.AddConstructorArgReference(connectionFactoryRef);
            }

            NamespaceUtils.SetValueIfAttributeDefined(builder, element, CHANNEL_TRANSACTED_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, QUEUE_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, EXCHANGE_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, ROUTING_KEY_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, REPLY_TIMEOUT_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, ENCODING_ATTRIBUTE);
            NamespaceUtils.SetReferenceIfAttributeDefined(builder, element, MESSAGE_CONVERTER_ATTRIBUTE);
        }
    }
}
