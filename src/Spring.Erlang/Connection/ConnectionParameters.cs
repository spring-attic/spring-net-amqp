// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionParameters.cs" company="The original author or authors.">
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
using Spring.Util;
#endregion

namespace Spring.Erlang.Connection
{
    /// <summary>
    /// Encapsulate properties to create a OtpConnection
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class ConnectionParameters
    {
        /// <summary>
        /// The otp self.
        /// </summary>
        private readonly OtpSelf otpSelf;

        /// <summary>
        /// The otp peer.
        /// </summary>
        private readonly OtpPeer otpPeer;

        /// <summary>Initializes a new instance of the <see cref="ConnectionParameters"/> class.</summary>
        /// <param name="otpSelf">The otp self.</param>
        /// <param name="otpPeer">The otp peer.</param>
        public ConnectionParameters(OtpSelf otpSelf, OtpPeer otpPeer)
        {
            AssertUtils.ArgumentNotNull(otpSelf, "OtpSelf must be non-null");
            AssertUtils.ArgumentNotNull(otpPeer, "OtpPeer must be non-null");
            this.otpSelf = otpSelf;
            this.otpPeer = otpPeer;
        }

        /// <summary>
        /// Gets the otp self.
        /// </summary>
        /// <returns>The otp self.</returns>
        public OtpSelf GetOtpSelf() { return this.otpSelf; }

        /// <summary>
        /// Gets the otp peer.
        /// </summary>
        /// <returns>The otp peer.</returns>
        public OtpPeer GetOtpPeer() { return this.otpPeer; }
    }
}
