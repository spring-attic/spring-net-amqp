
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Spring.Messaging.Amqp.Core;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A admin parser.
    /// </summary>
    public class AdminParser : AbstractSingleObjectDefinitionParser
    {
        private static readonly string CONNECTION_FACTORY_ATTRIBUTE = "connection-factory";

	    private static readonly string AUTO_STARTUP_ATTRIBUTE = "auto-startup";

        protected override string GetObjectTypeName(XmlElement element)
        {
            return "Spring.Messaging.Amqp.Rabbit.Core.RabbitAdmin";
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

            // At least one of 'templateRef' or 'connectionFactoryRef' attribute must be set.
            if (!StringUtils.HasText(connectionFactoryRef))
            {
                parserContext.ReaderContext.ReportFatalException(element, "A '" + CONNECTION_FACTORY_ATTRIBUTE + "' attribute must be set.");
            }

            if (StringUtils.HasText(connectionFactoryRef))
            {
                // Use constructor with connectionFactory parameter
                builder.AddConstructorArgReference(connectionFactoryRef);
            }

            String attributeValue;
            attributeValue = element.GetAttribute(AUTO_STARTUP_ATTRIBUTE);
            if (StringUtils.HasText(attributeValue))
            {
                builder.AddPropertyValue("AutoStartup", attributeValue);
            }
        }
    }
}
