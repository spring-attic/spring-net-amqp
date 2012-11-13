// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErlangTemplate.cs" company="The original author or authors.">
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
using Erlang.NET;
using Spring.Erlang.Connection;
using Spring.Erlang.Support;
using Spring.Erlang.Support.Converter;
using Spring.Util;
#endregion

namespace Spring.Erlang.Core
{
    /// <summary>
    /// An Erlang Template.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class ErlangTemplate : ErlangAccessor, IErlangOperations
    {
        /// <summary>
        /// The erlang converter.
        /// </summary>
        private volatile IErlangConverter erlangConverter = new SimpleErlangConverter();

        /// <summary>Initializes a new instance of the <see cref="ErlangTemplate"/> class.</summary>
        /// <param name="connectionFactory">The connection factory.</param>
        public ErlangTemplate(IConnectionFactory connectionFactory)
        {
            this.ConnectionFactory = connectionFactory;
            this.AfterPropertiesSet();
        }

        /// <summary>
        /// Gets or sets the erlang converter.
        /// </summary>
        /// <value>The erlang converter.</value>
        public IErlangConverter ErlangConverter { get { return this.erlangConverter; } set { this.erlangConverter = value; } }

        /// <summary>Executes the erlang RPC.</summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="args">The args.</param>
        /// <returns>The OtpErlangObject.</returns>
        public OtpErlangObject ExecuteErlangRpc(string module, string function, OtpErlangList args)
        {
            return this.Execute(
                delegate(IConnection connection)
                {
                    Logger.Debug("Sending RPC for module [" + module + "] function [" + function + "] args [" + args);
                    connection.SendRPC(module, function, args);
                    var response = connection.ReceiveRPC();
                    Logger.Debug("Response received = " + response);
                    this.HandleResponseError(module, function, response);
                    return response;
                });
        }

        /// <summary>Handles the response error.</summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="result">The result.</param>
        public void HandleResponseError(string module, string function, OtpErlangObject result)
        {
            /* {badrpc,{'EXIT',{undef,[{rabbit_access_control,list_users,[[]]},{rpc,'-handle_call/3-fun-0-',5}]}}} */
            if (result is OtpErlangTuple)
            {
                var msg = (OtpErlangTuple)result;
                if (msg.elementAt(0) is OtpErlangAtom)
                {
                    var responseAtom = (OtpErlangAtom)msg.elementAt(0);

                    // TODO: consider error handler strategy.
                    if (responseAtom.atomValue().Equals("badrpc"))
                    {
                        if (msg.elementAt(1) is OtpErlangTuple)
                        {
                            throw new ErlangBadRpcException((OtpErlangTuple)msg.elementAt(1));
                        }
                        else
                        {
                            throw new ErlangBadRpcException(msg.elementAt(1).ToString());
                        }
                    }
                    else if (responseAtom.atomValue().Equals("error"))
                    {
                        if (msg.elementAt(1) is OtpErlangTuple)
                        {
                            throw new ErlangErrorRpcException((OtpErlangTuple)msg.elementAt(1));
                        }
                        else
                        {
                            throw new ErlangErrorRpcException(msg.elementAt(1).ToString());
                        }
                    }
                }
            }
        }

        /// <summary>Executes the erlang RPC.</summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="args">The args.</param>
        /// <returns>The OtpErlangObject.</returns>
        public OtpErlangObject ExecuteErlangRpc(string module, string function, params OtpErlangObject[] args) { return this.ExecuteRpc(module, function, new OtpErlangList(args)); }

        /// <summary>Executes the RPC.</summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="args">The args.</param>
        /// <returns>The OtpErlangObject.</returns>
        public OtpErlangObject ExecuteRpc(string module, string function, params object[] args) { return this.ExecuteErlangRpc(module, function, (OtpErlangList)this.erlangConverter.ToErlang(args)); }

        /// <summary>Executes the and convert RPC.</summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="converterToUse">The converter to use.</param>
        /// <param name="args">The args.</param>
        /// <returns>The OtpErlangObject.</returns>
        public object ExecuteAndConvertRpc(string module, string function, IErlangConverter converterToUse, params object[] args) { return converterToUse.FromErlang(this.ExecuteRpc(module, function, converterToUse.ToErlang(args))); }

        /// <summary>Executes the and convert RPC.</summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="args">The args.</param>
        /// <returns>The object.</returns>
        public object ExecuteAndConvertRpc(string module, string function, params object[] args) { return this.erlangConverter.FromErlangRpc(module, function, this.ExecuteErlangRpc(module, function, (OtpErlangList)this.erlangConverter.ToErlang(args))); }

        /// <summary>Executes the specified action.</summary>
        /// <typeparam name="T">Type T.</typeparam>
        /// <param name="action">The action.</param>
        /// <returns>Object of Type T.</returns>
        public T Execute<T>(ConnectionCallbackDelegate<T> action)
        {
            AssertUtils.ArgumentNotNull(action, "Callback object must not be null");
            IConnection con = null;
            try
            {
                con = this.CreateConnection();
                return action(con);
            }
            catch (OtpException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred executing the action", ex);
                throw this.ConvertOtpAccessException(ex);
            }
            finally
            {
                ConnectionFactoryUtils.ReleaseConnection(con, this.ConnectionFactory);
            }

            // TODO: physically close and reopen the connection if there is an exception
        }

        /// <summary>Converts the otp access exception.</summary>
        /// <param name="ex">The ex.</param>
        /// <returns>The Erlang.NET.OtpException.</returns>
        protected OtpException ConvertOtpAccessException(Exception ex) { return ErlangUtils.ConvertOtpAccessException(ex); }
    }
}
