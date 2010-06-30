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

using log4net;
using org.apache.qpid.client;
using Spring.Objects.Factory;
using Spring.Util;

namespace Spring.Messaging.Amqp.Qpid.Core
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class QpidAccessor : IInitializingObject
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(QpidAccessor));


        private volatile IClientFactory clientFactory;

        private volatile bool channelTransacted;

        public IClientFactory ClientFactory
        {
            get { return clientFactory; }
            set { clientFactory = value; }
        }

        public bool ChannelTransacted
        {
            get { return channelTransacted; }
            set { channelTransacted = value; }
        }

        #region Implementation of IInitializingObject

        public virtual void AfterPropertiesSet()
        {
            AssertUtils.ArgumentNotNull(ClientFactory, "ClientFactory is required");
        }

        #endregion

        protected IClient CreateClient()
        {
            return ClientFactory.CreateClient();
        }

        protected IClientSession CreateChannel(IClient client)
        {
            AssertUtils.ArgumentNotNull(client, "connection must not be null");
            //TODO configure timeout.
            IClientSession session = client.CreateSession(50000);
            if (ChannelTransacted)
            {
                session.TxSelect();
            }
            return session;
        }
        
    }
}