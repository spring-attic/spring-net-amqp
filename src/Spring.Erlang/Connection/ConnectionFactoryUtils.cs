
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
using Common.Logging;

namespace Spring.Erlang.Connection
{
    /// <summary>
    /// Connection factory utilities.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class ConnectionFactoryUtils
    {
        /// <summary>
        /// The logger.
        /// </summary>
        protected static readonly ILog logger = LogManager.GetLogger(typeof(ConnectionFactoryUtils));

        /// <summary>
        /// Releases the connection.
        /// </summary>
        /// <param name="con">The con.</param>
        /// <param name="cf">The cf.</param>
        /// <remarks></remarks>
        public static void ReleaseConnection(IConnection con, IConnectionFactory cf)
        {
            if (con == null)
            {
                return;
            }

            try
            {
                con.Close();
            }
            catch (Exception ex)
            {
                logger.Debug("Could not close Otp Connection", ex);
            }
        }
    }

}