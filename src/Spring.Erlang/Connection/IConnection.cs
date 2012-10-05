// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConnection.cs" company="The original author or authors.">
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
    /// A simple interface that is used to wrap access to the OtpConnection class in order to support
    /// caching of OptConnections via method interception.
    /// Note:  The surface area of the API is all that is required to implement administrative functionality
    /// for the Spring AMQP admin project.  To access the underlying OtpConnection, use the method GetTargetConnection
    /// on the interface ConnectionProxy that is implemented by DefaultConnection.
    /// </summary>
    /// <remarks></remarks>
    public interface IConnection
    {
        /// <summary>
        /// Closes this instance.
        /// </summary>
        /// <remarks>Close the connection to the remote node.</remarks>
        void Close();

        /**
         * Send an RPC request to the remote Erlang node. This convenience function
         * creates the following message and sends it to 'rex' on the remote node:
         * 
         * <pre>
         * { self, { call, Mod, Fun, Args, user } }
         * </pre>
         * 
         * <p>
         * Note that this method has unpredicatble results if the remote node is not
         * an Erlang node.
         * </p>
         * 
         * @param mod
         *                the name of the Erlang module containing the function to
         *                be called.
         * @param fun
         *                the name of the function to call.
         * @param args
         *                a list of Erlang terms, to be used as arguments to the
         *                function.
         * 
         * @exception java.io.IOException
         *                    if the connection is not active or a communication
         *                    error occurs.
         */

        /// <summary>Sends the RPC.</summary>
        /// <param name="mod">The mod.</param>
        /// <param name="fun">The fun.</param>
        /// <param name="args">The args.</param>
        /// <remarks></remarks>
        void SendRPC(string mod, string fun, OtpErlangList args);

        /**
         * Receive an RPC reply from the remote Erlang node. This convenience
         * function receives a message from the remote node, and expects it to have
         * the following format:
         * 
         * <pre>
         * { rex, Term }
         * </pre>
         * 
         * @return the second element of the tuple if the received message is a
         *         two-tuple, otherwise null. No further error checking is
         *         performed.
         * 
         * @exception java.io.IOException
         *                    if the connection is not active or a communication
         *                    error occurs.
         * 
         * @exception OtpErlangExit
         *                    if an exit signal is received from a process on the
         *                    peer node.
         * 
         * @exception OtpAuthException
         *                    if the remote node sends a message containing an
         *                    invalid cookie.
         */

        /// <summary>
        /// Receives the RPC.
        /// </summary>
        /// <returns>The second element of the tuple if the received message is a two-tuple, otherwise null. No further error checking is performed.</returns>
        /// <remarks></remarks>
        OtpErlangObject ReceiveRPC();
    }
}
