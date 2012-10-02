// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArgumentEntryElementParser.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Xml;
#endregion

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
        // TODO: after more of this core functionality is exposed in the Spring.Core ObjectDefintionParserHelper class (in SPRNET-2.0),
        // this Rabbit-Argument-specific Dictionary parser can be removed

        /// <summary>The parse arguments element.</summary>
        /// <param name="mapEle">The map ele.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <returns>The System.Collections.IDictionary.</returns>
        public IDictionary ParseArgumentsElement(XmlElement mapEle, ParserContext parserContext) { return this.ParseDictionaryElement(mapEle, string.Empty, parserContext); }

        /// <summary>The convert to managed dictionary.</summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns>The Spring.Objects.Factory.Config.ManagedDictionary.</returns>
        public ManagedDictionary ConvertToManagedDictionary<TKey, TValue>(IDictionary dictionary)
        {
            var result = new ManagedDictionary();
            result.KeyTypeName = typeof(TKey).FullName;
            result.ValueTypeName = typeof(TValue).FullName;

            foreach (DictionaryEntry entry in dictionary)
            {
                result.Add(entry.Key, entry.Value);
            }

            return result;
        }

        /// <summary>The convert to typed dictionary.</summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns>The System.Collections.Generic.Dictionary`2[TKey -&gt; TKey, TValue -&gt; TValue].</returns>
        public Dictionary<TKey, TValue> ConvertToTypedDictionary<TKey, TValue>(IDictionary dictionary)
        {
            var result = new Dictionary<TKey, TValue>();

            foreach (DictionaryEntry entry in dictionary)
            {
                result.Add((TKey)entry.Key, (TValue)entry.Value);
            }

            return result;
        }

        // have to override the SelectNodes method b/c the base class impl. hard-codes the SPRING namespace prefix if none is provided
        // (and we need a special-case XPath expression too)
        /// <summary>The select nodes.</summary>
        /// <param name="element">The element.</param>
        /// <param name="childElementName">The child element name.</param>
        /// <returns>The System.Xml.XmlNodeList.</returns>
        protected override XmlNodeList SelectNodes(XmlElement element, string childElementName)
        {
            var nsManager = new XmlNamespaceManager(new NameTable());
            nsManager.AddNamespace("objects", "http://www.springframework.net");
            return element.SelectNodes("objects:" + childElementName, nsManager);
        }
    }
}
