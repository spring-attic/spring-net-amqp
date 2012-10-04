// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IAmqpAdmin.cs" company="The original author or authors.">
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

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Specifies a basic set of portable AMQP administrative operations for AMQP > 0.8
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public interface IAmqpAdmin
    {
        #region Exchange Operations

        /// <summary>Declares the exchange.</summary>
        /// <param name="exchange">The exchange.</param>
        void DeclareExchange(IExchange exchange);

        /// <summary>Deletes the exchange.</summary>
        /// <remarks>Look at implementation specific subclass for implementation specific behavior, for example
        /// for RabbitMQ this will delete the exchange without regard for whether it is in use or not.</remarks>
        /// <param name="exchangeName">Name of the exchange.</param>
        /// <returns>true if the exchange existed and was deleted</returns>
        bool DeleteExchange(string exchangeName);
        #endregion

        #region Queue Operations

        /// <summary>
        /// Declares a queue whose name is automatically named by the server.  It is created with
        /// exclusive = true, autoDelete=true, and durable = false.
        /// </summary>
        /// <returns>The queue.</returns>
        Queue DeclareQueue();

        /// <summary>Declares the given queue.</summary>
        /// <param name="queue">The queue to declare.</param>
        void DeclareQueue(Queue queue);

        /// <summary>Deletes the queue, without regard for whether it is in use or has messages on it </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <returns>true if the queue existed and was deleted.</returns>
        bool DeleteQueue(string queueName);
        
        /// <summary>Deletes the queue.</summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="unused">if set to <c>true</c> the queue should be deleted only if not in use.</param>
        /// <param name="empty">if set to <c>true</c> the queue should be deleted only if empty.</param>
        void DeleteQueue(string queueName, bool unused, bool empty);

        /// <summary>Purges the queue.</summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="noWait">if set to <c>true</c> [no wait].</param>
        void PurgeQueue(string queueName, bool noWait);
        #endregion

        #region Binding operations

        /// <summary>Declare a binding of a queue to an exchange.</summary>
        /// <param name="binding">Binding to declare.</param>
        void DeclareBinding(Binding binding);

        /// <summary>Remove a binding of a queue to an exchange. Note unbindQueue/removeBinding was not introduced until 0.9 of the
        /// specification.</summary>
        /// <param name="binding">Binding to remove.</param>
        void RemoveBinding(Binding binding);
        #endregion
    }
}
