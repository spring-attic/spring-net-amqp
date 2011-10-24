
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Spring.Messaging.Amqp.Core;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A topic exchange parser.
    /// </summary>
    public class TopicExchangeParser : AbstractExchangeParser
    {
        private static readonly string BINDING_PATTERN_ATTR = "pattern";

        protected override Type GetObjectType(XmlElement element)
        {
            return typeof(TopicExchange);
        }

        protected override AbstractObjectDefinition ParseBinding(string exchangeName, XmlElement binding, ParserContext parserContext)
        {
            var builder = ObjectDefinitionBuilder.GenericObjectDefinition(typeof(BindingFactoryObject));
            builder.AddPropertyReference("DestinationQueue", binding.GetAttribute(BINDING_QUEUE_ATTR));
            builder.AddPropertyValue("Exchange", new TypedStringValue(exchangeName));

            builder.AddPropertyValue("RoutingKey", new TypedStringValue(binding.GetAttribute(BINDING_PATTERN_ATTR)));
		    builder.AddPropertyValue("Arguments", new Hashtable());

            return builder.ObjectDefinition;
        }
    }
}
