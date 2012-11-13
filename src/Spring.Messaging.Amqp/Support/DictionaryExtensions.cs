// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DictionaryExtensions.cs" company="The original author or authors.">
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
using System.Collections.Generic;
#endregion

namespace Spring.Messaging.Amqp.Support
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>The to dictionary.</summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>The System.Collections.IDictionary.</returns>
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
