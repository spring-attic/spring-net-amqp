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

using System;
using Erlang.NET;
using Spring.Erlang.Connection;
using Spring.Erlang.Support;
using Spring.Erlang.Support.Converter;
using Spring.Util;

namespace Spring.Erlang.Core
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class ErlangTemplate : ErlangAccessor, IErlangOperations
    {

        private volatile IErlangConverter erlangConverter = new SimpleErlangConverter();

        public IErlangConverter ErlangConverter
        {
            get { return erlangConverter; }
            set { erlangConverter = value; }
        }

        public ErlangTemplate(IConnectionFactory connectionFactory)
        {
            ConnectionFactory = connectionFactory;
            AfterPropertiesSet();
        }



        public T Execute<T>(ConnectionCallbackDelegate<T> action)
        {
            AssertUtils.ArgumentNotNull(action, "Callback object must not be null");
            OtpConnection con = null;
            try
            {
                con = createConnection();
                return action(con);
            }
            catch (OtpException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw ConvertOtpAccessException(ex);
            }
            finally
            {
                ConnectionFactoryUtils.ReleaseConnection(con, ConnectionFactory);
            }
        }

        protected OtpException ConvertOtpAccessException(Exception ex)
        {
            return ErlangUtils.ConvertOtpAccessException(ex);
        }

        public OtpErlangObject ExecuteErlangRpc(string module, string function, OtpErlangList args)
        {
            return Execute<OtpErlangObject>(delegate(OtpConnection connection)
                                                {
                                                    logger.Debug("Sending RPC for module [" + module + "] function [" + function + "] args [" + args);
                                                    connection.sendRPC(module, function, args);
                                                    OtpErlangObject response = connection.receiveRPC();
                                                    logger.Debug("Response received = " + response.ToString());
                                                    HandleResponseError(module, function, response);
                                                    return response;
                                                });
        }

       	public void HandleResponseError(String module, String function, OtpErlangObject result) {
		//{badrpc,{'EXIT',{undef,[{rabbit_access_control,list_users,[[]]},{rpc,'-handle_call/3-fun-0-',5}]}}}

		if (result is OtpErlangTuple) {
			OtpErlangTuple msg = (OtpErlangTuple)result;
			OtpErlangObject[] elements = msg.elements();
			if (msg.elementAt(0) is OtpErlangAtom)
			{
				OtpErlangAtom responseAtom = (OtpErlangAtom)msg.elementAt(0);
				//TODO consider error handler strategy.
				if (responseAtom.atomValue().Equals("badrpc")) {		
					if (msg.elementAt(1) is OtpErlangTuple) {
						throw new ErlangBadRpcException( (OtpErlangTuple)msg.elementAt(1));
					} else {
						throw new ErlangBadRpcException( msg.elementAt(1).ToString());
					}
				} else if (responseAtom.atomValue().Equals("error")) {
					if (msg.elementAt(1) is OtpErlangTuple) {
						throw new ErlangErrorRpcException( (OtpErlangTuple)msg.elementAt(1));
					} else {
						throw new ErlangErrorRpcException( msg.elementAt(1).ToString());
					}
				}
			}
		}
	}

        public OtpErlangObject ExecuteErlangRpc(string module, string function, params OtpErlangObject[] args)
        {
            throw new NotImplementedException();
        }

        public OtpErlangObject ExecuteRpc(string module, string function, params object[] args)
        {
            return ExecuteErlangRpc(module, function, (OtpErlangList)erlangConverter.ToErlang(args));
        }

        public object ExecuteAndConvertRpc(string module, string function, IErlangConverter converterToUse, params object[] args)
        {
            throw new NotImplementedException();
        }

        public object ExecuteAndConvertRpc(string module, string function, params object[] args)
        {
            return erlangConverter.FromErlangRpc(module, function, ExecuteErlangRpc(module, function, (OtpErlangList)erlangConverter.ToErlang(args)));
	
        }
    }

}