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

namespace Spring.Messaging.Amqp.Support.Converter
{
    public class TypeMapper : ITypeMapper
    {

        public static readonly string DEFAULT_TYPEID_FIELD_NAME = "__TypeId__";
        private string defaultNamespace;
        private string defaultAssemblyName;

        //Generics not used to support both 1.1 and 2.0
        private IDictionary idTypeMapping;
        private IDictionary typeIdMapping;
        
        
        //TODO generalize?
        private string defaultHashtableTypeId = "Hashtable";

        private Type defaultHashtableClass = typeof(Hashtable);

        public TypeMapper()
        {
            idTypeMapping = new Hashtable();
            typeIdMapping = new Hashtable();
        }


        public IDictionary IdTypeMapping
        {
            get { return idTypeMapping; }
            set { idTypeMapping = value; }
        }

        public string TypeIdFieldName
        {
            get { return DEFAULT_TYPEID_FIELD_NAME; }
        }


        public Type DefaultHashtableClass
        {
            set { defaultHashtableClass = value; }
        }

        public string FromType(Type typeOfObjectToConvert)
        {

            if (typeIdMapping.Contains(typeOfObjectToConvert))
            {
                return typeIdMapping[typeOfObjectToConvert] as string;
            }
            else
            {
                if ( typeof (IDictionary).IsAssignableFrom(typeOfObjectToConvert))
                {
                    return defaultHashtableTypeId;
                }
                return typeOfObjectToConvert.Name;
            }
        }

        public Type ToType(string typeId)
        {
            if (idTypeMapping.Contains(typeId))
            {
                return idTypeMapping[typeId] as Type;
            }
            else
            {
                if (typeId.Equals(defaultHashtableTypeId))
                {
                    return defaultHashtableClass;
                }
                string fullyQualifiedTypeName = defaultNamespace + "." +
                                                typeId + ", " +
                                                DefaultAssemblyName;
                return TypeResolutionUtils.ResolveType(fullyQualifiedTypeName); 
            }

        }

        public string DefaultNamespace
        {
            get
            {
                return defaultNamespace;
            }
            set
            {
                defaultNamespace = value;
            }
            
        }

        public string DefaultAssemblyName
        {
            get
            {
                return defaultAssemblyName;
            }
            set
            {
                defaultAssemblyName = value;
            }
        }

        public void AfterPropertiesSet()
        {
            ValidateIdTypeMapping();
            if (DefaultAssemblyName != null && DefaultNamespace == null)
            {
                throw new ArgumentException("Default Namespace required when DefaultAssemblyName is set.");
            }
            if (DefaultNamespace != null && DefaultAssemblyName == null)
            {
                throw new ArgumentException("Default Assembly Name required when DefaultNamespace is set.");
            }
        }


        private void ValidateIdTypeMapping()
        {
            IDictionary finalIdTypeMapping = new Hashtable();
            foreach (DictionaryEntry entry in idTypeMapping)
            {
                string id = entry.Key.ToString();
                Type t = entry.Value as Type;
                if (t == null)
                {
                    //convert from string value.
                    string typeName = entry.Value.ToString();
                    t = TypeResolutionUtils.ResolveType(typeName);
                }
                finalIdTypeMapping.Add(id,t);
                typeIdMapping.Add(t,id);
                
            }
            idTypeMapping = finalIdTypeMapping;
        }
    }
}