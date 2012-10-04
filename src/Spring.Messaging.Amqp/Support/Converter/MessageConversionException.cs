// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageConversionException.cs" company="The original author or authors.">
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
#endregion

namespace Spring.Messaging.Amqp.Support.Converter
{
    /// <summary>
    /// A message conversion exception.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Mark Fisher</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class MessageConversionException : Exception
    {
        #region Constructor (s) / Destructor

        /// <summary>Initializes a new instance of the <see cref="MessageConversionException"/> class. 
        /// Creates a new instance of the IMessageConverterException class. with the specified message.</summary>
        /// <param name="message">A message about the exception.</param>
        public MessageConversionException(string message) : base(message) { }

        /// <summary>Initializes a new instance of the <see cref="MessageConversionException"/> class. 
        /// Creates a new instance of the IMessageConverterException class with the specified message
        /// and root cause.</summary>
        /// <param name="message">A message about the exception.</param>
        /// <param name="cause">The root exception that is being wrapped.</param>
        public MessageConversionException(string message, Exception cause) : base(message, cause) { }
        #endregion
    }
}
