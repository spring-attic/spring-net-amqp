
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Spring.Messaging.Amqp.Core;
using Spring.Objects.Factory;

using Queue = Spring.Messaging.Amqp.Core.Queue;

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A binding factory object.
    /// </summary>
    public class BindingFactoryObject : IFactoryObject
    {
        private IDictionary arguments;
        private string routingKey = string.Empty;
        private string exchange;
        private Queue destinationQueue;
        private IExchange destinationExchange;

        /// <summary>
        /// Sets the arguments.
        /// </summary>
        /// <value>The arguments.</value>
        public IDictionary Arguments
        {
            set { this.arguments = value; }
        }

        /// <summary>
        /// Sets the routing key.
        /// </summary>
        /// <value>The routing key.</value>
        public string RoutingKey
        {
            set { this.routingKey = value; }
        }

        /// <summary>
        /// Sets the exchange.
        /// </summary>
        /// <value>The exchange.</value>
        public string Exchange
        {
            set { this.exchange = value; }
        }

        /// <summary>
        /// Sets the destination queue.
        /// </summary>
        /// <value>The destination queue.</value>
        public Queue DestinationQueue
        {
            set { this.destinationQueue = value; }
        }

        /// <summary>
        /// Sets the destination exchange.
        /// </summary>
        /// <value>The destination exchange.</value>
        public IExchange DestinationExchange
        {
            set { this.destinationExchange = value; }
        }

        public object GetObject()
        {
            String destination;
            Binding.DestinationType destinationType;
            if (destinationQueue != null)
            {
                destination = destinationQueue.Name;
                destinationType = Binding.DestinationType.Queue;
            }
            else
            {
                destination = destinationExchange.Name;
                destinationType = Binding.DestinationType.Exchange;
            }
            return new Binding(destination, destinationType, exchange, routingKey, arguments);
        }

        public bool IsSingleton
        {
            get
            {
                return true;
            }
        }

        public Type ObjectType
        {
            get
            {
                return typeof(Binding);
            }
        }
    }
}
