// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultConnection.cs" company="The original author or authors.">
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

namespace Spring.Erlang.Connection
{
    /// <summary>
    /// Basic implementation of {@link ConnectionProxy} that delegates to an underlying OtpConnection.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class DefaultConnection : IConnectionProxy
    {
        /// <summary>
        /// The connection.
        /// </summary>
        private readonly OtpConnection otpConnection;

        /// <summary>Initializes a new instance of the <see cref="DefaultConnection"/> class.</summary>
        /// <param name="otpConnection">The otp connection.</param>
        public DefaultConnection(OtpConnection otpConnection) { this.otpConnection = otpConnection; }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close() { this.otpConnection.close(); }

        /// <summary>Sends the RPC.</summary>
        /// <param name="mod">The mod.</param>
        /// <param name="fun">The fun.</param>
        /// <param name="args">The args.</param>
        public void SendRPC(string mod, string fun, OtpErlangList args) { this.otpConnection.sendRPC(mod, fun, args); }

        /// <summary>
        /// Receives the RPC.
        /// </summary>
        /// <returns>The second element of the tuple if the received message is a two-tuple, otherwise null. No further error checking is performed.</returns>
        public OtpErlangObject ReceiveRPC() { return this.otpConnection.receiveRPC(); }

        /// <summary>
        /// Gets the target connection.
        /// </summary>
        /// <returns>The connection.</returns>
        public OtpConnection GetTargetConnection() { return this.otpConnection; }
    }
}
