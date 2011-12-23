using System.Collections;
using System.Xml;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// Parses &lt;map&gt; and &lt;entry&gt; elements into a dictonary.
    /// <remarks>
    /// This class is only required because general parser functionality hasn't (yet) been exposed in the Spring.Core.ObjectDefinitionParserHelper
    /// class.  Once this is complete (SPRNET-2.0) this class can be eliminated and the parsing responsibility can be returned to the Spring.Core Helper class.
    /// </remarks>
    /// </summary>
    public class MapEntryElementParser : ObjectsNamespaceParser
    {
        //TODO: after more of this core functionality is exposed in the Spring.Core ObjectDefintionParserHelper class (in SPRNET-2.0),
        // this Rabbit-Argument-specific Dictionary parser can be removed

        public IDictionary ParseArgumentsElement(XmlElement mapEle, ParserContext parserContext)
        {
            return ParseDictionaryElement(mapEle, string.Empty, parserContext);
        }

        //have to override the SelectNodes method b/c the base class impl. hard-codes the SPRING namespace prefix if none is provided
        //  (and we need a special-case XPath expression too)
        protected override XmlNodeList SelectNodes(XmlElement element, string childElementName)
        {
            XmlNamespaceManager nsManager = new XmlNamespaceManager(new NameTable());
            nsManager.AddNamespace("rabbit", element.NamespaceURI);
            return element.SelectNodes("descendant::rabbit" + ":" + childElementName, nsManager);
        }
    }
}