
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;

using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
using Spring.Util;

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

        public IObjectDefinition ParseElement(XmlElement element, ParserContext parserContext)
        {
            var compositeDef = new ObjectDefinitionHolder(parserContext.ParserHelper.ParseCustomElement(element), element.LocalName);
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
                        ParseListener((XmlElement)child, element, parserContext);
                    }
                }
            }

            parserContext.Registry.RegisterObjectDefinition(compositeDef.ObjectName, compositeDef.ObjectDefinition);
            return null;
        }

        private void ParseListener(XmlElement listenerEle, XmlElement containerEle, ParserContext parserContext) {
		var listenerDef = new RootObjectDefinition();
		// listenerDef.setSource(parserContext.extractSource(listenerEle));

		String aRef = listenerEle.GetAttribute(REF_ATTRIBUTE);
		if (!StringUtils.HasText(aRef)) {
			parserContext.ReaderContext.ReportFatalException(listenerEle, "Listener 'ref' attribute contains empty value.");
		} else {
			listenerDef.PropertyValues.Add("HandlerObject", new RuntimeObjectReference(aRef));
		}

		String method = null;
		if (listenerEle.HasAttribute(METHOD_ATTRIBUTE)) {
			method = listenerEle.GetAttribute(METHOD_ATTRIBUTE);
			if (!StringUtils.HasText(method)) {
				parserContext.ReaderContext
						.ReportFatalException(listenerEle, "Listener 'method' attribute contains empty value.");
			}
		}
		listenerDef.PropertyValues.Add("DefaultListenerMethod", method);

		if (containerEle.HasAttribute(MESSAGE_CONVERTER_ATTRIBUTE)) {
			String messageConverter = containerEle.GetAttribute(MESSAGE_CONVERTER_ATTRIBUTE);
			if (!StringUtils.HasText(messageConverter)) {
                parserContext.ReaderContext.ReportFatalException(containerEle, "Listener container 'message-converter' attribute contains empty value.");
			} else {
				listenerDef.PropertyValues.Add("MessageConverter", new RuntimeObjectReference(messageConverter));
			}
		}

		var containerDef = ParseContainer(listenerEle, containerEle, parserContext);

        if (listenerEle.HasAttribute(RESPONSE_EXCHANGE_ATTRIBUTE))
        {
            String responseExchange = listenerEle.GetAttribute(RESPONSE_EXCHANGE_ATTRIBUTE);
            listenerDef.PropertyValues.Add("ResponseExchange", responseExchange);
		}

        if (listenerEle.HasAttribute(RESPONSE_ROUTING_KEY_ATTRIBUTE))
        {
            String responseRoutingKey = listenerEle.GetAttribute(RESPONSE_ROUTING_KEY_ATTRIBUTE);
            listenerDef.PropertyValues.Add("ResponseRoutingKey", responseRoutingKey);
		}

		listenerDef.ObjectTypeName = "Spring.Messaging.Amqp.Rabbit.Listener.Adapter.MessageListenerAdapter";
        containerDef.PropertyValues.Add("MessageListener", listenerDef);

        String containerBeanName = containerEle.GetAttribute(ID_ATTRIBUTE);
		// If no bean id is given auto generate one using the ReaderContext's BeanNameGenerator
        if (!StringUtils.HasText(containerBeanName))
        {
			containerBeanName = parserContext.ReaderContext.GenerateObjectName(containerDef);
		}

		if (!NamespaceUtils.IsAttributeDefined(listenerEle, QUEUE_NAMES_ATTRIBUTE)
				&& !NamespaceUtils.IsAttributeDefined(listenerEle, QUEUES_ATTRIBUTE)) {
			parserContext.ReaderContext.ReportFatalException(listenerEle, "Listener 'queue-names' or 'queues' attribute must be provided.");
		}
		if (NamespaceUtils.IsAttributeDefined(listenerEle, QUEUE_NAMES_ATTRIBUTE)
				&& NamespaceUtils.IsAttributeDefined(listenerEle, QUEUES_ATTRIBUTE)) {
			parserContext.ReaderContext.ReportFatalException(listenerEle, "Listener 'queue-names' or 'queues' attribute must be provided but not both.");
		}

        String queueNames = listenerEle.GetAttribute(QUEUE_NAMES_ATTRIBUTE);
        if (StringUtils.HasText(queueNames))
        {
			var names = StringUtils.CommaDelimitedListToStringArray(queueNames);
			var values = new ManagedList();
			for (int i = 0; i < names.Length; i++) {
				values.Add(new TypedStringValue(names[i].Trim()));
			}
			containerDef.PropertyValues.Add("QueueNames", values);
		}
		var queues = listenerEle.GetAttribute(QUEUES_ATTRIBUTE);
		if (StringUtils.HasText(queues)) {
			var names = StringUtils.CommaDelimitedListToStringArray(queues);
			var values = new ManagedList();
			for (int i = 0; i < names.Length; i++) {
				values.Add(new RuntimeObjectReference(names[i].Trim()));
			}
			containerDef.PropertyValues.Add("Queues", values);
		}

		// Register the listener and fire event
		parserContext.Registry.RegisterObjectDefinition(containerBeanName, containerDef);
	}

        private IObjectDefinition ParseContainer(XmlElement listenerEle, XmlElement containerEle, ParserContext parserContext) {
		var containerDef = new RootObjectDefinition(typeof(SimpleMessageListenerContainer));
		// TODO? containerDef.(parserContext.ParserHelper.(containerEle));

		var connectionFactoryBeanName = "RabbitConnectionFactory";
		if (containerEle.HasAttribute(CONNECTION_FACTORY_ATTRIBUTE)) {
            connectionFactoryBeanName = containerEle.GetAttribute(CONNECTION_FACTORY_ATTRIBUTE);
			if (!StringUtils.HasText(connectionFactoryBeanName)) {
				parserContext.ReaderContext.ReportFatalException(containerEle,
						"Listener container 'connection-factory' attribute contains empty value.");
			}
		}
		if (StringUtils.HasText(connectionFactoryBeanName)) {
            containerDef.PropertyValues.Add("ConnectionFactory",
					new RuntimeObjectReference(connectionFactoryBeanName));
		}

        String taskExecutorBeanName = containerEle.GetAttribute(TASK_EXECUTOR_ATTRIBUTE);
        if (StringUtils.HasText(taskExecutorBeanName))
        {
            containerDef.PropertyValues.Add("TaskExecutor", new RuntimeObjectReference(taskExecutorBeanName));
		}

        String errorHandlerBeanName = containerEle.GetAttribute(ERROR_HANDLER_ATTRIBUTE);
        if (StringUtils.HasText(errorHandlerBeanName))
        {
            containerDef.PropertyValues.Add("ErrorHandler", new RuntimeObjectReference(errorHandlerBeanName));
		}

		AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode = ParseAcknowledgeMode(containerEle, parserContext);
		if (acknowledgeMode != null) 
        {
            containerDef.PropertyValues.Add("AcknowledgeMode", acknowledgeMode);
		}

		String transactionManagerBeanName = containerEle.GetAttribute(TRANSACTION_MANAGER_ATTRIBUTE);
        if (StringUtils.HasText(transactionManagerBeanName))
        {
            containerDef.PropertyValues.Add("TransactionManager", new RuntimeObjectReference(transactionManagerBeanName));
		}

        String concurrency = containerEle.GetAttribute(CONCURRENCY_ATTRIBUTE);
        if (StringUtils.HasText(concurrency))
        {
            containerDef.PropertyValues.Add("ConcurrentConsumers", new TypedStringValue(concurrency));
		}

        String prefetch = containerEle.GetAttribute(PREFETCH_ATTRIBUTE);
        if (StringUtils.HasText(prefetch))
        {
            containerDef.PropertyValues.Add("PrefetchCount", new TypedStringValue(prefetch));
		}

        String transactionSize = containerEle.GetAttribute(TRANSACTION_SIZE_ATTRIBUTE);
        if (StringUtils.HasText(transactionSize))
        {
            containerDef.PropertyValues.Add("TxSize", new TypedStringValue(transactionSize));
		}

        String phase = containerEle.GetAttribute(PHASE_ATTRIBUTE);
        if (StringUtils.HasText(phase))
        {
            containerDef.PropertyValues.Add("Phase", phase);
		}

        String autoStartup = containerEle.GetAttribute(AUTO_STARTUP_ATTRIBUTE);
        if (StringUtils.HasText(autoStartup))
        {
            containerDef.PropertyValues.Add("AutoStartup", new TypedStringValue(autoStartup));
		}

		String adviceChain = containerEle.GetAttribute(ADVICE_CHAIN_ATTRIBUTE);
        if (StringUtils.HasText(adviceChain))
        {
            containerDef.PropertyValues.Add("AdviceChain", new RuntimeObjectReference(adviceChain));
		}

		return containerDef;
	}

        private AcknowledgeModeUtils.AcknowledgeMode ParseAcknowledgeMode(XmlElement ele, ParserContext parserContext)
        {
            AcknowledgeModeUtils.AcknowledgeMode acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.AUTO;
            String acknowledge = ele.GetAttribute(ACKNOWLEDGE_ATTRIBUTE);
            if (StringUtils.HasText(acknowledge))
            {
                if (ACKNOWLEDGE_AUTO.Equals(acknowledge))
                {
                    acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.AUTO;
                }
                else if (ACKNOWLEDGE_MANUAL.Equals(acknowledge))
                {
                    acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.MANUAL;
                }
                else if (ACKNOWLEDGE_NONE.Equals(acknowledge))
                {
                    acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.NONE;
                }
                else
                {
                    parserContext.ReaderContext.ReportFatalException(ele,
                            "Invalid listener container 'acknowledge' setting [" + acknowledge
                                    + "]: only \"auto\", \"manual\", and \"none\" supported.");
                }
                return acknowledgeMode;
            }
            else
            {
                return AcknowledgeModeUtils.AcknowledgeMode.AUTO;
            }
        }
    }
}
