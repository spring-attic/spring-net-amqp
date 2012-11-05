// -----------------------------------------------------------------------
// <copyright file="DictionaryExtensions.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;

namespace Spring.Messaging.Amqp.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class DictionaryExtensions
    {
        public static IDictionary ToDictionary(this IDictionary<string, object> dictionary)
        {
            var result = new Hashtable();
            foreach (var item in dictionary)
            {
                result.Add(item.Key, item.Value);
            }

            return result;
        }
    }
}
