
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
    /// A headers exchange parser.
    /// </summary>
    public class HeadersExchangeParser : AbstractExchangeParser
    {
        protected override Type GetObjectType(XmlElement element)
        {
            return typeof(HeadersExchange);
        }

        protected override AbstractObjectDefinition ParseBinding(string exchangeName, XmlElement binding, ParserContext parserContext)
        {
            var builder = ObjectDefinitionBuilder.GenericObjectDefinition(typeof(BindingFactoryObject));
            builder.AddPropertyReference("DestinationQueue", binding.GetAttribute(BINDING_QUEUE_ATTR));
            builder.AddPropertyValue("Exchange", new TypedStringValue(exchangeName));
            
            var map = new Hashtable();
            var key = binding.GetAttribute("key");
            var value = binding.GetAttribute("value");
            map.Add(key, value);

            builder.AddPropertyValue("Arguments", map);

            return builder.ObjectDefinition;
        }
    }
}
