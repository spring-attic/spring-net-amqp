// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeadersExchangeParser.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.Xml;
using Spring.Messaging.Amqp.Core;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A headers exchange parser.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class HeadersExchangeParser : AbstractExchangeParser
    {
        /// <summary>The get object type.</summary>
        /// <param name="element">The element.</param>
        /// <returns>The System.Type.</returns>
        protected override Type GetObjectType(XmlElement element) { return typeof(HeadersExchange); }

        /// <summary>The parse binding.</summary>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="binding">The binding.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <returns>The Spring.Objects.Factory.Support.AbstractObjectDefinition.</returns>
        protected override AbstractObjectDefinition ParseBinding(string exchangeName, XmlElement binding, ParserContext parserContext)
        {
            var builder = ObjectDefinitionBuilder.GenericObjectDefinition(typeof(BindingFactoryObject));
            this.ParseDestination(binding, parserContext, builder);
            builder.AddPropertyValue("Exchange", new TypedStringValue(exchangeName));
            var map = new Dictionary<string, object>();
            var key = binding.GetAttribute("key");
            var value = binding.GetAttribute("value");
            map.Add(key, value);
            builder.AddPropertyValue("Arguments", map);
            return builder.ObjectDefinition;
        }
    }
}
