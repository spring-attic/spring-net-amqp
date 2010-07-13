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

#region

using System;
using Erlang.NET;
using Spring.Objects.Factory;
using Spring.Util;

#endregion

namespace Spring.Erlang.Connection
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class SimpleConnectionFactory : IConnectionFactory, IInitializingObject
    {
        private string selfNodeName;

        private string cookie;

        private string peerNodeName;

        private OtpSelf otpSelf;

        private OtpPeer otpPeer;

        public SimpleConnectionFactory(String selfNodeName, String cookie, String peerNodeName)
        {
            this.selfNodeName = selfNodeName;
            this.cookie = cookie;
            this.peerNodeName = peerNodeName;
        }

        public SimpleConnectionFactory(String selfNodeName, String peerNodeName)
        {
            this.selfNodeName = selfNodeName;
            this.peerNodeName = peerNodeName;
        }

        #region Implementation of IConnectionFactory

        public OtpConnection CreateConnection()
        {
            return otpSelf.connect(otpPeer);
        }

        #endregion

        #region Implementation of IInitializingObject

        public void AfterPropertiesSet()
        {
            AssertUtils.IsTrue(this.selfNodeName != null || this.peerNodeName != null,
                               "'selfNodeName' or 'peerNodeName' is required");
            if (this.cookie == null)
            {
                this.otpSelf = new OtpSelf(this.selfNodeName);
            }
            else
            {
                this.otpSelf = new OtpSelf(this.selfNodeName, this.cookie);
            }

            this.otpPeer = new OtpPeer(this.peerNodeName);
        }

        #endregion
    }
}