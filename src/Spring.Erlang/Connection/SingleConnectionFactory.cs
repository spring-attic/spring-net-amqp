// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SingleConnectionFactory.cs" company="The original author or authors.">
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
using System.IO;
using AopAlliance.Intercept;
using Common.Logging;
using Erlang.NET;
using Spring.Aop.Framework;
using Spring.Objects.Factory;
using Spring.Util;
#endregion

namespace Spring.Erlang.Connection
{
    /// <summary>
    /// A single connection factory.
    /// </summary>
    public class SingleConnectionFactory : IConnectionFactory, IInitializingObject, IDisposable
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        protected static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The unique self node name.
        /// </summary>
        private bool uniqueSelfNodeName = true;

        /// <summary>
        /// The self node name.
        /// </summary>
        private readonly string selfNodeName;

        /// <summary>
        /// The cookie.
        /// </summary>
        private readonly string cookie;

        /// <summary>
        /// The peer node name.
        /// </summary>
        private string peerNodeName;

        /// <summary>
        /// The otp self.
        /// </summary>
        private OtpSelf otpSelf;

        /// <summary>
        /// The otp peer.
        /// </summary>
        private OtpPeer otpPeer;

        /// <summary>
        /// Raw JInterface Connection
        /// </summary>
        private IConnection targetConnection;

        /// <summary>
        /// Proxy Connection.
        /// </summary> 
        private IConnection connection;

        /// <summary>
        /// Synchronization monitor for the shared Connection.
        /// </summary>
        private readonly object connectionMonitor = new object();

        /// <summary>Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.</summary>
        /// <param name="selfNodeName">Name of the self node.</param>
        /// <param name="cookie">The cookie.</param>
        /// <param name="peerNodeName">Name of the peer node.</param>
        public SingleConnectionFactory(string selfNodeName, string cookie, string peerNodeName)
        {
            this.selfNodeName = selfNodeName;
            this.cookie = cookie;
            this.peerNodeName = peerNodeName;
        }

        /// <summary>Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.</summary>
        /// <param name="selfNodeName">Name of the self node.</param>
        /// <param name="peerNodeName">Name of the peer node.</param>
        public SingleConnectionFactory(string selfNodeName, string peerNodeName)
        {
            this.selfNodeName = selfNodeName;
            this.peerNodeName = peerNodeName;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [unique self node name].
        /// </summary>
        /// <value><c>true</c> if [unique self node name]; otherwise, <c>false</c>.</value>
        public bool UniqueSelfNodeName { get { return this.uniqueSelfNodeName; } set { this.uniqueSelfNodeName = value; } }

        #region Implementation of IConnectionFactory

        /// <summary>
        /// Creates the connection.
        /// </summary>
        /// <returns>The connection.</returns>
        public IConnection CreateConnection()
        {
            lock (this.connectionMonitor)
            {
                if (this.connection == null)
                {
                    try
                    {
                        this.InitConnection();
                    }
                    catch (IOException e)
                    {
                        throw new OtpIOException("failed to connect from '" + this.selfNodeName + "' to peer node '" + this.peerNodeName + "'", e);
                    }
                }

                return this.connection;
            }
        }

        /// <summary>
        /// Inits the connection.
        /// </summary>
        public void InitConnection()
        {
            lock (this.connectionMonitor)
            {
                if (this.targetConnection != null)
                {
                    this.CloseConnection(this.targetConnection);
                }

                this.targetConnection = this.DoCreateConnection();
                this.PrepareConnection(this.targetConnection);
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info(
                        "Established shared Rabbit Connection: "
                        + this.targetConnection);
                }

                this.connection = this.GetSharedConnectionProxy(this.targetConnection);
            }
        }

        /// <summary>
        /// Resets the connection.
        /// </summary>
        /// Reset the underlying shared Connection, to be reinitialized on next access.
        public void ResetConnection()
        {
            lock (this.connectionMonitor)
            {
                if (this.targetConnection != null)
                {
                    this.CloseConnection(this.targetConnection);
                }

                this.targetConnection = null;
                this.connection = null;
            }
        }

        /// <summary>Closes the connection.</summary>
        /// <param name="connection">The connection.</param>
        /// Close the given Connection.
        /// @param connection
        /// the Connection to close
        protected void CloseConnection(IConnection connection)
        {
            if (Logger.IsDebugEnabled)
            {
                Logger.Debug("Closing shared Rabbit Connection: " + this.targetConnection);
            }

            try
            {
                connection.Close();
            }
            catch (Exception ex)
            {
                Logger.Debug("Could not close shared Rabbit Connection", ex);
            }
        }

        /// <summary>
        /// Does the create connection.
        /// </summary>
        /// <returns>The connection.</returns>
        /// Create a JInterface Connection via this class's ConnectionFactory.
        /// @return the new Otp Connection
        /// @throws OtpAuthException
        protected IConnection DoCreateConnection() { return new DefaultConnection(this.otpSelf.connect(this.otpPeer)); }

        /// <summary>Prepares the connection.</summary>
        /// <param name="con">The con.</param>
        protected virtual void PrepareConnection(IConnection con) { }

        /// <summary>Gets the shared connection proxy.</summary>
        /// <param name="target">The target.</param>
        /// <returns>The connection proxy.</returns>
        /// Wrap the given OtpConnection with a proxy that delegates every method
        /// call to it but suppresses close calls. This is useful for allowing
        /// application code to handle a special framework Connection just like an
        /// ordinary Connection from a Rabbit ConnectionFactory.
        /// @param target
        /// the original Connection to wrap
        /// @return the wrapped Connection
        protected IConnection GetSharedConnectionProxy(IConnection target)
        {
            /*//var classes = new List<string>(1) { typeof(IConnection).Name };
            //var connectionProxy = new ProxyFactoryObject()
            //                 {
            //                     ProxyInterfaces = classes.ToArray(),
            //                     Target = target,
            //                     InterceptorNames = new string[1]
            //                                            {
            //                                                typeof(SharedConnectionInvocationHandler).Name
            //                                            }
            //                 };*/
            return (IConnection)ProxyFactory.GetProxy(typeof(IConnection), new SharedConnectionInvocationHandler(target));

            /*factory.GetObject();
            builder = new Proxy.CompositionProxyTypeBuilder();
            builder.
            return (IConnection)
                .newProxyInstance(
                    Connection.)class.getClassLoader(),
                    classes.toArray(new Class[classes.size()]),
                    new SharedConnectionInvocationHandler(target));*/
        }

        #endregion

        #region Implementation of IInitializingObject

        /// <summary>
        /// Afters the properties set.
        /// </summary>
        public void AfterPropertiesSet()
        {
            AssertUtils.IsTrue(this.selfNodeName != null || this.peerNodeName != null, "'selfNodeName' or 'peerNodeName' is required");
            var selfNodeNameToUse = string.IsNullOrEmpty(this.selfNodeName) ? string.Empty : this.selfNodeName;
            if (this.UniqueSelfNodeName)
            {
                selfNodeNameToUse = this.selfNodeName + "-" + Guid.NewGuid().ToString();
                Logger.Debug("Creating OtpSelf with node name = [" + selfNodeNameToUse + "]");
            }

            try
            {
                if (this.cookie == null)
                {
                    this.otpSelf = new OtpSelf(selfNodeNameToUse.Trim());
                }
                else
                {
                    this.otpSelf = new OtpSelf(selfNodeNameToUse.Trim(), this.cookie);
                }
            }
            catch (IOException e)
            {
                throw new OtpIOException(e);
            }

            this.peerNodeName = string.IsNullOrEmpty(this.peerNodeName) ? string.Empty : this.peerNodeName;
            this.otpPeer = new OtpPeer(this.peerNodeName.Trim());
        }

        #endregion

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose() { this.ResetConnection(); }
        #endregion
    }

    /// <summary>
    /// A shared connection invocation handler.
    /// </summary>
    internal class SharedConnectionInvocationHandler : IMethodInterceptor
    {
        /// <summary>
        /// The target.
        /// </summary>
        private readonly IConnection target;

        /// <summary>Initializes a new instance of the <see cref="SharedConnectionInvocationHandler"/> class.</summary>
        /// <param name="target">The target.</param>
        public SharedConnectionInvocationHandler(IConnection target) { this.target = target; }

        /// <summary>Invokes the specified mi.</summary>
        /// <param name="mi">The mi.</param>
        /// <returns>The object.</returns>
        public object Invoke(IMethodInvocation mi)
        {
            if (mi.Method.Name.Equals("equals"))
            {
                // Only consider equal when proxies are identical.
                return mi.Proxy == mi.Arguments[0];
            }
            else if (mi.Method.Name.Equals("GetHashCode"))
            {
                // Use hashCode of Connection proxy.
                return mi.Proxy.GetHashCode();
            }
            else if (mi.Method.Name.Equals("ToString"))
            {
                return "Shared Otp Connection: " + this.target;
            }
            else if (mi.Method.Name.Equals("Close"))
            {
                // Handle close method: don't pass the call on.
                return null;
            }

            try
            {
                return mi.Method.Invoke(this.target, mi.Arguments);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
