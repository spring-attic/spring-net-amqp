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
using log4net;
using org.apache.qpid.client;
using Spring.Messaging.Amqp.Qpid.Core;

namespace Spring.Messaging.Amqp.Qpid.Client
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class ClientFactoryUtils
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ClientFactoryUtils));

        public static void ReleaseConnection(IClient client, IClientFactory clientFactory)
        {
            if (client == null)
            {
                return;
            }
            try
            {
                client.Close();
            } catch (Exception ex)
            {
                logger.Debug("Could not close QPID Client", ex);
            }
        }

        public static bool IsChannelTransactional(IClientSession session, IClientFactory clientFactory )
        {
            //TODO implement
            return false;
        }

        public static IClientSession GetTransactionalSession(
            IClientFactory cf, IClient existingCon, bool synchedLocalTransactionAllowed)
        {
            //TODO implement
            return null;
        }

        public static IClientSession DoGetTransactionalSession(IClientFactory clientFactory, IResourceFactory resourceFactory)
        {
            //TODO implement
            return null;
        }

    }
}