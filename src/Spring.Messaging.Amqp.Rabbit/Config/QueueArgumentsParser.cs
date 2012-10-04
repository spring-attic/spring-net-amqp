// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueueArgumentsParser.cs" company="The original author or authors.">
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
using System.Xml;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// Queue Arguments Parser
    /// </summary>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    internal class QueueArgumentsParser : AbstractSingleObjectDefinitionParser
    {
        /// <summary>The do parse.</summary>
        /// <param name="element">The element.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <param name="builder">The builder.</param>
        protected override void DoParse(XmlElement element, ParserContext parserContext, ObjectDefinitionBuilder builder)
        {
            var parser = new ObjectDefinitionParserHelper(parserContext);
            var map = parser.ParseMapElementToTypedDictionary(element, builder.RawObjectDefinition);

            builder.AddConstructorArg(map);
        }

        /// <summary>The get object type name.</summary>
        /// <param name="element">The element.</param>
        /// <returns>The System.String.</returns>
        protected override string GetObjectTypeName(XmlElement element) { return typeof(DictionaryFactoryObject).FullName; }
    }
}
