
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Config
{

    /// <summary>
    /// An abstract exchange parser
    /// </summary>
    public abstract class AbstractExchangeParser : AbstractSingleObjectDefinitionParser
    {
        private static readonly string ARGUMENTS_ELEMENT = "exchange-arguments";

	    private static readonly string DURABLE_ATTRIBUTE = "durable";

	    private static readonly string AUTO_DELETE_ATTRIBUTE = "auto-delete";

	    private static string BINDINGS_ELE = "bindings";

	    private static string BINDING_ELE = "binding";

	    protected static readonly string BINDING_QUEUE_ATTR = "queue";

        protected override bool ShouldGenerateIdAsFallback
        {
            get
            {
                return true;
            }
        }

        protected override void DoParse(XmlElement element, ParserContext parserContext, ObjectDefinitionBuilder builder) 
        {
		    var exchangeName = element.GetAttribute("name");
		    builder.AddConstructorArg(new TypedStringValue(exchangeName));
		    var bindingsElements = element.GetElementsByTagName(BINDINGS_ELE);

            var bindingsElement = (bindingsElements != null && bindingsElements.Count == 1) ? bindingsElements[0] as XmlElement : null;
		    if (bindingsElement != null)
		    {
		        var bindings = bindingsElement.GetElementsByTagName(BINDING_ELE);
			    foreach (var binding in bindingsElement) 
                {
				    var beanDefinition = ParseBinding(exchangeName, binding as XmlElement, parserContext);
				    RegisterObjectDefinition(new ObjectDefinitionHolder(beanDefinition, parserContext.ReaderContext.GenerateObjectName(beanDefinition)), parserContext.Registry);
			    }
		    }

		    NamespaceUtils.AddConstructorArgBooleanValueIfAttributeDefined(builder, element, DURABLE_ATTRIBUTE, true);
		    NamespaceUtils.AddConstructorArgBooleanValueIfAttributeDefined(builder, element, AUTO_DELETE_ATTRIBUTE,false);

		    var argumentsElements = element.GetElementsByTagName(ARGUMENTS_ELEMENT);
            var argumentsElement = argumentsElements != null && argumentsElements.Count == 1 ? argumentsElements[0] as XmlElement : null;
		    if (argumentsElement != null) 
            {
                try
                {
                    var map = parserContext.ParserHelper.ParseCustomElement(argumentsElement, builder.RawObjectDefinition);
                    builder.AddConstructorArg(map);
                }
                catch (Exception e)
                {
                    throw;
                }
                
		    }

	    }

        protected abstract AbstractObjectDefinition ParseBinding(String exchangeName, XmlElement binding, ParserContext parserContext);
    }
}
