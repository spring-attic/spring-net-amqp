// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ListenerContainerParser.cs" company="The original author or authors.">
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
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A listener container parser.
    /// </summary>
    /// <author>Mark Fisher</author>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald</author>
    public class ListenerContainerParser : IObjectDefinitionParser
    {
        private static readonly string LISTENER_ELEMENT = "listener";

        private static readonly string ID_ATTRIBUTE = "id";

        private static readonly string QUEUE_NAMES_ATTRIBUTE = "queue-names";

        private static readonly string QUEUES_ATTRIBUTE = "queues";

        private static readonly string REF_ATTRIBUTE = "ref";

        private static readonly string METHOD_ATTRIBUTE = "method";

        private static readonly string MESSAGE_CONVERTER_ATTRIBUTE = "message-converter";

        private static readonly string RESPONSE_EXCHANGE_ATTRIBUTE = "response-exchange";

        private static readonly string RESPONSE_ROUTING_KEY_ATTRIBUTE = "response-routing-key";

        /// <summary>The parse element.</summary>
        /// <param name="element">The element.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <returns>The Spring.Objects.Factory.Config.IObjectDefinition.</returns>
        public IObjectDefinition ParseElement(XmlElement element, ParserContext parserContext)
        {
            // var compositeDef = new GenericObjectDefinition(); //parserContext.ParserHelper.ParseCustomElement(element), element.LocalName);
            // parserContext.ContainingObjectDefinition = parserContext.ParserHelper.pushContainingComponent(compositeDef);
            var childNodes = element.ChildNodes;
            foreach (XmlNode child in childNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    var localName = child.LocalName;
                    if (LISTENER_ELEMENT.Equals(localName))
                    {
                        this.ParseListener((XmlElement)child, element, parserContext);
                    }
                }
            }

            // parserContext.Registry.RegisterObjectDefinition(compositeDef.ObjectName, compositeDef.ObjectDefinition);
            return null;
        }

        private void ParseListener(XmlElement listenerEle, XmlElement containerEle, ParserContext parserContext)
        {
            var listenerDef = new RootObjectDefinition();

            // listenerDef.setSource(parserContext.extractSource(listenerEle));
            var aRef = listenerEle.GetAttribute(REF_ATTRIBUTE);
            if (string.IsNullOrWhiteSpace(aRef))
            {
                parserContext.ReaderContext.ReportFatalException(listenerEle, "Listener 'ref' attribute contains empty value.");
            }
            else
            {
                listenerDef.PropertyValues.Add("HandlerObject", new RuntimeObjectReference(aRef));
            }

            string method = null;
            if (listenerEle.HasAttribute(METHOD_ATTRIBUTE))
            {
                method = listenerEle.GetAttribute(METHOD_ATTRIBUTE);
                if (string.IsNullOrWhiteSpace(method))
                {
                    parserContext.ReaderContext.ReportFatalException(listenerEle, "Listener 'method' attribute contains empty value.");
                }
            }

            listenerDef.PropertyValues.Add("DefaultListenerMethod", method);

            if (containerEle.HasAttribute(MESSAGE_CONVERTER_ATTRIBUTE))
            {
                var messageConverter = containerEle.GetAttribute(MESSAGE_CONVERTER_ATTRIBUTE);
                if (string.IsNullOrWhiteSpace(messageConverter))
                {
                    parserContext.ReaderContext.ReportFatalException(containerEle, "Listener container 'message-converter' attribute contains empty value.");
                }
                else
                {
                    listenerDef.PropertyValues.Add("MessageConverter", new RuntimeObjectReference(messageConverter));
                }
            }

            var containerDef = RabbitNamespaceUtils.ParseContainer(listenerEle, containerEle, parserContext);

            if (listenerEle.HasAttribute(RESPONSE_EXCHANGE_ATTRIBUTE))
            {
                var responseExchange = listenerEle.GetAttribute(RESPONSE_EXCHANGE_ATTRIBUTE);
                listenerDef.PropertyValues.Add("ResponseExchange", responseExchange);
            }

            if (listenerEle.HasAttribute(RESPONSE_ROUTING_KEY_ATTRIBUTE))
            {
                var responseRoutingKey = listenerEle.GetAttribute(RESPONSE_ROUTING_KEY_ATTRIBUTE);
                listenerDef.PropertyValues.Add("ResponseRoutingKey", responseRoutingKey);
            }

            listenerDef.ObjectTypeName = "Spring.Messaging.Amqp.Rabbit.Listener.Adapter.MessageListenerAdapter";
            containerDef.PropertyValues.Add("MessageListener", listenerDef);

            var containerObjectName = containerEle.GetAttribute(ID_ATTRIBUTE);

            // If no object id is given auto generate one using the ReaderContext's ObjectNameGenerator
            if (string.IsNullOrWhiteSpace(containerObjectName))
            {
                containerObjectName = parserContext.ReaderContext.GenerateObjectName(containerDef);
            }

            if (!NamespaceUtils.IsAttributeDefined(listenerEle, QUEUE_NAMES_ATTRIBUTE) && !NamespaceUtils.IsAttributeDefined(listenerEle, QUEUES_ATTRIBUTE))
            {
                parserContext.ReaderContext.ReportFatalException(listenerEle, "Listener 'queue-names' or 'queues' attribute must be provided.");
            }

            if (NamespaceUtils.IsAttributeDefined(listenerEle, QUEUE_NAMES_ATTRIBUTE) && NamespaceUtils.IsAttributeDefined(listenerEle, QUEUES_ATTRIBUTE))
            {
                parserContext.ReaderContext.ReportFatalException(listenerEle, "Listener 'queue-names' or 'queues' attribute must be provided but not both.");
            }

            var queueNames = listenerEle.GetAttribute(QUEUE_NAMES_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(queueNames))
            {
                var names = StringUtils.CommaDelimitedListToStringArray(queueNames);
                var values = new ManagedList();
                foreach (var name in names)
                {
                    values.Add(new TypedStringValue(name.Trim()));
                }

                containerDef.PropertyValues.Add("QueueNames", values);
            }

            var queues = listenerEle.GetAttribute(QUEUES_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(queues))
            {
                var names = StringUtils.CommaDelimitedListToStringArray(queues);
                var values = new ManagedList();
                foreach (var name in names)
                {
                    values.Add(new RuntimeObjectReference(name.Trim()));
                }

                containerDef.PropertyValues.Add("Queues", values);
            }

            // Register the listener and fire event
            parserContext.Registry.RegisterObjectDefinition(containerObjectName, containerDef);
        }
    }
}
