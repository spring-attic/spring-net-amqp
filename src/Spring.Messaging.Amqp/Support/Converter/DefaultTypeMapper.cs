// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultTypeMapper.cs" company="The original author or authors.">
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
using System.Text;
using Common.Logging;
using Spring.Core.TypeResolution;
using Spring.Messaging.Amqp.Core;
using Spring.Objects.Factory;
#endregion

namespace Spring.Messaging.Amqp.Support.Converter
{
    /// <summary>
    /// A type mapper implementaiton.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class DefaultTypeMapper : ITypeMapper, IInitializingObject
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        public static readonly string DEFAULT_TYPEID_FIELD_NAME = "__TypeId__";

        private IDictionary<string, Type> idTypeMapping = new Dictionary<string, Type>();
        private readonly IDictionary<Type, string> typeIdMapping = new Dictionary<Type, string>();

        private string defaultDictionaryTypeId = "Dictionary";

        private Type defaultDictionaryType = typeof(Dictionary<string, object>);

        /// <summary>
        /// Sets the default type of the dictionary.
        /// </summary>
        /// <value>The default type of the dictionary.</value>
        public Type DefaultDictionaryType { set { this.defaultDictionaryType = value; } }

        /// <summary>
        /// Gets the name of the type id field.
        /// </summary>
        public virtual string TypeIdFieldName { get { return DEFAULT_TYPEID_FIELD_NAME; } }

        /// <summary>
        /// Sets the id type mapping.
        /// </summary>
        /// <value>The id type mapping.</value>
        public IDictionary<string, object> IdTypeMapping
        {
            set
            {
                var suppliedValue = value;
                var finalValue = new Dictionary<string, Type>();
                foreach (var entry in suppliedValue)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Value != null && !string.IsNullOrWhiteSpace(entry.Value.ToString()))
                    {
                        var entryType = TypeResolutionUtils.ResolveType(entry.Value.ToString());
                        if (entryType != null && !string.IsNullOrWhiteSpace(entryType.FullName))
                        {
                            finalValue.Add(entry.Key, entryType);
                        }
                    }
                }

                this.idTypeMapping = finalValue;
            }
        }

        /// <summary>Convert from Type to string.</summary>
        /// <param name="typeOfObjectToConvert">The type of object to convert.</param>
        /// <returns>The string.</returns>
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

            return typeOfObjectToConvert.FullName;
        }

        /// <summary>Convert a string type identifier to a Type.</summary>
        /// <param name="typeId">The type id.</param>
        /// <returns>The Type</returns>
        private Type ToType(string typeId)
        {
            Logger.Trace(m => m("Converting TypeId: [{0}] to a Type", typeId));
            if (this.idTypeMapping == null)
            {
                Logger.Trace(m => m("Warning - idTypeMapping is null."));
            }

            if (this.idTypeMapping != null && this.idTypeMapping.ContainsKey(typeId))
            {
                Logger.Trace(m => m("Returning existing Type from idTypeMapping"));
                return this.idTypeMapping[typeId];
            }

            Logger.Trace(m => m("defaultDictionaryTypeId Is: [{0}]", this.defaultDictionaryTypeId));
            if (typeId.Equals(this.defaultDictionaryTypeId))
            {
                Logger.Trace(m => m("Returning defaultDictionaryTypeId"));
                return this.defaultDictionaryType;
            }

            try
            {
                Logger.Trace(m => m("Resolving type with typeId: [{0}]", typeId));
                return TypeResolutionUtils.ResolveType(typeId);
            }
            catch (Exception ex)
            {
                Logger.Error(m => m("An exception was caught resolving the type: {0}", typeId), ex);
                try
                {
                    /* Commenting this out to avoid an AccessViolationException in certain (but not all) environments. Better make sure you have the typemappings set in config :)...
                    
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
                    */
                }
                catch (Exception e)
                {
                    Logger.Error(m => m("An exception was caught trying to use the fallback type resolution method."), e);
                }

                throw new MessageConversionException("failed to resolve type name [" + typeId + "]", ex);
            }
        }

        /// <summary>
        /// Performs validation actions after properties are set.
        /// </summary>
        public void AfterPropertiesSet() { this.ValidateIdTypeMapping(); }

        /// <summary>
        /// Validates Id Type Mapping.
        /// </summary>
        private void ValidateIdTypeMapping()
        {
            var finalIdTypeMapping = new Dictionary<string, Type>();
            foreach (var entry in this.idTypeMapping)
            {
                var id = entry.Key;

                var t = entry.Value;

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

        /// <summary>The from type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="properties">The properties.</param>
        public void FromType(Type type, MessageProperties properties) { properties.Headers.Add(this.TypeIdFieldName, this.FromType(type)); }

        /// <summary>Convert from string to Type.</summary>
        /// <param name="properties">The message properties.</param>
        /// <returns>The type.</returns>
        public Type ToType(MessageProperties properties)
        {
            var headers = properties.Headers;

            if (!headers.ContainsKey(this.TypeIdFieldName))
            {
                throw new MessageConversionException("failed to convert Message content. Could not resolve " + this.TypeIdFieldName + " in header");
            }

            object typeIdFieldNameValue;
            try
            {
                typeIdFieldNameValue = headers[this.TypeIdFieldName];
            }
            catch (Exception)
            {
                typeIdFieldNameValue = null;
            }

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
        /// <summary>Toes the name of the type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>String representation of the type (Assembly.Namespace.Type, Assembly) for use with TypeResolutionUtils.ResolveType(string typeName).</returns>
        public static string ToTypeName(this Type type)
        {
            if (type == null || type.AssemblyQualifiedName == null || string.IsNullOrWhiteSpace(type.AssemblyQualifiedName))
            {
                return string.Empty;
            }

            return type.AssemblyQualifiedName.Substring(0, IndexOfOccurence(type.AssemblyQualifiedName, ",", 0, 2));
        }

        /// <summary>The index of occurence.</summary>
        /// <param name="input">The input.</param>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="occurence">The occurence.</param>
        /// <returns>The System.Int32.</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static int IndexOfOccurence(this string input, string value, int startIndex, int occurence)
        {
            if (occurence < 1)
            {
                throw new NotSupportedException("Param 'occurence' must be greater than 0!");
            }

            if (occurence == 1)
            {
                return input.IndexOf(value, startIndex);
            }

            return input.IndexOfOccurence(value, input.IndexOf(value, startIndex) + 1, --occurence);
        }
    }
}
