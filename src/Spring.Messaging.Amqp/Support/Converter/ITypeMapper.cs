// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITypeMapper.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using Spring.Messaging.Amqp.Core;
#endregion

namespace Spring.Messaging.Amqp.Support.Converter
{
    /// <summary>
    /// Provides a layer of indirection when adding the 'type' of the object as a message property.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>James Carr</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public interface ITypeMapper
    {
        /// <summary>Convert from Type to string</summary>
        /// <param name="typeOfObjectToConvert">The type of object to convert.</param>
        /// <param name="properties">The properties.</param>
        void FromType(Type typeOfObjectToConvert, MessageProperties properties);

        /// <summary>Convert from string to Type.</summary>
        /// <param name="properties">The properties.</param>
        /// <returns>The type.</returns>
        Type ToType(MessageProperties properties);
    }
}
