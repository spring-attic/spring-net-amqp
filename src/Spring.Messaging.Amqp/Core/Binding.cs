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
using System.Collections;

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Simple container collecting information to describe a queue binding. Takes Queue and Exchange
    /// class as arguments to facilitate wiring using [Definition] based configuration.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class Binding
    {
        private string queue;

        private string exchange;

        private string routingKey;

        private IDictionary arguments;

        public static Binding CreateBinding(Queue queue, IExchange exchange, string routingKey)
        {
            return new Binding(queue, exchange, routingKey);
        }

        internal Binding(Queue queue, IExchange exchange, string routingKey)
        {
            this.queue = queue.Name;
            this.exchange = exchange.Name;
            this.routingKey = routingKey;
        }

        public Binding(Queue queue, FanoutExchange exchange)
        {
            this.queue = queue.Name;
            this.exchange = exchange.Name;
            this.routingKey = "";
        }

        public Binding(Queue queue, DirectExchange exchange, String routingKey)
        {
            this.queue = queue.Name;
            this.exchange = exchange.Name;
            this.routingKey = routingKey;
        }

        public Binding(Queue queue, DirectExchange exchange)
        {
            this.queue = queue.Name;
            this.exchange = exchange.Name;
            this.routingKey = "";
        }


        public Binding(Queue queue, TopicExchange exchange, String routingKey)
        {
            this.queue = queue.Name;
            this.exchange = exchange.Name;
            this.routingKey = routingKey;
        }

        public Binding(Queue queue, TopicExchange exchange)
        {
            this.queue = queue.Name;
            this.exchange = exchange.Name;
            this.routingKey = "";
        }

        public string Queue
        {
            get { return queue; }
        }

        public string Exchange
        {
            get { return exchange; }
        }

        public string RoutingKey
        {
            get { return routingKey; }
        }

        public IDictionary Arguments
        {
            set { this.arguments = value; }
            get { return arguments; }
        }
    }

}