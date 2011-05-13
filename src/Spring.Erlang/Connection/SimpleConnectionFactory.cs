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
using System.IO;
using Common.Logging;
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
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(SimpleConnectionFactory));

        /// <summary>
        /// The unique self node name.
        /// </summary>
        private bool uniqueSelfNodeName = true;

        /// <summary>
        /// The self node name.
        /// </summary>
        private readonly string selfNodeName;

        /// <summary>
        /// The peer node name.
        /// </summary>
        private readonly string peerNodeName;

        /// <summary>
        /// The cookie.
        /// </summary>
        private readonly string cookie;

        /// <summary>
        /// The otp self.
        /// </summary>
        private OtpSelf otpSelf;

        /// <summary>
        /// The otp peer.
        /// </summary>
        private OtpPeer otpPeer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleConnectionFactory"/> class.
        /// </summary>
        /// <param name="selfNodeName">Name of the self node.</param>
        /// <param name="peerNodeName">Name of the peer node.</param>
        /// <param name="cookie">The cookie.</param>
        /// <remarks></remarks>
        public SimpleConnectionFactory(string selfNodeName, string peerNodeName, string cookie)
        {
            this.selfNodeName = selfNodeName;
            this.peerNodeName = peerNodeName;
            this.cookie = cookie;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleConnectionFactory"/> class.
        /// </summary>
        /// <param name="selfNodeName">Name of the self node.</param>
        /// <param name="peerNodeName">Name of the peer node.</param>
        /// <remarks></remarks>
        public SimpleConnectionFactory(string selfNodeName, string peerNodeName)
        {
            this.selfNodeName = selfNodeName;
            this.peerNodeName = peerNodeName;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [unique self node name].
        /// </summary>
        /// <value><c>true</c> if [unique self node name]; otherwise, <c>false</c>.</value>
        /// <remarks></remarks>
        public bool UniqueSelfNodeName
        {
            get { return this.uniqueSelfNodeName; }
            set { this.uniqueSelfNodeName = value; }
        }

        #region Implementation of IConnectionFactory

        /// <summary>
        /// Creates the connection.
        /// </summary>
        /// <returns>The connection.</returns>
        /// <remarks></remarks>
        public IConnection CreateConnection()
        {
            try
            {
                return new DefaultConnection(this.otpSelf.connect(this.otpPeer));
            }
            catch (IOException ex)
            {
                throw new OtpIOException("failed to connect from '" + this.selfNodeName + "' to peer node '" + this.peerNodeName + "'", ex);
            }
        }

        #endregion

        #region Implementation of IInitializingObject

        /// <summary>
        /// Afters the properties set.
        /// </summary>
        /// <remarks></remarks>
        public void AfterPropertiesSet()
        {
            AssertUtils.IsTrue(this.selfNodeName != null || this.peerNodeName != null, "'selfNodeName' or 'peerNodeName' is required");

            var selfNodeNameToUse = this.selfNodeName;
            selfNodeNameToUse = string.IsNullOrEmpty(selfNodeNameToUse) ? string.Empty : selfNodeNameToUse;
            if (this.UniqueSelfNodeName)
            {
                selfNodeNameToUse = this.selfNodeName + "-" + Guid.NewGuid().ToString();
                this.logger.Debug("Creating OtpSelf with node name = [" + selfNodeNameToUse + "]");
            }

            try
            {
                if (StringUtils.HasText(this.cookie))
                {
                    this.otpSelf = new OtpSelf(selfNodeNameToUse.Trim(), this.cookie);
                }
                else
                {
                    this.otpSelf = new OtpSelf(selfNodeNameToUse.Trim());
                }
            }
            catch (IOException e)
            {
                throw new OtpIOException(e);
            }

            this.otpPeer = new OtpPeer(string.IsNullOrEmpty(this.peerNodeName) ? string.Empty : this.peerNodeName.Trim());
        }

        #endregion
    }
}