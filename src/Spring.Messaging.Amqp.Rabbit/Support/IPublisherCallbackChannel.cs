// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPublisherCallbackChannel.cs" company="The original author or authors.">
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
    /// Instances of this interface support a single publisherCallbackChannelListener being  registered for publisher confirms with multiple channels, by adding context to the callbacks.
    /// </summary>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public interface IPublisherCallbackChannel : IModel
    {
        // static string RETURN_CORRELATION = "spring_return_correlation";

        /// <summary>Adds a {@link Listener} and returns a reference to the pending confirms map for that publisherCallbackChannelListener's pending
        /// confirms, allowing the Listener to assess unconfirmed sends at any point in time.
        /// The client must <b>NOT</b> modify the contents of this array, and must synchronize on it when iterating over its collections.</summary>
        /// <param name="publisherCallbackChannelListener">The publisherCallbackChannelListener.</param>
        /// <returns>A reference to pending confirms for the publisherCallbackChannelListener. The System.Collections.Generic.SortedList`2[TKey -&gt; System.Int64, TValue -&gt; Spring.Messaging.Amqp.Rabbit.Support.PendingConfirm].</returns>
        SortedDictionary<long, PendingConfirm> AddListener(IPublisherCallbackChannelListener publisherCallbackChannelListener);

        /// <summary>The remove publisherCallbackChannelListener.</summary>
        /// <param name="publisherCallbackChannelListener">Removes the publisherCallbackChannelListener.</param>
        /// <returns>The System.Boolean.</returns>
        bool RemoveListener(IPublisherCallbackChannelListener publisherCallbackChannelListener);

        /// <summary>Adds a pending confirmation to this channel's map.</summary>
        /// <param name="publisherCallbackChannelListener">The publisherCallbackChannelListener.</param>
        /// <param name="seq">The key to the map.</param>
        /// <param name="pendingConfirm">The PendingConfirm object.</param>
        void AddPendingConfirm(IPublisherCallbackChannelListener publisherCallbackChannelListener, long seq, PendingConfirm pendingConfirm);
    }
}
