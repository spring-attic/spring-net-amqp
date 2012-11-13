// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErlangUtils.cs" company="The original author or authors.">
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
using Spring.Util;
#endregion

namespace Spring.Erlang.Support
{
    /// <summary>
    /// Erlang Utilities.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class ErlangUtils
    {
        /// <summary>Releases the connection.</summary>
        /// <param name="con">The con.</param>
        public static void ReleaseConnection(OtpConnection con)
        {
            if (con == null)
            {
                return;
            }

            con.close();
        }

        /// <summary>Converts the otp access exception.</summary>
        /// <param name="ex">The ex.</param>
        /// <returns>The exception.</returns>
        public static OtpException ConvertOtpAccessException(Exception ex)
        {
            AssertUtils.ArgumentNotNull(ex, "Exception must not be null");

            // TODO: preserve stack strace
            return new UncategorizedOtpException(ex.Message);
        }
    }
}
