// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErlangErrorRpcException.cs" company="The original author or authors.">
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
using Erlang.NET;
#endregion

namespace Spring.Erlang
{
    /// <summary>
    /// Exception thrown when an 'error' is received from an Erlang RPC call. 
    /// </summary>
    /// <author>Mark Pollack</author>
    public class ErlangErrorRpcException : OtpErlangException
    {
        private readonly OtpErlangTuple reasonTuple;

        /// <summary>Initializes a new instance of the <see cref="ErlangErrorRpcException"/> class.</summary>
        /// <param name="reason">The reason.</param>
        public ErlangErrorRpcException(string reason) : base(reason) { }

        /// <summary>Initializes a new instance of the <see cref="ErlangErrorRpcException"/> class.</summary>
        /// <param name="tuple">The tuple.</param>
        public ErlangErrorRpcException(OtpErlangTuple tuple) : base(tuple.ToString()) { this.reasonTuple = tuple; }

        /// <summary>Gets the reason tuple.</summary>
        public OtpErlangTuple ReasonTuple { get { return this.reasonTuple; } }
    }
}
