// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlObjectFactoryExtensions.cs" company="The original author or authors.">
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
using System.Collections;
using System.Linq;
using Spring.Objects.Factory;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Test
{
    /// <summary>
    /// XmlObjectFactory Extensions
    /// </summary>
    public static class XmlObjectFactoryExtensions
    {
        /// <summary>The get object.</summary>
        /// <param name="factory">The factory.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The T.</returns>
        public static T GetObject<T>(this IListableObjectFactory factory)
        {
            var objectsForType = factory.GetObjectNamesForType(typeof(T));

            if (objectsForType.Count() > 0)
            {
                return (T)factory.GetObject(objectsForType[0]);
            }

            // TODO: determine why this behavior is provided as a fall-back to not finding the object in the container...
            return default(T);
        }

        /// <summary>The get object.</summary>
        /// <param name="factory">The factory.</param>
        /// <param name="name">The name.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The T.</returns>
        public static T GetObject<T>(this IObjectFactory factory, string name) { return (T)factory.GetObject(name, typeof(T)); }

        /// <summary>The get objects of type.</summary>
        /// <param name="factory">The factory.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The System.Collections.IDictionary.</returns>
        public static IDictionary GetObjectsOfType<T>(this IListableObjectFactory factory) { return factory.GetObjectsOfType(typeof(T)); }
    }
}
