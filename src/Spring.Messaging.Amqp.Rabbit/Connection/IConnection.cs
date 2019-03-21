// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConnection.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using RabbitMQ.Client;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// An interface for connections.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public interface IConnection
    {
        /// <summary>Create a new channel, using an internally allocated channel number.</summary>
        /// <param name="transactional">Transactional true if the channel should support transactions.</param>
        /// <returns>A new channel descriptor, or null if none is available.</returns>
        IModel CreateChannel(bool transactional);

        /// <summary>
        /// Close this connection and all its channels with the {@link com.rabbitmq.client.AMQP#REPLY_SUCCESS} close code and message 'OK'.
        /// Waits for all the close operations to complete.
        /// </summary>
        void Close();

        /// <summary>
        /// Flag to indicate the status of the connection.
        /// </summary>
        /// <returns>
        /// True if the connection is open
        /// </returns>
        bool IsOpen();
    }
}
