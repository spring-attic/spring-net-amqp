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


using System;
using System.Collections;
using Spring.Core.TypeResolution;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Support.Converter
{
    using System.Text;

    using Spring.Objects.Factory;

    /// <summary>
    /// A type mapper implementaiton.
    /// </summary>
    public class TypeMapper : ITypeMapper, IInitializingObject
    {
        public static readonly string DEFAULT_TYPEID_FIELD_NAME = "__TypeId__";
        private string defaultNamespace;
        private string defaultAssemblyName;

        //Generics not used to support both 1.1 and 2.0
        private IDictionary idTypeMapping;
        private IDictionary typeIdMapping;

        // TODO generalize?
        private string defaultHashtableTypeId = "Hashtable";

        private Type defaultHashtableClass = typeof(Hashtable);

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeMapper"/> class.
        /// </summary>
        public TypeMapper()
        {
            this.idTypeMapping = new Hashtable();
            this.typeIdMapping = new Hashtable();
        }

        /// <summary>
        /// Gets or sets IdTypeMapping.
        /// </summary>
        public IDictionary IdTypeMapping
        {
            get { return this.idTypeMapping; }
            set { this.idTypeMapping = value; }
        }

        /// <summary>
        /// Gets TypeIdFieldName.
        /// </summary>
        public string TypeIdFieldName
        {
            get { return DEFAULT_TYPEID_FIELD_NAME; }
        }

        /// <summary>
        /// Sets DefaultHashtableClass.
        /// </summary>
        public Type DefaultHashtableClass
        {
            set { this.defaultHashtableClass = value; }
        }


        /// <summary>
        /// Gets or sets DefaultNamespace.
        /// </summary>
        public string DefaultNamespace
        {
            get { return this.defaultNamespace; }
            set { this.defaultNamespace = value; }
        }

        /// <summary>
        /// Gets or sets DefaultAssemblyName.
        /// </summary>
        public string DefaultAssemblyName
        {
            get { return this.defaultAssemblyName; }
            set { this.defaultAssemblyName = value; }
        }

        /// <summary>
        /// Add type mapping to headers.
        /// </summary>
        /// <param name="typeOfObjectToConvert">
        /// The type of object to convert.
        /// </param>
        /// <param name="properties">
        /// The properties.
        /// </param>
        public void FromType(Type typeOfObjectToConvert, MessageProperties properties)
        {
            properties.Headers.Add(this.TypeIdFieldName, this.FromType(typeOfObjectToConvert));
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
            var typeIdFieldNameValue = headers[this.TypeIdFieldName];

            string typeId = null;
            if (typeIdFieldNameValue != null)
            {
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


        /// <summary>
        /// Performs validation actions after properties are set.
        /// </summary>
        public void AfterPropertiesSet()
        {
            this.ValidateIdTypeMapping();
            if (this.DefaultAssemblyName != null && this.DefaultNamespace == null)
            {
                throw new ArgumentException("Default Namespace required when DefaultAssemblyName is set.");
            }

            if (this.DefaultNamespace != null && this.DefaultAssemblyName == null)
            {
                throw new ArgumentException("Default Assembly Name required when DefaultNamespace is set.");
            }
        }


        /// <summary>
        /// Validates Id Type Mapping.
        /// </summary>
        private void ValidateIdTypeMapping()
        {
            IDictionary finalIdTypeMapping = new Hashtable();
            foreach (DictionaryEntry entry in this.idTypeMapping)
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
            if (this.typeIdMapping.Contains(typeOfObjectToConvert))
            {
                return this.typeIdMapping[typeOfObjectToConvert] as string;
            }
            else
            {
                if (typeof(IDictionary).IsAssignableFrom(typeOfObjectToConvert))
                {
                    return this.defaultHashtableTypeId;
                }

                return typeOfObjectToConvert.Name;
            }
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
            if (this.idTypeMapping.Contains(typeId))
            {
                return this.idTypeMapping[typeId] as Type;
            }

            if (typeId.Equals(this.defaultHashtableTypeId))
            {
                return this.defaultHashtableClass;
            }

            var fullyQualifiedTypeName = this.defaultNamespace + "." + typeId + ", " + this.DefaultAssemblyName;
            return TypeResolutionUtils.ResolveType(fullyQualifiedTypeName);
        }
    }
}