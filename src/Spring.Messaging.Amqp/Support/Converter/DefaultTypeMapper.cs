
#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Spring.Objects.Factory;
using System;
using System.Collections;
using System.Collections.Generic;

using Spring.Core.TypeResolution;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Support.Converter
{
    /// <summary>
    /// A type mapper implementaiton.
    /// </summary>
    public class DefaultTypeMapper : ITypeMapper, IInitializingObject
    {
        public static readonly string DEFAULT_TYPEID_FIELD_NAME = "__TypeId__";
        
        private IDictionary<string, Type> idTypeMapping = new Dictionary<string, Type>();
        private IDictionary<Type, string> typeIdMapping = new Dictionary<Type, string>();

        private string defaultDictionaryTypeId = "Dictionary";

        private Type defaultDictionaryType = typeof(Dictionary<string, object>);

        /// <summary>
        /// Sets the default type of the dictionary.
        /// </summary>
        /// <value>The default type of the dictionary.</value>
        public Type DefaultDictionaryType
        {
            set { this.defaultDictionaryType = value; }
        }

        /// <summary>
        /// Gets the name of the type id field.
        /// </summary>
        public string TypeIdFieldName
        {
            get { return DEFAULT_TYPEID_FIELD_NAME; }
        }

        /// <summary>
        /// Sets the id type mapping.
        /// </summary>
        /// <value>The id type mapping.</value>
        public IDictionary IdTypeMapping
        {
            set
            {
                var suppliedValue = value;
                var finalValue = new Dictionary<string, Type>();
                foreach(DictionaryEntry entry in suppliedValue)
                {
                    if(!string.IsNullOrWhiteSpace(entry.Key.ToString()) && entry.Value != null && !string.IsNullOrWhiteSpace(entry.Value.ToString()))
                    {
                        var entryType = TypeResolutionUtils.ResolveType(entry.Value.ToString());
                        if(entryType != null && !string.IsNullOrWhiteSpace(entryType.FullName)) finalValue.Add(entry.Key.ToString(), entryType);
                    }
                }

                this.idTypeMapping = finalValue;
            }
        }
        
        /// <summary>
        /// Convert from Type to string.
        /// </summary>
        /// <param name="typeOfObjectToConvert">
        /// The type of object to convert.
        /// </param>
        /// <returns>
        /// The string.
        /// </returns>
        private string FromType(Type typeOfObjectToConvert)
        {
            if (this.typeIdMapping.ContainsKey(typeOfObjectToConvert))
            {
                return this.typeIdMapping[typeOfObjectToConvert];
            }

            if (typeof(IDictionary).IsAssignableFrom(typeOfObjectToConvert))
            {
                return this.defaultDictionaryTypeId;
            }

            return typeOfObjectToConvert.Name;
        }

        /// <summary>
        /// Convert a string type identifier to a Type.
        /// </summary>
        /// <param name="typeId">
        /// The type id.
        /// </param>
        /// <returns>
        /// The Type
        /// </returns>
        private Type ToType(string typeId)
        {
            if (this.idTypeMapping.ContainsKey(typeId))
            {
                return this.idTypeMapping[typeId];
            }

            if (typeId.Equals(this.defaultDictionaryTypeId))
            {
                return this.defaultDictionaryType;
            }

            try
            {
                return TypeResolutionUtils.ResolveType(typeId);
            }
            catch (Exception ex)
            {
                #region TODO: Determine if we should be this forgiving :) This doesn't come for free, and could result in unpredictable behavior!
                try
                {
                    var candidateAssemblies = (from a in AppDomain.CurrentDomain.GetAssemblies()
                                               where
                                                   !a.FullName.ToLower().StartsWith("mscorlib")
                                                   && !a.FullName.ToLower().StartsWith("system")
                                               select a).ToList();

                    foreach (var assembly in candidateAssemblies)
                    {
                        var candidateType = (from t in assembly.GetTypes() where t.Name == typeId select t).FirstOrDefault();
                        if(candidateType != null)
                        {
                            this.idTypeMapping.Add(typeId, candidateType); // Speeds things up if we're doing this over and over again...
                            return candidateType;
                        }
                    }
                }
                catch { }
                #endregion

                throw new MessageConversionException("failed to resolve type name [" + typeId + "]", ex);

            }
        }

        /// <summary>
        /// Performs validation actions after properties are set.
        /// </summary>
        public void AfterPropertiesSet()
        {
            this.ValidateIdTypeMapping();
        }


        /// <summary>
        /// Validates Id Type Mapping.
        /// </summary>
        private void ValidateIdTypeMapping()
        {
            var finalIdTypeMapping = new Dictionary<string, Type>();
            foreach (var entry in this.idTypeMapping)
            {
                var id = entry.Key.ToString();

                var t = entry.Value as Type;

                if (t == null)
                {
                    var typeName = entry.Value.ToString();
                    t = TypeResolutionUtils.ResolveType(typeName);
                }

                finalIdTypeMapping.Add(id, t);
                this.typeIdMapping.Add(t, id);
            }

            this.idTypeMapping = finalIdTypeMapping;
        }

        public void FromType(Type type, MessageProperties properties)
        {
            properties.Headers.Add(TypeIdFieldName, FromType(type));
        }

        /// <summary>
        /// Convert from string to Type.
        /// </summary>
        /// <param name="properties">
        /// The message properties.
        /// </param>
        /// <returns>
        /// The type.
        /// </returns>
        public Type ToType(MessageProperties properties)
        {
            var headers = properties.Headers;
            
            if(!headers.ContainsKey(this.TypeIdFieldName))
            {
                throw new MessageConversionException("failed to convert Message content. Could not resolve " + this.TypeIdFieldName + " in header");
            }

            var typeIdFieldNameValue = headers[this.TypeIdFieldName];

            string typeId = null;
            if (typeIdFieldNameValue != null)
            {
                // Not in the Java version - validate that this is needed..
                if (typeIdFieldNameValue is byte[])
                {
                    typeId = Encoding.UTF8.GetString((byte[])typeIdFieldNameValue);
                }
                else
                {
                    typeId = typeIdFieldNameValue.ToString();
                }
            }

            if (typeId == null)
            {
                throw new MessageConversionException("failed to convert Message content. Could not resolve " + this.TypeIdFieldName + " in header");
            }

            return this.ToType(typeId);
        }
    }

    /// <summary>
    /// Extensions to assist with populating IdTypeMapping via CodeConfig.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Toes the name of the type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>String representation of the type (Assembly.Namespace.Type, Assembly) for use with TypeResolutionUtils.ResolveType(string typeName).</returns>
        public static string ToTypeName(this System.Type type)
        {
            if(type == null || type.AssemblyQualifiedName == null || string.IsNullOrWhiteSpace(type.AssemblyQualifiedName))
            {
                return string.Empty;
            }

            return type.AssemblyQualifiedName.Substring(0, IndexOfOccurence(type.AssemblyQualifiedName, ",", 0, 2));
        }

        public static int IndexOfOccurence(this string input, string value, int startIndex, int occurence)
        {
            if (occurence < 1) throw new NotSupportedException("Param 'occurence' must be greater than 0!");
            if (occurence == 1) return input.IndexOf(value, startIndex);
            return input.IndexOfOccurence(value, input.IndexOf(value, startIndex) + 1, --occurence);
        }
    }
}