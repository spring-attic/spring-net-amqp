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
using System.Collections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class TestConnection : IConnection
    {
        private int closeCount;
        private int createModelCount;

        public int CloseCount
        {
            get { return closeCount; }
        }

        public int CreateModelCount
        {
            get { return createModelCount; }
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation of IConnection

        public IModel CreateModel()
        {
            createModelCount++;
            return new TestModel();
        }

        public void Close()
        {
            closeCount++;
        }

        public void Close(ushort reasonCode, string reasonText)
        {
            closeCount++;
        }

        public void Close(int timeout)
        {
            closeCount++;
        }

        public void Close(ushort reasonCode, string reasonText, int timeout)
        {
            closeCount++;
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public void Abort(ushort reasonCode, string reasonText)
        {
            throw new NotImplementedException();
        }

        public void Abort(int timeout)
        {
            throw new NotImplementedException();
        }

        public void Abort(ushort reasonCode, string reasonText, int timeout)
        {
            throw new NotImplementedException();
        }

        public AmqpTcpEndpoint Endpoint
        {
            get { throw new NotImplementedException(); }
        }

        public IProtocol Protocol
        {
            get { throw new NotImplementedException(); }
        }

        public ushort ChannelMax
        {
            get { throw new NotImplementedException(); }
        }

        public uint FrameMax
        {
            get { throw new NotImplementedException(); }
        }

        public ushort Heartbeat
        {
            get { throw new NotImplementedException(); }
        }

        public IDictionary ClientProperties
        {
            get { throw new NotImplementedException(); }
        }

        public IDictionary ServerProperties
        {
            get { throw new NotImplementedException(); }
        }

        public AmqpTcpEndpoint[] KnownHosts
        {
            get { throw new NotImplementedException(); }
        }

        public ShutdownEventArgs CloseReason
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsOpen
        {
            get { throw new NotImplementedException(); }
        }

        public bool AutoClose
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IList ShutdownReport
        {
            get { throw new NotImplementedException(); }
        }

        public event ConnectionShutdownEventHandler ConnectionShutdown;
        public event CallbackExceptionEventHandler CallbackException;

        #endregion
    }

}