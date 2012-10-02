// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitAccessor.cs" company="The original author or authors.">
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
using Common.Logging;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Objects.Factory;
using Spring.Util;
using IConnection = Spring.Messaging.Amqp.Rabbit.Connection.IConnection;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// Base implementation of a RabbitAccessor.
    /// </summary>
    /// <author>Mark Pollack</author>
    public abstract class RabbitAccessor : IInitializingObject
    {
        /// <summary>
        /// Logger available to subclasses.
        /// </summary>
        protected readonly ILog logger = LogManager.GetLogger(typeof(RabbitAccessor));

        /// <summary>
        /// The connection factory.
        /// </summary>
        private IConnectionFactory connectionFactory;

        /// <summary>
        /// The transactional flag.
        /// </summary>
        private bool transactional;

        /// <summary>
        /// Gets or sets ConnectionFactory.
        /// </summary>
        public IConnectionFactory ConnectionFactory { get { return this.connectionFactory; } set { this.connectionFactory = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether ChannelTransacted.
        /// </summary>
        public bool ChannelTransacted { get { return this.transactional; } set { this.transactional = value; } }

        #region Implementation of IInitializingObject

        /// <summary>
        /// Runs after properties are set.
        /// </summary>
        public virtual void AfterPropertiesSet() { AssertUtils.ArgumentNotNull(this.ConnectionFactory, "ConnectionFactory is required"); }
        #endregion

        /// <summary>
        /// Create a RabbitMQ Connection via this template's ConnectionFactory and its host and port values.
        /// </summary>
        /// <returns>
        /// The connection.
        /// </returns>
        protected IConnection CreateConnection() { return this.ConnectionFactory.CreateConnection(); }

        /// <summary>Fetch an appropriate Connection from the given RabbitResourceHolder.</summary>
        /// <param name="holder">The holder.</param>
        /// <returns>The connection.</returns>
        protected IConnection GetConnection(RabbitResourceHolder holder) { return holder.Connection; }

        /// <summary>Create the channel.</summary>
        /// <param name="holder">The rabbit resource holder.</param>
        /// <returns>The channel.</returns>
        protected IModel GetChannel(RabbitResourceHolder holder) { return holder.Channel; }

        /// <summary>
        /// Get a transactional resource holder.
        /// </summary>
        /// <returns>
        /// The rabbit resource holder.
        /// </returns>
        protected RabbitResourceHolder GetTransactionalResourceHolder() { return ConnectionFactoryUtils.GetTransactionalResourceHolder(this.connectionFactory, this.ChannelTransacted); }

        /// <summary>Converts a rabbit access exception.</summary>
        /// <param name="ex">The ex.</param>
        /// <returns>The system exception.</returns>
        protected SystemException ConvertRabbitAccessException(Exception ex) { return RabbitUtils.ConvertRabbitAccessException(ex); }
    }
}
