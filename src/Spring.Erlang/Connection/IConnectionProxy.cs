// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConnectionProxy.cs" company="The original author or authors.">
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
using Erlang.NET;
#endregion

namespace Spring.Erlang.Connection
{
    /// <summary>
    /// Subinterface of {@link IConnection} to be implemented by Connection proxies.
    /// Allows access to the underlying target Connection.
    /// </summary>
    /// <remarks></remarks>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public interface IConnectionProxy : IConnection
    {
        /// <summary>
        /// Gets the target connection.
        /// </summary>
        /// <returns>The connection.</returns>
        /// <remarks></remarks>
        OtpConnection GetTargetConnection();
    }
}
