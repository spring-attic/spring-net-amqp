// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleConnection.cs" company="The original author or authors.">
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
using RabbitMQ.Client;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A Simple Connection implementation.
    /// </summary>
    /// <author>Joe Fitzgerald</author>
    public class SimpleConnection : IConnection
    {
        /// <summary>
        /// The connection delegate.
        /// </summary>
        private readonly RabbitMQ.Client.IConnection connectionDelegate;

        /// <summary>Initializes a new instance of the <see cref="SimpleConnection"/> class.</summary>
        /// <param name="connectionDelegate">The connection delegate.</param>
        public SimpleConnection(RabbitMQ.Client.IConnection connectionDelegate) { this.connectionDelegate = connectionDelegate; }

        /// <summary>Create a channel, given a flag indicating whether it should be transactional or not.</summary>
        /// <param name="transactional">The transactional.</param>
        /// <returns>The channel.</returns>
        public IModel CreateChannel(bool transactional)
        {
            try
            {
                var channel = this.connectionDelegate.CreateModel();
                if (transactional)
                {
                    // Just created so we want to start the transaction
                    channel.TxSelect();
                }

                return channel;
            }
            catch (Exception e)
            {
                throw RabbitUtils.ConvertRabbitAccessException(e);
            }
        }

        /// <summary>
        /// Close the channel.
        /// </summary>
        public void Close()
        {
            try
            {
                this.connectionDelegate.Close();
            }
            catch (Exception e)
            {
                throw RabbitUtils.ConvertRabbitAccessException(e);
            }
        }

        /// <summary>
        /// Determine if the channel is open.
        /// </summary>
        /// <returns>
        /// True if open, else false.
        /// </returns>
        public bool IsOpen() { return this.connectionDelegate != null && this.connectionDelegate.IsOpen; }
    }
}
