
using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A composite channel listener.
    /// </summary>
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
        public IList<IChannelListener> Delegates
        {
            get { return this.delegates; }
            set { this.delegates = value; }
        }

        /// <summary>
        /// Adds the delegate.
        /// </summary>
        /// <param name="channelListener">The channel listener.</param>
        public void AddDelegate(IChannelListener channelListener)
        {
            this.delegates.Add(channelListener);
        }

        /// <summary>
        /// Called when [create].
        /// </summary>
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
