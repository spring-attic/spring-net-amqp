// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPublisherCallbackChannelListener.cs" company="The original author or authors.">
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
using System.Collections.Generic;
using RabbitMQ.Client;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// Listeners implementing this interface can participate
    /// in publisher confirms received from multiple channels,
    /// by invoking addListener on each channel. Standard
    /// AMQP channels do not support a listener being
    /// registered on multiple channels.
    /// </summary>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public interface IPublisherCallbackChannelListener
    {
        /// <summary>Invoked by the channel when a confirm is received.</summary>
        /// <param name="pendingConfirm">The pending confirmation, containing correlation data.</param>
        /// <param name="ack">The ack. True when 'ack', false when 'nack'.</param>
        void HandleConfirm(PendingConfirm pendingConfirm, bool ack);

        /// <summary>The handle return.</summary>
        /// <param name="replyCode">The reply code.</param>
        /// <param name="replyText">The reply text.</param>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="body">The body.</param>
        void HandleReturn(
            int replyCode, 
            string replyText, 
            string exchange, 
            string routingKey, 
            IBasicProperties properties, 
            byte[] body);

        /// <summary>When called, this listener must remove all references to the pending confirm map.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="unconfirmed">The pending confirm map.</param>
        void RemovePendingConfirmsReference(IModel channel, SortedDictionary<long, PendingConfirm> unconfirmed);

        /// <summary>Returns the UUID used to identify this Listener for returns.</summary>
        string Uuid { get; }

        /// <summary>Gets a value indicating whether is confirm listener.</summary>
        bool IsConfirmListener { get; }

        /// <summary>Gets a value indicating whether is return listener.</summary>
        bool IsReturnListener { get; }
    }
}
