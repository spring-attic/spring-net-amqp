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
    public class CloseSuppressingConnection : IDecoratorConnection
    {
        private IConnection target;
        private CachingConnectionFactory cachingConnectionFactory;

        public CloseSuppressingConnection(CachingConnectionFactory factory, IConnection connection)
        {
            this.target = connection;
            this.cachingConnectionFactory = factory;
        }

        public IConnection TargetConnection
        {
            get { return target; }
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            target.Dispose();            
        }

        #endregion

        #region Implementation of IConnection

        public IModel CreateModel()
        {
            IModel model = this.cachingConnectionFactory.GetChannel(target);
            if (model != null)
            {
                return model;
            }
            return target.CreateModel();
        }

        public void Close()
        {
            // don't pass the call to the target.
        }

        public void Close(ushort reasonCode, string reasonText)
        {
            // don't pass the call to the target.
        }

        public void Close(int timeout)
        {
            // don't pass the call to the target.
        }

        public void Close(ushort reasonCode, string reasonText, int timeout)
        {
            // don't pass the call to the target.
        }

        public void Abort()
        {
            target.Abort();
        }

        public void Abort(ushort reasonCode, string reasonText)
        {
            target.Abort(reasonCode, reasonText);
        }

        public void Abort(int timeout)
        {
            target.Abort(timeout);
        }

        public void Abort(ushort reasonCode, string reasonText, int timeout)
        {
            target.Abort(reasonCode, reasonText, timeout);
        }

        public AmqpTcpEndpoint Endpoint
        {
            get { return target.Endpoint; }
        }

        public IProtocol Protocol
        {
            get { return target.Protocol; }
        }

        public ConnectionParameters Parameters
        {
            get { return target.Parameters; }
        }

        public ushort ChannelMax
        {
            get { return target.ChannelMax; }
        }

        public uint FrameMax
        {
            get { return target.FrameMax; }
        }

        public ushort Heartbeat
        {
            get { return target.Heartbeat; }
        }

        public AmqpTcpEndpoint[] KnownHosts
        {
            get { return target.KnownHosts; }
        }

        public ShutdownEventArgs CloseReason
        {
            get { return target.CloseReason; }
        }

        public bool IsOpen
        {
            get { return target.IsOpen; }
        }

        public bool AutoClose
        {
            get { return target.AutoClose; }
            set { target.AutoClose = value; }
        }

        public IList ShutdownReport
        {
            get { return target.ShutdownReport; }
        }

        public event ConnectionShutdownEventHandler ConnectionShutdown
        {
            add
            {
                target.ConnectionShutdown += value;
            }
            remove
            {
                target.ConnectionShutdown -= value;
            }
        }
        public event CallbackExceptionEventHandler CallbackException
        {
            add
            {
                target.CallbackException += value;
            }
            remove
            {
                target.CallbackException -= value;
            }
        }

        #endregion
    }

}