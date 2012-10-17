// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectDefinitionParserHelper.cs" company="The original author or authors.">
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Spring.Collections;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// Parses &lt;map&gt; and &lt;entry&gt; elements into a dictonary.
    /// <remarks>
    /// This class is only required because the necessary general parser functionality hasn't (yet) been exposed in the
    /// existing <c ref="Spring.Objects.Factory.Xml.ObjectDefinitionParserHelper"/> class.  Once this is complete (planned for SPRNET-2.0) this
    /// class can be eliminated and the parsing responsibility can be returned to the Helper class in Spring.Core.  Note that most methods
    /// in this impl. are artificially "private" and that the entire class is artificially "internal" so that it doesn't bleed out into
    /// any consuming code and is only able to offer a very narrow set of services specifically in support of only this project.
    /// </remarks>
    /// </summary>
    internal class ObjectDefinitionParserHelper
    {
        private readonly ParserContext parserContext;

        public static string OBJECTS_NAMESPACE_URI = "http://www.springframework.net";

        public static string TRUE_VALUE = "true";

        public static string DEFAULT_VALUE = "default";

        public static string DESCRIPTION_ELEMENT = "description";

        public static string AUTOWIRE_BY_NAME_VALUE = "byName";

        public static string AUTOWIRE_BY_TYPE_VALUE = "byType";

        public static string AUTOWIRE_CONSTRUCTOR_VALUE = "constructor";

        public static string AUTOWIRE_AUTODETECT_VALUE = "autodetect";

        public static string DEPENDENCY_CHECK_ALL_ATTRIBUTE_VALUE = "all";

        public static string DEPENDENCY_CHECK_SIMPLE_ATTRIBUTE_VALUE = "simple";

        public static string DEPENDENCY_CHECK_OBJECTS_ATTRIBUTE_VALUE = "objects";

        public static string NAME_ATTRIBUTE = "name";

        public static string OBJECT_ELEMENT = "object";

        public static string META_ELEMENT = "meta";

        public static string ID_ATTRIBUTE = "id";

        public static string PARENT_ATTRIBUTE = "parent";

        public static string CLASS_ATTRIBUTE = "class";

        public static string ABSTRACT_ATTRIBUTE = "abstract";

        public static string SCOPE_ATTRIBUTE = "scope";

        public static string SINGLETON_ATTRIBUTE = "singleton";

        public static string LAZY_INIT_ATTRIBUTE = "lazy-init";

        public static string AUTOWIRE_ATTRIBUTE = "autowire";

        public static string AUTOWIRE_CANDIDATE_ATTRIBUTE = "autowire-candidate";

        public static string PRIMARY_ATTRIBUTE = "primary";

        public static string DEPENDENCY_CHECK_ATTRIBUTE = "dependency-check";

        public static string DEPENDS_ON_ATTRIBUTE = "depends-on";

        public static string INIT_METHOD_ATTRIBUTE = "init-method";

        public static string DESTROY_METHOD_ATTRIBUTE = "destroy-method";

        public static string FACTORY_METHOD_ATTRIBUTE = "factory-method";

        public static string FACTORY_OBJECT_ATTRIBUTE = "factory-object";

        public static string CONSTRUCTOR_ARG_ELEMENT = "constructor-arg";

        public static string INDEX_ATTRIBUTE = "index";

        public static string TYPE_ATTRIBUTE = "type";

        public static string VALUE_TYPE_ATTRIBUTE = "value-type";

        public static string KEY_TYPE_ATTRIBUTE = "key-type";

        public static string PROPERTY_ELEMENT = "property";

        public static string REF_ATTRIBUTE = "ref";

        public static string VALUE_ATTRIBUTE = "value";

        public static string LOOKUP_METHOD_ELEMENT = "lookup-method";

        public static string REPLACED_METHOD_ELEMENT = "replaced-method";

        public static string REPLACER_ATTRIBUTE = "replacer";

        public static string ARG_TYPE_ELEMENT = "arg-type";

        public static string ARG_TYPE_MATCH_ATTRIBUTE = "match";

        public static string REF_ELEMENT = "ref";

        public static string IDREF_ELEMENT = "idref";

        public static string OBJECT_REF_ATTRIBUTE = "object";

        public static string LOCAL_REF_ATTRIBUTE = "local";

        public static string PARENT_REF_ATTRIBUTE = "parent";

        public static string VALUE_ELEMENT = "value";

        public static string NULL_ELEMENT = "null";

        public static string ARRAY_ELEMENT = "array";

        public static string LIST_ELEMENT = "list";

        public static string SET_ELEMENT = "set";

        public static string MAP_ELEMENT = "map";

        public static string ENTRY_ELEMENT = "entry";

        public static string KEY_ELEMENT = "key";

        public static string KEY_ATTRIBUTE = "key";

        public static string KEY_REF_ATTRIBUTE = "key-ref";

        public static string VALUE_REF_ATTRIBUTE = "value-ref";

        public static string PROPS_ELEMENT = "props";

        public static string PROP_ELEMENT = "prop";

        public static string MERGE_ATTRIBUTE = "merge";

        public static string QUALIFIER_ELEMENT = "qualifier";

        public static string QUALIFIER_ATTRIBUTE_ELEMENT = "attribute";

        public static string DEFAULT_LAZY_INIT_ATTRIBUTE = "default-lazy-init";

        public static string DEFAULT_MERGE_ATTRIBUTE = "default-merge";

        public static string DEFAULT_AUTOWIRE_ATTRIBUTE = "default-autowire";

        public static string DEFAULT_DEPENDENCY_CHECK_ATTRIBUTE = "default-dependency-check";

        public static string DEFAULT_AUTOWIRE_CANDIDATES_ATTRIBUTE = "default-autowire-candidates";

        public static string DEFAULT_INIT_METHOD_ATTRIBUTE = "default-init-method";

        public static string DEFAULT_DESTROY_METHOD_ATTRIBUTE = "default-destroy-method";

        private readonly DocumentDefaultsDefinition defaults = new DocumentDefaultsDefinition();

        /// <summary>Initializes a new instance of the <see cref="ObjectDefinitionParserHelper"/> class.</summary>
        /// <param name="parserContext">The parser context.</param>
        public ObjectDefinitionParserHelper(ParserContext parserContext) { this.parserContext = parserContext; }

        /// <summary>Parse the merge attribute of a collection element, if any.</summary>
        /// <param name="collectionElement">element to parse</param>
        /// <returns>true if merge is enabled, else false</returns>
        private bool ParseMergeAttribute(XmlElement collectionElement)
        {
            string value = collectionElement.GetAttribute(MERGE_ATTRIBUTE);
            if (DEFAULT_VALUE.Equals(value))
            {
                value = this.defaults.Merge;
            }

            return TRUE_VALUE.Equals(value);
        }

        private string GetLocalName(XmlNode node) { return node.LocalName; }

        private bool NodeNameEquals(XmlNode node, string desiredName) { return desiredName.Equals(node.Name) || desiredName.Equals(this.GetLocalName(node)); }

        /// <summary>The error.</summary>
        /// <param name="message">The message.</param>
        /// <param name="source">The source.</param>
        protected void Error(string message, XmlNode source) { this.parserContext.ReaderContext.ReportException(source, source.Name, message); }

        /// <summary>The has attribute.</summary>
        /// <param name="node">The node.</param>
        /// <param name="match">The match.</param>
        /// <returns>The System.Boolean.</returns>
        protected bool HasAttribute(XmlNode node, string match)
        {
            if (null == node.Attributes)
            {
                return false;
            }

            return node.Attributes[match] != null;

            // foreach (XmlAttribute candidate in attributes)
            // {
            // if (candidate.Name == match)
            // {
            // return true;
            // }
            // }

            // return false;
        }

        /// <summary>The build typed string value.</summary>
        /// <param name="value">The value.</param>
        /// <param name="targetTypeName">The target type name.</param>
        /// <returns>The Spring.Objects.Factory.Config.TypedStringValue.</returns>
        protected TypedStringValue buildTypedStringValue(string value, string targetTypeName)
        {
            TypedStringValue typedValue;
            if (!StringUtils.HasText(targetTypeName))
            {
                typedValue = new TypedStringValue(value);
            }
            else
            {
                var targetType = Type.GetType(targetTypeName);

                if (targetType != null)
                {
                    typedValue = new TypedStringValue(value, targetType);
                }
                else
                {
                    typedValue = new TypedStringValue(value, targetTypeName);
                }
            }

            return typedValue;
        }

        /// <summary>The build typed string value for map.</summary>
        /// <param name="value">The value.</param>
        /// <param name="defaultTypeName">The default type name.</param>
        /// <returns>The System.Object.</returns>
        protected object buildTypedStringValueForMap(string value, string defaultTypeName)
        {
            TypedStringValue typedValue = this.buildTypedStringValue(value, defaultTypeName);
            return typedValue;
        }

        /// <summary>The parse key element.</summary>
        /// <param name="keyEle">The key ele.</param>
        /// <param name="bd">The bd.</param>
        /// <param name="defaultKeyTypeName">The default key type name.</param>
        /// <returns>The System.Object.</returns>
        protected object parseKeyElement(XmlElement keyEle, IObjectDefinition bd, string defaultKeyTypeName)
        {
            XmlNodeList nl = keyEle.ChildNodes;
            XmlElement subElement = null;
            for (int i = 0; i < nl.Count; i++)
            {
                XmlNode node = nl.Item(i);
                if (node is XmlElement)
                {
                    // Child element is what we're looking for.
                    if (subElement != null)
                    {
                        this.Error("<key> element must not contain more than one value sub-element", keyEle);
                    }
                    else
                    {
                        subElement = (XmlElement)node;
                    }
                }
            }

            return this.parsePropertySubElement(subElement, bd, defaultKeyTypeName);
        }

        private string getNamespaceURI(XmlNode node) { return node.NamespaceURI; }

        private bool isDefaultNamespace(string namespaceUri) { return !StringUtils.HasLength(namespaceUri) || OBJECTS_NAMESPACE_URI.Equals(namespaceUri); }

        private bool isDefaultNamespace(XmlNode node) { return this.isDefaultNamespace(this.getNamespaceURI(node)); }

        private ObjectDefinitionHolder parseNestedCustomElement(XmlElement ele, IObjectDefinition containingBd)
        {
            IObjectDefinition innerDefinition = this.parserContext.ParserHelper.ParseCustomElement(ele, containingBd);
            if (innerDefinition == null)
            {
                this.Error(
                    "Incorrect usage of element '" + ele.Name + "' in a nested manner. " +
                    "This tag cannot be used nested inside <property>.", 
                    ele);
                return null;
            }

            string id = ele.Name + ObjectDefinitionReaderUtils.GENERATED_OBJECT_NAME_SEPARATOR +
                        ObjectUtils.GetIdentityHexString(innerDefinition);

            return new ObjectDefinitionHolder(innerDefinition, id);
        }

        // private ObjectDefinitionHolder decorateObjectDefinitionIfRequired(XmlElement ele, ObjectDefinitionHolder definitionHolder)
        // {
        // return decorateObjectDefinitionIfRequired(ele, definitionHolder, null);
        // }

        // private ObjectDefinitionHolder decorateObjectDefinitionIfRequired(
        // XmlElement ele, ObjectDefinitionHolder definitionHolder, IObjectDefinition containingBd)
        // {

        // ObjectDefinitionHolder finalDefinition = definitionHolder;

        // // Decorate based on custom attributes first.
        // XmlAttributeCollection attributes = ele.Attributes;
        // for (int i = 0; i < attributes.Count; i++)
        // {
        // XmlNode node = attributes.Item(i);
        // finalDefinition = decorateIfRequired(node, finalDefinition, containingBd);
        // }

        // // Decorate based on custom nested elements.
        // XmlNodeList children = ele.ChildNodes;
        // for (int i = 0; i < children.Count; i++)
        // {
        // XmlNode node = children.Item(i);
        // if (node != null && node.NodeType == XmlNodeType.Element)
        // {
        // finalDefinition = decorateIfRequired(node, finalDefinition, containingBd);
        // }
        // }
        // return finalDefinition;
        // }

        // private ObjectDefinitionHolder decorateIfRequired(
        // XmlNode node, ObjectDefinitionHolder originalDef, IObjectDefinition containingBd)
        // {

        // String namespaceUri = getNamespaceURI(node);
        // if (!isDefaultNamespace(namespaceUri))
        // {
        // INamespaceParser handler = NamespaceParserRegistry.GetParser(namespaceUri);

        // if (handler != null)
        // {
        // return handler.Decorate(node, originalDef, new ParserContext(parserContext.ReaderContext, this, containingBd));
        // }
        // else if (namespaceUri != null && namespaceUri.StartsWith("http://www.springframework.net/"))
        // {
        // Error("Unable to locate Spring NamespaceHandler for XML schema namespace [" + namespaceUri + "]", node);
        // }
        // //else
        // //{
        // //    // A custom namespace, not to be handled by Spring - maybe "xml:...".
        // //    if (Logger.isDebugEnabled())
        // //    {
        // //        Logger.debug("No Spring NamespaceHandler found for XML schema namespace [" + namespaceUri + "]");
        // //    }
        // //}
        // }
        // return originalDef;
        // }
        private object parseValueElement(XmlElement ele, string defaultTypeName)
        {
            // It's a literal value.
            string value = ele.Value;
            string specifiedTypeName = ele.Attributes[TYPE_ATTRIBUTE].Value;
            string typeName = specifiedTypeName;
            if (!StringUtils.HasText(typeName))
            {
                typeName = defaultTypeName;
            }

            TypedStringValue typedValue = this.buildTypedStringValue(value, typeName);
            typedValue.TargetTypeName = specifiedTypeName;
            return typedValue;
        }

        private object parsePropertySubElement(XmlElement ele, IObjectDefinition bd, string defaultValueType)
        {
            if (!isDefaultNamespace(ele))
            {
                return this.parseNestedCustomElement(ele, bd);
            }
            else if (this.NodeNameEquals(ele, OBJECT_ELEMENT))
            {
                ObjectDefinitionHolder nestedBd = this.parserContext.ParserHelper.ParseObjectDefinitionElement(ele, bd);

                // if (nestedBd != null)
                // {
                // nestedBd = decorateObjectDefinitionIfRequired(ele, nestedBd, bd);
                // }
                return nestedBd;
            }
            else if (this.NodeNameEquals(ele, REF_ELEMENT))
            {
                // A generic reference to any name of any object.
                string refName = ele.GetAttribute(OBJECT_REF_ATTRIBUTE);
                bool toParent = false;
                if (!StringUtils.HasLength(refName))
                {
                    // A reference to the id of another object in the same XML file.
                    refName = ele.GetAttribute(LOCAL_REF_ATTRIBUTE);
                    if (!StringUtils.HasLength(refName))
                    {
                        // A reference to the id of another object in a parent context.
                        refName = ele.GetAttribute(PARENT_REF_ATTRIBUTE);
                        toParent = true;
                        if (!StringUtils.HasLength(refName))
                        {
                            this.Error("'object', 'local' or 'parent' is required for <ref> element", ele);
                            return null;
                        }
                    }
                }

                if (!StringUtils.HasText(refName))
                {
                    this.Error("<ref> element contains empty target attribute", ele);
                    return null;
                }

                var reference = new RuntimeObjectReference(refName, toParent);
                return reference;
            }
                
                
                
                
                
                
                
                // else if (NodeNameEquals(ele, IDREF_ELEMENT)) {
                // return parseIdRefElement(ele);
                // }
            else if (this.NodeNameEquals(ele, VALUE_ELEMENT))
            {
                return this.parseValueElement(ele, defaultValueType);
            }
            else if (this.NodeNameEquals(ele, NULL_ELEMENT))
            {
                // It's a distinguished null value. Let's wrap it in a TypedStringValue
                // object in order to preserve the source location.
                var nullHolder = new TypedStringValue(null);
                return nullHolder;
            }
            else if (this.NodeNameEquals(ele, ARRAY_ELEMENT))
            {
                return this.parseArrayElement(ele, bd);
            }
            else if (this.NodeNameEquals(ele, LIST_ELEMENT))
            {
                return this.parseListElement(ele, bd);
            }
            else if (this.NodeNameEquals(ele, SET_ELEMENT))
            {
                return this.parseSetElement(ele, bd);
            }
            else if (this.NodeNameEquals(ele, MAP_ELEMENT))
            {
                return this.ParseMapElement(ele, bd);
            }
                
                
                
                
                
                
                
                // else if (NodeNameEquals(ele, PROPS_ELEMENT))
                // {
                // return parsePropsElement(ele);
                // }
            else
            {
                this.Error("Unknown property sub-element: [" + ele.Name + "]", ele);
                return null;
            }
        }

        private IList parseListElement(XmlElement collectionEle, IObjectDefinition bd)
        {
            string defaultElementType = collectionEle.Attributes[VALUE_TYPE_ATTRIBUTE].Value;
            XmlNodeList nl = collectionEle.ChildNodes;
            var target = new ManagedList(nl.Count);
            target.ElementTypeName = defaultElementType;
            target.MergeEnabled = this.ParseMergeAttribute(collectionEle);
            this.parseCollectionElements(nl, target, bd, defaultElementType);
            return target;
        }

        private Set parseSetElement(XmlElement collectionEle, IObjectDefinition bd)
        {
            string defaultElementType = collectionEle.Attributes[VALUE_TYPE_ATTRIBUTE].Value;
            XmlNodeList nl = collectionEle.ChildNodes;
            var target = new ManagedSet(nl.Count);
            target.ElementTypeName = defaultElementType;
            target.MergeEnabled = this.ParseMergeAttribute(collectionEle);
            this.parseCollectionElements(nl, target, bd, defaultElementType);
            return target;
        }

        /// <summary>The parse collection elements.</summary>
        /// <param name="elementNodes">The element nodes.</param>
        /// <param name="target">The target.</param>
        /// <param name="bd">The bd.</param>
        /// <param name="defaultElementType">The default element type.</param>
        protected void parseCollectionElements(XmlNodeList elementNodes, ICollection target, IObjectDefinition bd, string defaultElementType)
        {
            for (int i = 0; i < elementNodes.Count; i++)
            {
                XmlNode node = elementNodes.Item(i);
                if (node is XmlElement && !this.NodeNameEquals(node, DESCRIPTION_ELEMENT))
                {
                    object subElement = this.parsePropertySubElement((XmlElement)node, bd, defaultElementType);

                    if (target as DictionarySet != null)
                    {
                        ((DictionarySet)target).Add(subElement);
                    }
                    else if (target as ManagedList != null)
                    {
                        ((ManagedList)target).Add(subElement);
                    }
                }
            }
        }

        private object parseArrayElement(XmlElement arrayEle, IObjectDefinition bd)
        {
            string elementType = arrayEle.Attributes[VALUE_TYPE_ATTRIBUTE].Value;
            XmlNodeList nl = arrayEle.ChildNodes;
            var target = new ManagedList(nl.Count);
            target.ElementTypeName = elementType;
            target.MergeEnabled = this.ParseMergeAttribute(arrayEle);
            this.parseCollectionElements(nl, target, bd, elementType);
            return target;
        }

        /// <summary>The parse map element to typed dictionary.</summary>
        /// <param name="mapEle">The map ele.</param>
        /// <param name="od">The od.</param>
        /// <returns>The System.Collections.Generic.IDictionary`2[TKey -&gt; System.String, TValue -&gt; System.Object].</returns>
        public IDictionary<string, object> ParseMapElementToTypedDictionary(XmlElement mapEle, IObjectDefinition od) { return this.ConvertToTypedDictionary(this.ParseMapElement(mapEle, od)); }

        /// <summary>The parse map element.</summary>
        /// <param name="mapEle">The map ele.</param>
        /// <param name="bd">The bd.</param>
        /// <returns>The System.Collections.IDictionary.</returns>
        public IDictionary ParseMapElement(XmlElement mapEle, IObjectDefinition bd)
        {
            string defaultKeyType = mapEle.GetAttribute(KEY_TYPE_ATTRIBUTE);
            string defaultValueType = mapEle.GetAttribute(VALUE_TYPE_ATTRIBUTE);

            XmlNodeList entryEles = mapEle.GetElementsByTagName(ENTRY_ELEMENT, OBJECTS_NAMESPACE_URI);
            var map = new ManagedDictionary(entryEles.Count);
            map.KeyTypeName = defaultKeyType;
            map.ValueTypeName = defaultValueType;
            map.MergeEnabled = this.ParseMergeAttribute(mapEle);

            foreach (XmlNode entryEle in entryEles)
            {
                // Should only have one value child element: ref, value, list, etc.
                // Optionally, there might be a key child element.
                XmlNodeList entrySubNodes = entryEle.ChildNodes;
                XmlElement keyEle = null;
                XmlElement valueEle = null;
                for (int j = 0; j < entrySubNodes.Count; j++)
                {
                    XmlNode node = entrySubNodes.Item(j);
                    if (node is XmlElement)
                    {
                        var candidateEle = (XmlElement)node;
                        if (this.NodeNameEquals(candidateEle, KEY_ELEMENT))
                        {
                            if (keyEle != null)
                            {
                                this.Error("<entry> element is only allowed to contain one <key> sub-element", entryEle);
                            }
                            else
                            {
                                keyEle = candidateEle;
                            }
                        }
                        else
                        {
                            // Child element is what we're looking for.
                            if (valueEle != null)
                            {
                                this.Error("<entry> element must not contain more than one value sub-element", entryEle);
                            }
                            else
                            {
                                valueEle = candidateEle;
                            }
                        }
                    }
                }

                // Extract key from attribute or sub-element.
                object key = null;
                bool hasKeyAttribute = this.HasAttribute(entryEle, KEY_ATTRIBUTE);
                bool hasKeyRefAttribute = this.HasAttribute(entryEle, KEY_REF_ATTRIBUTE);
                if ((hasKeyAttribute && hasKeyRefAttribute) ||
                    (hasKeyAttribute || hasKeyRefAttribute) && keyEle != null)
                {
                    this.Error(
                        "<entry> element is only allowed to contain either " +
                        "a 'key' attribute OR a 'key-ref' attribute OR a <key> sub-element", 
                        entryEle);
                }

                if (hasKeyAttribute)
                {
                    key = this.buildTypedStringValueForMap(entryEle.Attributes[KEY_ATTRIBUTE].Value, defaultKeyType);
                }
                else if (hasKeyRefAttribute)
                {
                    string refName = entryEle.Attributes[KEY_REF_ATTRIBUTE].Value;
                    if (!StringUtils.HasText(refName))
                    {
                        this.Error("<entry> element contains empty 'key-ref' attribute", entryEle);
                    }

                    var reference = new RuntimeObjectReference(refName);
                    key = reference;
                }
                else if (keyEle != null)
                {
                    key = this.parseKeyElement(keyEle, bd, defaultKeyType);
                }
                else
                {
                    this.Error("<entry> element must specify a key", entryEle);
                }

                // Extract value from attribute or sub-element.
                object value = null;
                bool hasValueAttribute = this.HasAttribute(entryEle, VALUE_ATTRIBUTE);
                bool hasValueRefAttribute = this.HasAttribute(entryEle, VALUE_REF_ATTRIBUTE);
                if ((hasValueAttribute && hasValueRefAttribute) ||
                    (hasValueAttribute || hasValueRefAttribute) && valueEle != null)
                {
                    this.Error(
                        "<entry> element is only allowed to contain either " +
                        "'value' attribute OR 'value-ref' attribute OR <value> sub-element", 
                        entryEle);
                }

                if (hasValueAttribute)
                {
                    value = this.buildTypedStringValueForMap(entryEle.Attributes[VALUE_ATTRIBUTE].Value, defaultValueType);
                }
                else if (hasValueRefAttribute)
                {
                    string refName = entryEle.Attributes[VALUE_REF_ATTRIBUTE].Value;
                    if (!StringUtils.HasText(refName))
                    {
                        this.Error("<entry> element contains empty 'value-ref' attribute", entryEle);
                    }

                    var reference = new RuntimeObjectReference(refName);
                    value = reference;
                }
                else if (valueEle != null)
                {
                    value = this.parsePropertySubElement(valueEle, bd, defaultValueType);
                }
                else
                {
                    this.Error("<entry> element must specify a value", entryEle);
                }

                // Add final key and value to the Map.
                map.Add(key, value);
            }

            return map;
        }

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
        public Dictionary<string, object> ConvertToTypedDictionary(IDictionary dictionary)
        {
            var result = new Dictionary<string, object>();

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is TypedStringValue)
                {
                    result.Add(((TypedStringValue)entry.Key).Value, entry.Value);
                }
                else
                {
                    result.Add((string)entry.Key, entry.Value);
                }
            }

            return result;
        }
    }
}
