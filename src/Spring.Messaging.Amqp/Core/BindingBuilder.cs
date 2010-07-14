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

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Builder class to create bindings for a more fluent API style in code based configuration.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Mark Fisher</author>
    public class BindingBuilder
    {
        public ExchangeConfigurer From(Queue queue)
        {
            return new ExchangeConfigurer(queue);
        }

        public class ExchangeConfigurer
        {
            private Queue queue;

            internal ExchangeConfigurer(Queue queue)
            {
                this.queue = queue;
            }

            public Binding To(FanoutExchange fanoutExchange)
            {
                return new Binding(this.queue, fanoutExchange);
            }

            public DirectExchangeRoutingKeyConfigurer To(DirectExchange directExchange)
            {
                return new DirectExchangeRoutingKeyConfigurer(this.queue, directExchange);
            }

            public RoutingKeyConfigurer To(IExchange exchange)
            {
                return new RoutingKeyConfigurer(this.queue, exchange);
            }


        }

        public class RoutingKeyConfigurer
        {
            protected readonly Queue queue;

            protected readonly IExchange exchange;

            public RoutingKeyConfigurer(Queue queue, IExchange exchange)
            {
                this.queue = queue;
                this.exchange = exchange;
            }

            public Binding With(string routingKey)
            {
                return new Binding(this.queue, this.exchange, routingKey);
            }

            public Binding With(Enum routingKeyEnum)
            {
                return new Binding(this.queue, this.exchange, routingKeyEnum.ToString());
                    
            }
        }

        public class DirectExchangeRoutingKeyConfigurer : RoutingKeyConfigurer
        {
            public DirectExchangeRoutingKeyConfigurer(Queue queue, IExchange exchange) : base(queue, exchange)
            {
            }

            public Binding WithQueueName()
            {
                return new Binding(this.queue, this.exchange, this.queue.Name);
            }
        }
    }

}