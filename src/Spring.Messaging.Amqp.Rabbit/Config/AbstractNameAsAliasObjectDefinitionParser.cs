using System.Xml;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    public class AbstractNameAsAliasObjectDefinitionParser : AbstractSingleObjectDefinitionParser
    {
        public override IObjectDefinition ParseElement(XmlElement element, ParserContext parserContext)
        {
            AbstractObjectDefinition definition = this.ParseInternal(element, parserContext);

            if (!parserContext.IsNested)
            {
                string id = null;
                try
                {
                    id = this.ResolveId(element, definition, parserContext);
                    if (!StringUtils.HasText(id))
                    {
                        parserContext.ReaderContext.ReportException(element, "null",
                                                                    "Id is required for element '" + element.LocalName + "' when used as a top-level tag", null);
                    }

                    string[] name = new string[0];

                    if (NamespaceUtils.IsAttributeDefined(element, "name"))
                    {
                        name = new[] { GetAttributeValue(element, "name") };
                    }

                    ObjectDefinitionHolder holder;

                    if (name.Length == 0)
                    {
                        holder = new ObjectDefinitionHolder(definition, id);
                    }
                    else
                    {
                        holder = new ObjectDefinitionHolder(definition, id, name);
                    }


                    this.RegisterObjectDefinition(holder, parserContext.Registry);
                }
                catch (ObjectDefinitionStoreException ex)
                {
                    parserContext.ReaderContext.ReportException(element, id, ex.Message);
                    return null;
                }
            }
            return definition;
        }
    }
}