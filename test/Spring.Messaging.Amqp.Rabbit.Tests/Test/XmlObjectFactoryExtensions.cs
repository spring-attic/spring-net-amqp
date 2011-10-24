
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Test
{
    /// <summary>
    /// XmlObjectFactory Extensions
    /// </summary>
    public static class XmlObjectFactoryExtensions
    {
        /// <summary>
        /// Gets the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory">The factory.</param>
        /// <returns>The requested object.</returns>
        public static T GetObject<T>(this XmlObjectFactory factory)
        {
            var objectsForType = factory.GetObjectNamesForType(typeof(T));

            if(objectsForType.Count() > 0)
            {
                return (T)factory.GetObject(objectsForType[0]);
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Gets the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory">The factory.</param>
        /// <param name="name">The name.</param>
        /// <returns>The requested object.</returns>
        public static T GetObject<T>(this XmlObjectFactory factory, string name)
        {
            return (T)factory.GetObject(name, typeof(T));
        }
    }
}
