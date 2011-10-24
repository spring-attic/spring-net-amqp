
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
using Spring.Util;

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

        protected override Type GetObjectType(XmlElement element)
        {
            return typeof(RabbitTemplate);
        }

        protected override bool ShouldGenerateId
        {
            get
            {
                return false;
            }
        }

        protected override bool ShouldGenerateIdAsFallback
        {
            get
            {
                return true;
            }
        }

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
