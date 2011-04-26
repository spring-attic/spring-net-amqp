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

using org.apache.qpid.client;
using Spring.Messaging.Amqp.Qpid.Core;

namespace Spring.Messaging.Amqp.Qpid.Client
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class SimpleClientFactory : IClientFactory
    {
        private ConnectionParameters connectionParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public SimpleClientFactory(ConnectionParameters connectionParameters)
        {
            this.connectionParameters = connectionParameters;
        }

        #region Implementation of IClientFactory

        /// <summary>
        /// Eagerly creates a new client.
        /// </summary>
        /// <returns></returns>
        public IClient CreateClient()
        {
            IClient client = new org.apache.qpid.client.Client();
            client.Connect(connectionParameters.Host,
                           connectionParameters.Port,
                           connectionParameters.VirtualHostName,
                           connectionParameters.Username,
                           connectionParameters.Password);
            return client;
        }

        #endregion
    }
}