
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Spring.Objects.Factory;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Test
{
    /// <summary>
    /// XmlObjectFactory Extensions
    /// </summary>
    public static class XmlObjectFactoryExtensions
    {

        public static T GetObject<T>(this IListableObjectFactory factory)
        {
            var objectsForType = factory.GetObjectNamesForType(typeof(T));

            if(objectsForType.Count() > 0)
            {
                return (T)factory.GetObject(objectsForType[0]);
            }
            
            //TODO: determine why this behavior is provided as a fall-back to not finding the object in the container...
            return default(T);
        }

        public static T GetObject<T>(this IObjectFactory factory, string name)
        {
            return (T)factory.GetObject(name, typeof(T));
        }

        public static IDictionary GetObjectsOfType<T>(this IListableObjectFactory factory)
        {
            return factory.GetObjectsOfType(typeof (T));
        }
    }
}
