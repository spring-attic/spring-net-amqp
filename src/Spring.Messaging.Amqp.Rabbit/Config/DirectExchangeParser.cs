
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
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A direct exchange parser.
    /// </summary>
    public class DirectExchangeParser : AbstractExchangeParser
    {
        private static readonly String BINDING_KEY_ATTR = "key";

	    protected override Type GetObjectType(XmlElement element) 
        {
		    return typeof(DirectExchange);
	    }
        
        protected override AbstractObjectDefinition ParseBinding(string exchangeName, XmlElement binding, ParserContext parserContext)
        {
            var builder = ObjectDefinitionBuilder.GenericObjectDefinition(typeof(BindingFactoryObject));
		    builder.AddPropertyReference("DestinationQueue", binding.GetAttribute(BINDING_QUEUE_ATTR));
		    builder.AddPropertyValue("Exchange", new TypedStringValue(exchangeName));
		    String bindingKey = binding.GetAttribute(BINDING_KEY_ATTR);
		    if (!StringUtils.HasText(bindingKey)) {
			    bindingKey = "";
		    }
		    builder.AddPropertyValue("RoutingKey", new TypedStringValue(bindingKey));
		    builder.AddPropertyValue("Arguments", new Hashtable());
		    return builder.ObjectDefinition;
        }
    }
}
