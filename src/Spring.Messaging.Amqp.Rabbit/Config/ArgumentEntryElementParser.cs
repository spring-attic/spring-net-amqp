using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Spring.Core.TypeConversion;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
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
    public class ArgumentEntryElementParser : ObjectsNamespaceParser
    {
        //TODO: after more of this core functionality is exposed in the Spring.Core ObjectDefintionParserHelper class (in SPRNET-2.0),
        // this Rabbit-Argument-specific Dictionary parser can be removed

        public IDictionary ParseArgumentsElement(XmlElement mapEle, ParserContext parserContext)
        {
            return ParseDictionaryElement(mapEle, string.Empty, parserContext);
        }

        public ManagedDictionary ConvertToManagedDictionary<TKey, TValue>(IDictionary dictionary)
        {
            var result = new ManagedDictionary();
            result.KeyTypeName = typeof (TKey).FullName;
            result.ValueTypeName = typeof(TValue).FullName;

            foreach (DictionaryEntry entry in dictionary)
            {
                result.Add(entry.Key, entry.Value);
            }

            return result;
        }

        public Dictionary<TKey, TValue> ConvertToTypedDictionary<TKey, TValue>(IDictionary dictionary)
        {
            var result = new Dictionary<TKey, TValue>();

            foreach (DictionaryEntry entry in dictionary)
            {
                result.Add((TKey)entry.Key, (TValue)entry.Value);
            }

            return result;
        }

        //have to override the SelectNodes method b/c the base class impl. hard-codes the SPRING namespace prefix if none is provided
        //  (and we need a special-case XPath expression too)
        protected override XmlNodeList SelectNodes(XmlElement element, string childElementName)
        {
            XmlNamespaceManager nsManager = new XmlNamespaceManager(new NameTable());
            nsManager.AddNamespace("objects", "http://www.springframework.net");
            return element.SelectNodes("objects:" + childElementName, nsManager);
        }

       
    }
}