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
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Objects.Factory;

namespace Spring.Messaging.Amqp.Rabbit.Core.Support
{
    /// <summary>
    /// Convenient super class for application classes that need Rabbit access.
    /// </summary>
    /// <remarks>
    ///  Requires a ConnectionFactory or a RabbitTemplate instance to be set.
    ///  It will create its own RabbitTemplate if a ConnectionFactory is passed in.
    ///  A custom RabbitTemplate instance can be created for a given ConnectionFactory
    ///  through overriding the <code>createNmsTemplate</code> method.
    /// </remarks>
    /// <author>Mark Pollack</author>
    public class RabbitGatewaySupport : IInitializingObject
    {

        #region Logging

        private readonly ILog logger = LogManager.GetLogger(typeof(RabbitGatewaySupport));

        #endregion

        private RabbitTemplate rabbitTemplate;

        /// <summary>
        /// Gets or sets the Rabbit template for the gateway.
        /// </summary>
        /// <value>The Tabbity template.</value>
        public RabbitTemplate RabbitTemplate
        {
            get { return rabbitTemplate; }
            set { rabbitTemplate = value; }
        }

        /// <summary>
        /// Gets or sets he NMS connection factory to be used by the gateway.
        /// Will automatically create a NmsTemplate for the given ConnectionFactory.
        /// </summary>
        /// <value>The connection factory.</value>
        public IConnectionFactory ConnectionFactory
        {
            get
            {
                return (rabbitTemplate != null ? this.rabbitTemplate.ConnectionFactory : null);
            }
            set
            {
                this.rabbitTemplate = CreateRabbitTemplate(value);
            }
        }

        /// <summary>
        /// Creates a RabbitTemplate for the given ConnectionFactory.
        /// </summary>
        /// <remarks>Only invoked if populating the gateway with a ConnectionFactory reference.
        /// Can be overridden in subclasses to provide a different RabbitTemplate instance
        /// </remarks>
        ///
        /// <param name="connectionFactory">The connection factory.</param>
        /// <returns></returns>
        protected virtual RabbitTemplate CreateRabbitTemplate(IConnectionFactory connectionFactory)
        {
            return new RabbitTemplate(connectionFactory);
        }

        #region Implementation of IInitializingObject

        /// <summary>
        /// Ensures that the Rabbit Template is specified and calls <see cref="InitGateway"/>.
        /// </summary>
        public void AfterPropertiesSet()
        {
            if (rabbitTemplate == null)
            {
                throw new ArgumentException("connectionFactory or rabbitTemplate is required");
            }
            try
            {
                InitGateway();
            }
            catch (Exception e)
            {
                throw new ObjectInitializationException("Initialization of the Rabbit gateway failed: " + e.Message, e);
            }
        }

        #endregion

        /// <summary>
        /// Subclasses can override this for custom initialization behavior.
        /// Gets called after population of this instance's properties.
        /// </summary>
        protected virtual void InitGateway()
        {

        }
    }
}