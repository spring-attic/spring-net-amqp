// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeChannelListener.cs" company="The original author or authors.">
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

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A composite channel listener.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class CompositeChannelListener : IChannelListener
    {
        /// <summary>
        /// The delegates.
        /// </summary>
        private IList<IChannelListener> delegates = new List<IChannelListener>();

        /// <summary>
        /// Gets or sets the delegates.
        /// </summary>
        /// <value>The delegates.</value>
        public IList<IChannelListener> Delegates { get { return this.delegates; } set { this.delegates = value; } }

        /// <summary>Adds the delegate.</summary>
        /// <param name="channelListener">The channel listener.</param>
        public void AddDelegate(IChannelListener channelListener) { this.delegates.Add(channelListener); }

        /// <summary>Called when [create].</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="transactional">if set to <c>true</c> [transactional].</param>
        public void OnCreate(IModel channel, bool transactional)
        {
            foreach (var item in this.delegates)
            {
                item.OnCreate(channel, transactional);
            }
        }
    }
}
