
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

using Erlang.NET;

namespace Spring.Erlang.Support.Converter
{
    /// <summary>
    /// Converter between .NET and Erlang Types.  Additional support for converting results from RPC calls.  
    /// </summary>
    /// <author>Mark Pollack</author>
    public interface IErlangConverter
    {
        /// <summary>
        /// Convert a .NET object to a Erlang data type.
        /// </summary>
        /// <param name="objectToConvert">The object to convert.</param>
        /// <returns>the Erlang data type</returns>
        /// <exception cref="ErlangConversionException">in case of conversion failures</exception>
        OtpErlangObject ToErlang(object objectToConvert);

        /// <summary>
        /// Convert from an Erlang data type to a .NET data type.
        /// </summary>
        /// <param name="erlangObject">The erlang object.</param>
        /// <returns>The converted .NET object</returns>
        /// <exception cref="ErlangConversionException">in case of conversion failures</exception>
        object FromErlang(OtpErlangObject erlangObject);

        /// <summary>
        /// The return value from executing the Erlang RPC.
        /// </summary>
        /// <param name="module">The module to call</param>
        /// <param name="function">The function to invoke</param>
        /// <param name="erlangObject">The erlang object that is passed in as a parameter</param>
        /// <returns>The converted .NET object return value from the RPC call.</returns>
        /// <exception cref="ErlangConversionException">in case of conversion failures</exception> 
        object FromErlangRpc(string module, string function, OtpErlangObject erlangObject);
    }
}