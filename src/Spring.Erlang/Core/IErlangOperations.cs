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
using Spring.Erlang.Support.Converter;

namespace Spring.Erlang.Core
{
    /// <summary>
    /// An erlang operations interface.
    /// </summary>
    /// <author>Mark Pollack</author>
    public interface IErlangOperations
    {
        /// <summary>
        /// Executes the specified action.
        /// </summary>
        /// <typeparam name="T">Type T.</typeparam>
        /// <param name="action">The action.</param>
        /// <returns>An object.</returns>
        /// <remarks></remarks>
        T Execute<T>(ConnectionCallbackDelegate<T> action);

        /// <summary>
        /// Executes the erlang RPC.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="args">The args.</param>
        /// <returns>An object.</returns>
        /// <remarks></remarks>
        OtpErlangObject ExecuteErlangRpc(string module, string function, OtpErlangList args);

        /// <summary>
        /// Executes the erlang RPC.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="args">The args.</param>
        /// <returns>An object.</returns>
        /// <remarks></remarks>
        OtpErlangObject ExecuteErlangRpc(string module, string function, params OtpErlangObject[] args);

        /// <summary>
        /// Executes the RPC.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="args">The args.</param>
        /// <returns>An object.</returns>
        /// <remarks></remarks>
        OtpErlangObject ExecuteRpc(string module, string function, params object[] args);

        /// <summary>
        /// Executes the and convert RPC.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="converterToUse">The converter to use.</param>
        /// <param name="args">The args.</param>
        /// <returns>An object.</returns>
        /// <remarks></remarks>
        object ExecuteAndConvertRpc(string module, string function, IErlangConverter converterToUse,params object[] args);

        /// <summary>
        /// Executes the and convert RPC.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="args">The args.</param>
        /// <returns>An object.</returns>
        /// <remarks></remarks>
        object ExecuteAndConvertRpc(string module, string function, params object[] args);
    }
}