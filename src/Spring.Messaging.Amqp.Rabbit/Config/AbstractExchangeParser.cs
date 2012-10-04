// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AbstractExchangeParser.cs" company="The original author or authors.">
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
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// An abstract exchange parser
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public abstract class AbstractExchangeParser : AbstractSingleObjectDefinitionParser
    {
        private static readonly string ARGUMENTS_ELEMENT = "exchange-arguments";

        private static readonly string ARGUMENTS_PROPERTY = "Arguments";

        private static readonly string DURABLE_ATTRIBUTE = "durable";

        private static readonly string AUTO_DELETE_ATTRIBUTE = "auto-delete";

        private static string BINDINGS_ELE = "bindings";

        private static string BINDING_ELE = "binding";

        protected static readonly string BINDING_QUEUE_ATTR = "queue";

        protected static readonly string BINDING_EXCHANGE_ATTR = "exchange";

        /// <summary>Gets a value indicating whether should generate id as fallback.</summary>
        protected override bool ShouldGenerateIdAsFallback { get { return true; } }

        /// <summary>The do parse.</summary>
        /// <param name="element">The element.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <param name="builder">The builder.</param>
        protected override void DoParse(XmlElement element, ParserContext parserContext, ObjectDefinitionBuilder builder)
        {
            var exchangeName = element.GetAttribute("name");
            builder.AddConstructorArg(new TypedStringValue(exchangeName));
            this.ParseBindings(element, parserContext, builder, exchangeName);

            NamespaceUtils.AddConstructorArgBooleanValueIfAttributeDefined(builder, element, DURABLE_ATTRIBUTE, true);
            NamespaceUtils.AddConstructorArgBooleanValueIfAttributeDefined(builder, element, AUTO_DELETE_ATTRIBUTE, false);

            var argumentsElements = element.GetElementsByTagName(ARGUMENTS_ELEMENT);
            var argumentsElement = argumentsElements.Count == 1 ? argumentsElements[0] as XmlElement : null;

            if (argumentsElement != null)
            {
                var parser = new ObjectDefinitionParserHelper(parserContext);
                var map = parser.ParseMapElementToTypedDictionary(argumentsElement, builder.RawObjectDefinition);

                builder.AddConstructorArg(map);
            }
        }

        /// <summary>The parse bindings.</summary>
        /// <param name="element">The element.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="exchangeName">The exchange name.</param>
        protected virtual void ParseBindings(XmlElement element, ParserContext parserContext, ObjectDefinitionBuilder builder, string exchangeName)
        {
            var bindings = element.GetElementsByTagName(BINDINGS_ELE);
            var bindingElement = bindings.Count == 1 ? bindings[0] as XmlElement : null;

            this.DoParseBindings(parserContext, exchangeName, bindingElement, this);
        }

        /// <summary>The do parse bindings.</summary>
        /// <param name="parserContext">The parser context.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="bindings">The bindings.</param>
        /// <param name="parser">The parser.</param>
        protected void DoParseBindings(ParserContext parserContext, string exchangeName, XmlElement bindings, AbstractExchangeParser parser)
        {
            if (bindings != null)
            {
                foreach (var binding in bindings.GetElementsByTagName(BINDING_ELE))
                {
                    var objectDefinition = this.ParseBinding(exchangeName, binding as XmlElement, parserContext);
                    this.RegisterObjectDefinition(new ObjectDefinitionHolder(objectDefinition, parserContext.ReaderContext.GenerateObjectName(objectDefinition)), parserContext.Registry);
                }
            }
        }

        /// <summary>The parse binding.</summary>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="binding">The binding.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <returns>The Spring.Objects.Factory.Support.AbstractObjectDefinition.</returns>
        protected abstract AbstractObjectDefinition ParseBinding(string exchangeName, XmlElement binding, ParserContext parserContext);

        /// <summary>The parse destination.</summary>
        /// <param name="binding">The binding.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <param name="builder">The builder.</param>
        protected void ParseDestination(XmlElement binding, ParserContext parserContext, ObjectDefinitionBuilder builder)
        {
            var queueAttribute = binding.GetAttribute(BINDING_QUEUE_ATTR);
            var exchangeAttribute = binding.GetAttribute(BINDING_EXCHANGE_ATTR);
            var hasQueueAttribute = string.IsNullOrWhiteSpace(queueAttribute);
            var hasExchangeAttribute = string.IsNullOrWhiteSpace(exchangeAttribute);
            if (!(hasQueueAttribute ^ hasExchangeAttribute))
            {
                parserContext.ReaderContext.ReportFatalException(binding, "Binding must have exactly one of 'queue' or 'exchange'");
            }

            if (hasQueueAttribute)
            {
                builder.AddPropertyReference("destinationQueue", queueAttribute);
            }

            if (hasExchangeAttribute)
            {
                builder.AddPropertyReference("destinationExchange", exchangeAttribute);
            }
        }
    }
}
