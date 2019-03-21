// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NamespaceUtils.cs" company="The original author or authors.">
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
using System.Xml;
using Spring.Core;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// Namespace Utilities
    /// </summary>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class NamespaceUtils
    {
        private static readonly string BASE_PACKAGE = "Spring.Messaging.Amqp.Rabbit.Config";
        private static readonly string REF_ATTRIBUTE = "ref";
        private static readonly string METHOD_ATTRIBUTE = "method";
        private static readonly string ORDER = "order";

        /// <summary>Sets the value if attribute defined.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>True if successful, else false.</returns>
        public static bool SetValueIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName, string propertyName)
        {
            var attributeValue = element.GetAttribute(attributeName);
            if (!string.IsNullOrWhiteSpace(attributeValue))
            {
                builder.AddPropertyValue(propertyName, new TypedStringValue(attributeValue));
                return true;
            }

            return false;
        }

        /// <summary>Sets the value if attribute defined.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns>True if successful, else false.</returns>
        public static bool SetValueIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName) { return SetValueIfAttributeDefined(builder, element, attributeName, Conventions.AttributeNameToPropertyName(attributeName)); }

        /// <summary>Determines whether [is attribute defined] [the specified element].</summary>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool IsAttributeDefined(XmlElement element, string attributeName)
        {
            var value = element.GetAttribute(attributeName);
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>Adds the constructor arg value if attribute defined.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool AddConstructorArgValueIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName)
        {
            var value = element.GetAttribute(attributeName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder.AddConstructorArg(new TypedStringValue(value));
                return true;
            }

            return false;
        }

        /// <summary>Adds the constructor arg boolean value if attribute defined.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="defaultValue">if set to <c>true</c> [default value].</param>
        public static void AddConstructorArgBooleanValueIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName, bool defaultValue)
        {
            var value = element.GetAttribute(attributeName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder.AddConstructorArg(new TypedStringValue(value));
            }
            else
            {
                builder.AddConstructorArg(defaultValue);
            }
        }

        /// <summary>Adds the constructor arg ref if attribute defined.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool AddConstructorArgRefIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName)
        {
            var value = element.GetAttribute(attributeName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder.AddConstructorArgReference(value);
                return true;
            }

            return false;
        }

        /// <summary>Adds the constructor arg parent ref if attribute defined.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool AddConstructorArgParentRefIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName)
        {
            var value = element.GetAttribute(attributeName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                var child = ObjectDefinitionBuilder.GenericObjectDefinition();
                child.RawObjectDefinition.ParentName = value;
                builder.AddConstructorArg(child.ObjectDefinition);
                return true;
            }

            return false;
        }

        /// <summary>Sets the reference if attribute defined.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool SetReferenceIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName, string propertyName)
        {
            var attributeValue = element.GetAttribute(attributeName);
            if (!string.IsNullOrWhiteSpace(attributeValue))
            {
                builder.AddPropertyReference(propertyName, attributeValue);
                return true;
            }

            return false;
        }

        /// <summary>Sets the reference if attribute defined.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool SetReferenceIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName) { return SetReferenceIfAttributeDefined(builder, element, attributeName, Conventions.AttributeNameToPropertyName(attributeName)); }

        /// <summary>Creates the element description.</summary>
        /// <param name="element">The element.</param>
        /// <returns>The element description.</returns>
        public static string CreateElementDescription(XmlElement element)
        {
            var elementId = "'" + element.LocalName + "'";
            var id = element.GetAttribute("id");
            if (!string.IsNullOrWhiteSpace(id))
            {
                elementId += " with id='" + id + "'";
            }

            return elementId;
        }

        /// <summary>The parse inner object definition.</summary>
        /// <param name="element">The element.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <returns>The Spring.Objects.Factory.Config.IObjectDefinition.</returns>
        public static IObjectDefinition ParseInnerObjectDefinition(XmlElement element, ParserContext parserContext)
        {
            // parses out inner object definition for concrete implementation if defined
            var childElements = element.GetElementsByTagName("object");
            IObjectDefinition innerComponentDefinition = null;
            IConfigurableObjectDefinition inDef = null;

            if (childElements != null && childElements.Count == 1)
            {
                var objectElement = childElements[0] as XmlElement;

                // var odDelegate = parserContext.GetDelegate();
                var odHolder = parserContext.ParserHelper.ParseObjectDefinitionElement(objectElement);

                // odHolder = odDelegate.DecorateObjectDefinitionIfRequired(objectElement, odHolder);
                inDef = odHolder.ObjectDefinition as IConfigurableObjectDefinition;
                var objectName = ObjectDefinitionReaderUtils.GenerateObjectName(inDef, parserContext.Registry);

                // innerComponentDefinition = new ObjectComponentDefinition(inDef, objectName);
                parserContext.Registry.RegisterObjectDefinition(objectName, inDef);
            }

            var aRef = element.GetAttribute(REF_ATTRIBUTE);
            AssertUtils.IsTrue(
                !(!string.IsNullOrWhiteSpace(aRef) && inDef != null), 
                "Ambiguous definition. Inner object " + (inDef == null ? string.Empty : inDef.ObjectTypeName) + " declaration and \"ref\" " + aRef + " are not allowed together."
                );

            return inDef;
        }
    }
}
