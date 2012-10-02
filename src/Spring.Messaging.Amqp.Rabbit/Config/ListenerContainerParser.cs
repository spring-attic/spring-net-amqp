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
using System;
using System.Xml;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
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
    public class ListenerContainerParser : IObjectDefinitionParser
    {
        private static readonly string CONNECTION_FACTORY_ATTRIBUTE = "connection-factory";

        private static readonly string TASK_EXECUTOR_ATTRIBUTE = "task-executor";

        private static readonly string ERROR_HANDLER_ATTRIBUTE = "error-handler";

        private static readonly string LISTENER_ELEMENT = "listener";

        private static readonly string ID_ATTRIBUTE = "id";

        private static readonly string QUEUE_NAMES_ATTRIBUTE = "queue-names";

        private static readonly string QUEUES_ATTRIBUTE = "queues";

        private static readonly string REF_ATTRIBUTE = "ref";

        private static readonly string METHOD_ATTRIBUTE = "method";

        private static readonly string MESSAGE_CONVERTER_ATTRIBUTE = "message-converter";

        private static readonly string RESPONSE_EXCHANGE_ATTRIBUTE = "response-exchange";

        private static readonly string RESPONSE_ROUTING_KEY_ATTRIBUTE = "response-routing-key";

        private static readonly string ACKNOWLEDGE_ATTRIBUTE = "acknowledge";

        private static readonly string ACKNOWLEDGE_AUTO = "auto";

        private static readonly string ACKNOWLEDGE_MANUAL = "manual";

        private static readonly string ACKNOWLEDGE_NONE = "none";

        private static readonly string TRANSACTION_MANAGER_ATTRIBUTE = "transaction-manager";

        private static readonly string CONCURRENCY_ATTRIBUTE = "concurrency";

        private static readonly string PREFETCH_ATTRIBUTE = "prefetch";

        private static readonly string TRANSACTION_SIZE_ATTRIBUTE = "transaction-size";

        private static readonly string PHASE_ATTRIBUTE = "phase";

        private static readonly string AUTO_STARTUP_ATTRIBUTE = "auto-startup";

        private static readonly string ADVICE_CHAIN_ATTRIBUTE = "advice-chain";

        /// <summary>The parse element.</summary>
        /// <param name="element">The element.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <returns>The Spring.Objects.Factory.Config.IObjectDefinition.</returns>
        public IObjectDefinition ParseElement(XmlElement element, ParserContext parserContext)
        {
            // var compositeDef = new GenericObjectDefinition(); //parserContext.ParserHelper.ParseCustomElement(element), element.LocalName);
            // parserContext.ContainingObjectDefinition = parserContext.ParserHelper.pushContainingComponent(compositeDef);
            var childNodes = element.ChildNodes;
            for (int i = 0; i < childNodes.Count; i++)
            {
                var child = childNodes[i];
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

            // listenerDef.ResourceDescription = parserContext.setSource(parserContext.extractSource(listenerEle));
            string aRef = listenerEle.GetAttribute(REF_ATTRIBUTE);
            if (!StringUtils.HasText(aRef))
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
                if (!StringUtils.HasText(method))
                {
                    parserContext.ReaderContext
                        .ReportFatalException(listenerEle, "Listener 'method' attribute contains empty value.");
                }
            }

            listenerDef.PropertyValues.Add("DefaultListenerMethod", method);

            if (containerEle.HasAttribute(MESSAGE_CONVERTER_ATTRIBUTE))
            {
                string messageConverter = containerEle.GetAttribute(MESSAGE_CONVERTER_ATTRIBUTE);
                if (!StringUtils.HasText(messageConverter))
                {
                    parserContext.ReaderContext.ReportFatalException(containerEle, "Listener container 'message-converter' attribute contains empty value.");
                }
                else
                {
                    listenerDef.PropertyValues.Add("MessageConverter", new RuntimeObjectReference(messageConverter));
                }
            }

            var containerDef = this.ParseContainer(listenerEle, containerEle, parserContext);

            if (listenerEle.HasAttribute(RESPONSE_EXCHANGE_ATTRIBUTE))
            {
                string responseExchange = listenerEle.GetAttribute(RESPONSE_EXCHANGE_ATTRIBUTE);
                listenerDef.PropertyValues.Add("ResponseExchange", responseExchange);
            }

            if (listenerEle.HasAttribute(RESPONSE_ROUTING_KEY_ATTRIBUTE))
            {
                string responseRoutingKey = listenerEle.GetAttribute(RESPONSE_ROUTING_KEY_ATTRIBUTE);
                listenerDef.PropertyValues.Add("ResponseRoutingKey", responseRoutingKey);
            }

            listenerDef.ObjectTypeName = "Spring.Messaging.Amqp.Rabbit.Listener.Adapter.MessageListenerAdapter";
            containerDef.PropertyValues.Add("MessageListener", listenerDef);

            var containerObjectName = containerEle.GetAttribute(ID_ATTRIBUTE);

            // If no object id is given auto generate one using the ReaderContext's ObjectNameGenerator
            if (!StringUtils.HasText(containerObjectName))
            {
                containerObjectName = parserContext.ReaderContext.GenerateObjectName(containerDef);
            }

            if (!NamespaceUtils.IsAttributeDefined(listenerEle, QUEUE_NAMES_ATTRIBUTE)
                && !NamespaceUtils.IsAttributeDefined(listenerEle, QUEUES_ATTRIBUTE))
            {
                parserContext.ReaderContext.ReportFatalException(listenerEle, "Listener 'queue-names' or 'queues' attribute must be provided.");
            }

            if (NamespaceUtils.IsAttributeDefined(listenerEle, QUEUE_NAMES_ATTRIBUTE)
                && NamespaceUtils.IsAttributeDefined(listenerEle, QUEUES_ATTRIBUTE))
            {
                parserContext.ReaderContext.ReportFatalException(listenerEle, "Listener 'queue-names' or 'queues' attribute must be provided but not both.");
            }

            string queueNames = listenerEle.GetAttribute(QUEUE_NAMES_ATTRIBUTE);
            if (StringUtils.HasText(queueNames))
            {
                var names = StringUtils.CommaDelimitedListToStringArray(queueNames);
                var values = new ManagedList();
                for (int i = 0; i < names.Length; i++)
                {
                    values.Add(new TypedStringValue(names[i].Trim()));
                }

                containerDef.PropertyValues.Add("QueueNames", values);
            }

            var queues = listenerEle.GetAttribute(QUEUES_ATTRIBUTE);
            if (StringUtils.HasText(queues))
            {
                var names = StringUtils.CommaDelimitedListToStringArray(queues);
                var values = new ManagedList();
                foreach (var t in names)
                {
                    values.Add(new RuntimeObjectReference(t.Trim()));
                }

                containerDef.PropertyValues.Add("Queues", values);
            }

            // Register the listener and fire event
            parserContext.Registry.RegisterObjectDefinition(containerObjectName, containerDef);
        }

        private IObjectDefinition ParseContainer(XmlElement listenerEle, XmlElement containerEle, ParserContext parserContext)
        {
            var containerDef = new RootObjectDefinition(typeof(SimpleMessageListenerContainer));

            // TODO? containerDef.(parserContext.ParserHelper.(containerEle));
            var connectionFactoryObjectName = "RabbitConnectionFactory";
            if (containerEle.HasAttribute(CONNECTION_FACTORY_ATTRIBUTE))
            {
                connectionFactoryObjectName = containerEle.GetAttribute(CONNECTION_FACTORY_ATTRIBUTE);
                if (!StringUtils.HasText(connectionFactoryObjectName))
                {
                    parserContext.ReaderContext.ReportFatalException(
                        containerEle, 
                        "Listener container 'connection-factory' attribute contains empty value.");
                }
            }

            if (StringUtils.HasText(connectionFactoryObjectName))
            {
                containerDef.PropertyValues.Add(
                    "ConnectionFactory", 
                    new RuntimeObjectReference(connectionFactoryObjectName));
            }

            string taskExecutorObjectName = containerEle.GetAttribute(TASK_EXECUTOR_ATTRIBUTE);
            if (StringUtils.HasText(taskExecutorObjectName))
            {
                containerDef.PropertyValues.Add("TaskExecutor", new RuntimeObjectReference(taskExecutorObjectName));
            }

            string errorHandlerObjectName = containerEle.GetAttribute(ERROR_HANDLER_ATTRIBUTE);
            if (StringUtils.HasText(errorHandlerObjectName))
            {
                containerDef.PropertyValues.Add("ErrorHandler", new RuntimeObjectReference(errorHandlerObjectName));
            }

            AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode = this.ParseAcknowledgeMode(containerEle, parserContext);
            if (acknowledgeMode != null)
            {
                containerDef.PropertyValues.Add("AcknowledgeMode", acknowledgeMode);
            }

            string transactionManagerObjectName = containerEle.GetAttribute(TRANSACTION_MANAGER_ATTRIBUTE);
            if (StringUtils.HasText(transactionManagerObjectName))
            {
                containerDef.PropertyValues.Add("TransactionManager", new RuntimeObjectReference(transactionManagerObjectName));
            }

            string concurrency = containerEle.GetAttribute(CONCURRENCY_ATTRIBUTE);
            if (StringUtils.HasText(concurrency))
            {
                containerDef.PropertyValues.Add("ConcurrentConsumers", new TypedStringValue(concurrency));
            }

            string prefetch = containerEle.GetAttribute(PREFETCH_ATTRIBUTE);
            if (StringUtils.HasText(prefetch))
            {
                containerDef.PropertyValues.Add("PrefetchCount", new TypedStringValue(prefetch));
            }

            string transactionSize = containerEle.GetAttribute(TRANSACTION_SIZE_ATTRIBUTE);
            if (StringUtils.HasText(transactionSize))
            {
                containerDef.PropertyValues.Add("TxSize", new TypedStringValue(transactionSize));
            }

            string phase = containerEle.GetAttribute(PHASE_ATTRIBUTE);
            if (StringUtils.HasText(phase))
            {
                containerDef.PropertyValues.Add("Phase", phase);
            }

            string autoStartup = containerEle.GetAttribute(AUTO_STARTUP_ATTRIBUTE);
            if (StringUtils.HasText(autoStartup))
            {
                containerDef.PropertyValues.Add("AutoStartup", new TypedStringValue(autoStartup));
            }

            string adviceChain = containerEle.GetAttribute(ADVICE_CHAIN_ATTRIBUTE);
            if (StringUtils.HasText(adviceChain))
            {
                containerDef.PropertyValues.Add("AdviceChain", new RuntimeObjectReference(adviceChain));
            }

            return containerDef;
        }

        private AcknowledgeModeUtils.AcknowledgeMode ParseAcknowledgeMode(XmlElement ele, ParserContext parserContext)
        {
            var acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
            string acknowledge = ele.GetAttribute(ACKNOWLEDGE_ATTRIBUTE);
            if (StringUtils.HasText(acknowledge))
            {
                if (ACKNOWLEDGE_AUTO.Equals(acknowledge))
                {
                    acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
                }
                else if (ACKNOWLEDGE_MANUAL.Equals(acknowledge))
                {
                    acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Manual;
                }
                else if (ACKNOWLEDGE_NONE.Equals(acknowledge))
                {
                    acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.None;
                }
                else
                {
                    parserContext.ReaderContext.ReportFatalException(
                        ele, 
                        "Invalid listener container 'acknowledge' setting [" + acknowledge
                        + "]: only \"auto\", \"manual\", and \"none\" supported.");
                }

                return acknowledgeMode;
            }
            else
            {
                return AcknowledgeModeUtils.AcknowledgeMode.Auto;
            }
        }
    }
}
