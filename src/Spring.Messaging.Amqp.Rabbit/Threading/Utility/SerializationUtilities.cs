// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationUtilities.cs" company="The original author or authors.">
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
using System.Reflection;
using System.Runtime.Serialization;
#endregion

namespace Spring.Utility
{
    /// <summary>
    /// Collection of static methods that aid in serialization.
    /// </summary>
    public static class SerializationUtilities
    {
        // NET_ONLY
        /// <summary>Writes the serializable fields to the SerializationInfo object, which stores all the data needed to serialize the specified object object.</summary>
        /// <param name="info">SerializationInfo parameter from the GetObjectData method.</param>
        /// <param name="context">StreamingContext parameter from the GetObjectData method.</param>
        /// <param name="instance">Object to serialize.</param>
        public static void DefaultWriteObject(SerializationInfo info, StreamingContext context, object instance)
        {
            Type thisType = instance.GetType();
            MemberInfo[] mi = FormatterServices.GetSerializableMembers(thisType, context);
            for (int i = 0; i < mi.Length; i++)
            {
                info.AddValue(mi[i].Name, ((FieldInfo)mi[i]).GetValue(instance));
            }
        }

        /// <summary>Reads the serialized fields written by the DefaultWriteObject method.</summary>
        /// <param name="info">SerializationInfo parameter from the special deserialization constructor.</param>
        /// <param name="context">StreamingContext parameter from the special deserialization constructor</param>
        /// <param name="instance">Object to deserialize.</param>
        public static void DefaultReadObject(SerializationInfo info, StreamingContext context, object instance)
        {
            Type thisType = instance.GetType();
            MemberInfo[] mi = FormatterServices.GetSerializableMembers(thisType, context);
            for (int i = 0; i < mi.Length; i++)
            {
                var fi = (FieldInfo)mi[i];
                fi.SetValue(instance, info.GetValue(fi.Name, fi.FieldType));
            }
        }
    }
}
