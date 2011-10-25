
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

using Spring.Core;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// Namespace Utilities
    /// </summary>
    public class NamespaceUtils
    {
        static readonly string BASE_PACKAGE = "Spring.Messaging.Amqp.Rabbit.Config";
	    static readonly string REF_ATTRIBUTE = "ref";
	    static readonly string METHOD_ATTRIBUTE = "method";
	    static readonly string ORDER = "order";

        /// <summary>
        /// Sets the value if attribute defined.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>True if successful, else false.</returns>
        public static bool SetValueIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName, string propertyName)
        {
            var attributeValue = element.GetAttribute(attributeName);
            if (StringUtils.HasText(attributeValue))
            {
                builder.AddPropertyValue(propertyName, new TypedStringValue(attributeValue));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the value if attribute defined.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns>True if successful, else false.</returns>
        public static bool SetValueIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName)
        {
            return SetValueIfAttributeDefined(builder, element, attributeName, Conventions.AttributeNameToPropertyName(Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(attributeName)));
        }

        /// <summary>
        /// Determines whether [is attribute defined] [the specified element].
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool IsAttributeDefined(XmlElement element, string attributeName)
        {
            var value = element.GetAttribute(attributeName);
            return (StringUtils.HasText(value));
        }

        /// <summary>
        /// Adds the constructor arg value if attribute defined.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool AddConstructorArgValueIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName)
        {
            var value = element.GetAttribute(attributeName);
            if (StringUtils.HasText(value))
            {
                builder.AddConstructorArg(new TypedStringValue(value));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds the constructor arg boolean value if attribute defined.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="defaultValue">if set to <c>true</c> [default value].</param>
        public static void AddConstructorArgBooleanValueIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName, bool defaultValue)
        {
            var value = element.GetAttribute(attributeName);
            if (StringUtils.HasText(value))
            {
                builder.AddConstructorArg(new TypedStringValue(value));
            }
            else
            {
                builder.AddConstructorArg(defaultValue);
            }
        }

        /// <summary>
        /// Adds the constructor arg ref if attribute defined.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool AddConstructorArgRefIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName)
        {
            var value = element.GetAttribute(attributeName);
            if (StringUtils.HasText(value))
            {
                builder.AddConstructorArgReference(value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds the constructor arg parent ref if attribute defined.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool AddConstructorArgParentRefIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName)
        {
            var value = element.GetAttribute(attributeName);
            if (StringUtils.HasText(value))
            {
                var child = ObjectDefinitionBuilder.GenericObjectDefinition();
                child.RawObjectDefinition.ParentName = value;
                builder.AddConstructorArg(child.ObjectDefinition);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the reference if attribute defined.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool SetReferenceIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName, string propertyName)
        {
            var attributeValue = element.GetAttribute(attributeName);
            if (StringUtils.HasText(attributeValue))
            {
                builder.AddPropertyReference(propertyName, attributeValue);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the reference if attribute defined.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns><c>true</c> if [is attribute defined] [the specified element]; otherwise, <c>false</c>.</returns>
        public static bool SetReferenceIfAttributeDefined(ObjectDefinitionBuilder builder, XmlElement element, string attributeName)
        {
            return SetReferenceIfAttributeDefined(builder, element, attributeName,Conventions.AttributeNameToPropertyName(attributeName));
        }

        /// <summary>
        /// Creates the element description.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The element description.</returns>
        public static string CreateElementDescription(XmlElement element)
        {
            var elementId = "'" + element.LocalName + "'";
            var id = element.GetAttribute("id");
            if (StringUtils.HasText(id))
            {
                elementId += " with id='" + id + "'";
            }
            return elementId;
        }

        public static IObjectDefinition ParseInnerBeanDefinition(XmlElement element, ParserContext parserContext) 
        {
		    // parses out inner bean definition for concrete implementation if defined
		    var childElements = element.GetElementsByTagName("object");

            // IObjectDefinition innerComponentDefinition = null;
		    if (childElements != null && childElements.Count == 1)
		    {
		        var beanElement = childElements[0] as XmlElement;
		        var bdHolder = parserContext.ParserHelper.ParseObjectDefinitionElement(beanElement);

		        // ObjectDefinitionParserDelegate delegate = parserContext.getDelegate();
		        // BeanDefinitionHolder bdHolder = delegate.parseBeanDefinitionElement(beanElement);
		        // bdHolder = delegate.decorateBeanDefinitionIfRequired(beanElement, bdHolder);
		        var inDef = bdHolder.ObjectDefinition as IConfigurableObjectDefinition;
		        var beanName = ObjectDefinitionReaderUtils.GenerateObjectName(inDef, parserContext.Registry);
		        parserContext.Registry.RegisterObjectDefinition(beanName, inDef);
		        return inDef;
		    }

            return null;
            /* var aRef = element.GetAttribute(REF_ATTRIBUTE);
		    AssertUtils.IsTrue(!(StringUtils.HasText(aRef) && innerComponentDefinition != null), "Ambiguous definition. Inner object " + (innerComponentDefinition == null ? innerComponentDefinition : innerComponentDefinition.ObjectDefinition.getBeanClassName()) + " declaration and \"ref\" " + aRef + " are not allowed together.");
		    return innerComponentDefinition; */
        }
    }
}
