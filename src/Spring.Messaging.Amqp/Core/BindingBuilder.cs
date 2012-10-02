// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BindingBuilder.cs" company="The original author or authors.">
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
using System.Collections;
using System.Collections.Generic;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// The binding builder.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Mark Fisher</author>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public sealed class BindingBuilder
    {
        /// <summary>The bind.</summary>
        /// <param name="queue">The queue.</param>
        /// <returns>The destination configurer.</returns>
        public static DestinationConfigurer Bind(Queue queue) { return new DestinationConfigurer(queue.Name, Binding.DestinationType.Queue); }

        /// <summary>The bind.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <returns>The destination configurer.</returns>
        public static DestinationConfigurer Bind(IExchange exchange) { return new DestinationConfigurer(exchange.Name, Binding.DestinationType.Exchange); }

        /// <summary>The create map for keys.</summary>
        /// <param name="keys">The keys.</param>
        /// <returns>The map.</returns>
        private static IDictionary CreateMapForKeys(params string[] keys)
        {
            IDictionary map = new Dictionary<string, object>();
            foreach (var key in keys)
            {
                map.Add(key, null);
            }

            return map;
        }

        #region Nested type: AbstractRoutingKeyConfigurer

        /// <summary>The abstract routing key configurer.</summary>
        /// <typeparam name="T">An exchange.</typeparam>
        public abstract class AbstractRoutingKeyConfigurer<T> where T : IExchange
        {
            /// <summary>
            ///   The destination.
            /// </summary>
            public readonly DestinationConfigurer destination;

            /// <summary>
            ///   The exchange.
            /// </summary>
            public readonly string exchange;

            /// <summary>Initializes a new instance of the <see cref="AbstractRoutingKeyConfigurer{T}"/> class. 
            /// Initializes a new instance of the <see cref="AbstractRoutingKeyConfigurer{E}"/> class.</summary>
            /// <param name="destination">The destination.</param>
            /// <param name="exchange">The exchange.</param>
            protected AbstractRoutingKeyConfigurer(DestinationConfigurer destination, string exchange)
            {
                this.destination = destination;
                this.exchange = exchange;
            }
        }
        #endregion

        #region Nested type: DestinationConfigurer

        /// <summary>
        /// The destination configurer.
        /// </summary>
        public class DestinationConfigurer
        {
            /// <summary>
            ///   The name.
            /// </summary>
            private readonly string name;

            /// <summary>
            ///   The destination type.
            /// </summary>
            private readonly Binding.DestinationType type;

            /// <summary>Initializes a new instance of the <see cref="DestinationConfigurer"/> class.</summary>
            /// <param name="name">The name.</param>
            /// <param name="type">The type.</param>
            internal DestinationConfigurer(string name, Binding.DestinationType type)
            {
                this.name = name;
                this.type = type;
            }

            /// <summary>
            ///  Gets the Name.
            /// </summary>
            public string Name { get { return this.name; } }

            /// <summary>
            /// Gets the Destination Type.
            /// </summary>
            public Binding.DestinationType Type { get { return this.type; } }

            /// <summary>The to.</summary>
            /// <param name="exchange">The exchange.</param>
            /// <returns>The binding.</returns>
            public Binding To(FanoutExchange exchange) { return new Binding(this.name, this.type, exchange.Name, string.Empty, null); }

            /// <summary>The to.</summary>
            /// <param name="exchange">The exchange.</param>
            /// <returns>The headers exchange map configurer.</returns>
            public HeadersExchangeMapConfigurer To(HeadersExchange exchange) { return new HeadersExchangeMapConfigurer(this, exchange); }

            /// <summary>The to.</summary>
            /// <param name="exchange">The exchange.</param>
            /// <returns>The direct exchange routing key configurer.</returns>
            public DirectExchangeRoutingKeyConfigurer To(DirectExchange exchange) { return new DirectExchangeRoutingKeyConfigurer(this, exchange); }

            /// <summary>The to.</summary>
            /// <param name="exchange">The exchange.</param>
            /// <returns>The topic exchange routing key configurer.</returns>
            public TopicExchangeRoutingKeyConfigurer To(TopicExchange exchange) { return new TopicExchangeRoutingKeyConfigurer(this, exchange); }

            /// <summary>The to.</summary>
            /// <param name="exchange">The exchange.</param>
            /// <returns>The generic exchange routing key configurer.</returns>
            public GenericExchangeRoutingKeyConfigurer To(IExchange exchange) { return new GenericExchangeRoutingKeyConfigurer(this, exchange); }
        }
        #endregion

        #region Nested type: DirectExchangeRoutingKeyConfigurer

        /// <summary>
        /// The direct exchange routing key configurer.
        /// </summary>
        public class DirectExchangeRoutingKeyConfigurer : AbstractRoutingKeyConfigurer<DirectExchange>
        {
            /// <summary>Initializes a new instance of the <see cref="DirectExchangeRoutingKeyConfigurer"/> class.</summary>
            /// <param name="destination">The destination.</param>
            /// <param name="exchange">The exchange.</param>
            internal DirectExchangeRoutingKeyConfigurer(DestinationConfigurer destination, DirectExchange exchange) : base(destination, exchange.Name) { }

            /// <summary>The with.</summary>
            /// <param name="routingKey">The routing key.</param>
            /// <returns>The binding.</returns>
            public Binding With(string routingKey) { return new Binding(this.destination.Name, this.destination.Type, this.exchange, routingKey, null); }

            /// <summary>The with.</summary>
            /// <param name="routingKeyEnum">The routing key enumeration.</param>
            /// <returns>The binding.</returns>
            public Binding With(Enum routingKeyEnum) { return new Binding(this.destination.Name, this.destination.Type, this.exchange, routingKeyEnum.ToString(), null); }

            /// <summary>
            /// The with queue name.
            /// </summary>
            /// <returns>
            /// The binding.
            /// </returns>
            public Binding WithQueueName() { return new Binding(this.destination.Name, this.destination.Type, this.exchange, this.destination.Name, null); }
        }
        #endregion

        #region Nested type: GenericArgumentsConfigurer

        /// <summary>
        /// The generic arguments configurer.
        /// </summary>
        public class GenericArgumentsConfigurer
        {
            /// <summary>
            ///   The configurer.
            /// </summary>
            private readonly GenericExchangeRoutingKeyConfigurer configurer;

            /// <summary>
            ///   The routing key.
            /// </summary>
            private readonly string routingKey;

            /// <summary>Initializes a new instance of the <see cref="GenericArgumentsConfigurer"/> class.</summary>
            /// <param name="configurer">The configurer.</param>
            /// <param name="routingKey">The routing key.</param>
            public GenericArgumentsConfigurer(GenericExchangeRoutingKeyConfigurer configurer, string routingKey)
            {
                this.configurer = configurer;
                this.routingKey = routingKey;
            }

            /// <summary>The and.</summary>
            /// <param name="map">The map.</param>
            /// <returns>The binding.</returns>
            public Binding And(IDictionary map) { return new Binding(this.configurer.destination.Name, this.configurer.destination.Type, this.configurer.exchange, this.routingKey, map); }

            /// <summary>
            /// The noargs.
            /// </summary>
            /// <returns>
            /// The binding.
            /// </returns>
            public Binding Noargs() { return new Binding(this.configurer.destination.Name, this.configurer.destination.Type, this.configurer.exchange, this.routingKey, null); }
        }
        #endregion

        #region Nested type: GenericExchangeRoutingKeyConfigurer

        /// <summary>
        /// The generic exchange routing key configurer.
        /// </summary>
        public class GenericExchangeRoutingKeyConfigurer : AbstractRoutingKeyConfigurer<TopicExchange>
        {
            /// <summary>Initializes a new instance of the <see cref="GenericExchangeRoutingKeyConfigurer"/> class.</summary>
            /// <param name="destination">The destination.</param>
            /// <param name="exchange">The exchange.</param>
            internal GenericExchangeRoutingKeyConfigurer(DestinationConfigurer destination, IExchange exchange) : base(destination, exchange.Name) { }

            /// <summary>The with.</summary>
            /// <param name="routingKey">The routing key.</param>
            /// <returns>The generic arguments configurer.</returns>
            public GenericArgumentsConfigurer With(string routingKey) { return new GenericArgumentsConfigurer(this, routingKey); }

            /// <summary>The with.</summary>
            /// <param name="routingKeyEnum">The routing Key Enum.</param>
            /// <returns>The generic arguments configurer.</returns>
            public GenericArgumentsConfigurer With(Enum routingKeyEnum) { return new GenericArgumentsConfigurer(this, routingKeyEnum.ToString()); }
        }
        #endregion

        #region Nested type: HeadersExchangeMapConfigurer

        /// <summary>
        /// The headers exchange map configurer.
        /// </summary>
        public class HeadersExchangeMapConfigurer
        {
            /// <summary>
            ///   The destination.
            /// </summary>
            private readonly DestinationConfigurer destination;

            /// <summary>
            ///   The exchange.
            /// </summary>
            private readonly HeadersExchange exchange;

            /// <summary>Initializes a new instance of the <see cref="HeadersExchangeMapConfigurer"/> class.</summary>
            /// <param name="destination">The destination.</param>
            /// <param name="exchange">The exchange.</param>
            internal HeadersExchangeMapConfigurer(DestinationConfigurer destination, HeadersExchange exchange)
            {
                this.destination = destination;
                this.exchange = exchange;
            }

            /// <summary>The where.</summary>
            /// <param name="key">The key.</param>
            /// <returns>The headers exchange map binding creator.</returns>
            public HeadersExchangeSingleValueBindingCreator Where(string key) { return new HeadersExchangeSingleValueBindingCreator(this.destination, this.exchange, key); }

            /// <summary>The where any.</summary>
            /// <param name="headerKeys">The header keys</param>
            /// <returns>The headers exchange map binding creator.</returns>
            public HeadersExchangeKeysBindingCreator WhereAny(params string[] headerKeys) { return new HeadersExchangeKeysBindingCreator(this.destination, this.exchange, headerKeys, false); }

            /// <summary>The where any.</summary>
            /// <param name="headerValues">The header values.</param>
            /// <returns>The headers exchange map binding creator.</returns>
            public HeadersExchangeMapBindingCreator WhereAny(IDictionary headerValues) { return new HeadersExchangeMapBindingCreator(this.destination, this.exchange, headerValues, false); }

            /// <summary>The where all.</summary>
            /// <param name="headerKeys">The header keys.</param>
            /// <returns>The headers exchange map binding creator.</returns>
            public HeadersExchangeKeysBindingCreator WhereAll(params string[] headerKeys) { return new HeadersExchangeKeysBindingCreator(this.destination, this.exchange, headerKeys, true); }

            /// <summary>The where all.</summary>
            /// <param name="headerValues">The header values.</param>
            /// <returns>The headers exchange map binding creator.</returns>
            public HeadersExchangeMapBindingCreator WhereAll(IDictionary headerValues) { return new HeadersExchangeMapBindingCreator(this.destination, this.exchange, headerValues, true); }

            #region Nested type: HeadersExchangeKeysBindingCreator

            /// <summary>
            /// The headers exchange keys binding creator.
            /// </summary>
            public class HeadersExchangeKeysBindingCreator
            {
                /// <summary>
                ///   The destination.
                /// </summary>
                private readonly DestinationConfigurer destination;

                /// <summary>
                ///   The exchange.
                /// </summary>
                private readonly HeadersExchange exchange;

                /// <summary>
                ///   The header map.
                /// </summary>
                private readonly IDictionary headerMap;

                /// <summary>Initializes a new instance of the <see cref="HeadersExchangeKeysBindingCreator"/> class.</summary>
                /// <param name="destination">The destination.</param>
                /// <param name="exchange">The exchange.</param>
                /// <param name="headerKeys">The header keys.</param>
                /// <param name="matchAll">The match all.</param>
                public HeadersExchangeKeysBindingCreator(DestinationConfigurer destination, HeadersExchange exchange, string[] headerKeys, bool matchAll)
                {
                    AssertUtils.ArgumentNotNull(headerKeys, "headerKeys", "header key list must not be empty");
                    this.headerMap = CreateMapForKeys(headerKeys);
                    this.headerMap.Add("x-match", matchAll ? "all" : "any");
                    this.destination = destination;
                    this.exchange = exchange;
                }

                /// <summary>
                /// The exist.
                /// </summary>
                /// <returns>
                /// The binding.
                /// </returns>
                public Binding Exist() { return new Binding(this.destination.Name, this.destination.Type, this.exchange.Name, string.Empty, this.headerMap); }
            }
            #endregion

            #region Nested type: HeadersExchangeMapBindingCreator

            /// <summary>
            /// The headers exchange map binding creator.
            /// </summary>
            public class HeadersExchangeMapBindingCreator
            {
                /// <summary>
                ///   The destination.
                /// </summary>
                private readonly DestinationConfigurer destination;

                /// <summary>
                ///   The exchange.
                /// </summary>
                private readonly HeadersExchange exchange;

                /// <summary>
                ///   The header map.
                /// </summary>
                private readonly IDictionary headerMap;

                /// <summary>Initializes a new instance of the <see cref="HeadersExchangeMapBindingCreator"/> class.</summary>
                /// <param name="destination">The Destination.</param>
                /// <param name="exchange">The Exchange.</param>
                /// <param name="headerMap">The header map.</param>
                /// <param name="matchAll">The match all.</param>
                internal HeadersExchangeMapBindingCreator(DestinationConfigurer destination, HeadersExchange exchange, IDictionary headerMap, bool matchAll)
                {
                    // Assert.notEmpty(headerMap, "header map must not be empty");
                    this.headerMap = new Hashtable(headerMap) { { "x-match", matchAll ? "all" : "any" } };
                    this.destination = destination;
                    this.exchange = exchange;
                }

                /// <summary>
                /// The match.
                /// </summary>
                /// <returns>
                /// The binding.
                /// </returns>
                public Binding Match() { return new Binding(this.destination.Name, this.destination.Type, this.exchange.Name, string.Empty, this.headerMap); }
            }
            #endregion

            #region Nested type: HeadersExchangeSingleValueBindingCreator

            /// <summary>
            /// The headers exchange single value binding creator.
            /// </summary>
            public class HeadersExchangeSingleValueBindingCreator
            {
                /// <summary>
                ///   The destination.
                /// </summary>
                private readonly DestinationConfigurer destination;

                /// <summary>
                ///   The exchange.
                /// </summary>
                private readonly HeadersExchange exchange;

                /// <summary>
                ///   The key.
                /// </summary>
                private readonly string key;

                /// <summary>Initializes a new instance of the <see cref="HeadersExchangeSingleValueBindingCreator"/> class.</summary>
                /// <param name="destination">The destination.</param>
                /// <param name="exchange">The exchange.</param>
                /// <param name="key">The key.</param>
                internal HeadersExchangeSingleValueBindingCreator(DestinationConfigurer destination, HeadersExchange exchange, string key)
                {
                    AssertUtils.ArgumentHasText(key, "key", "key must not be null");
                    this.key = key;
                }

                /// <summary>
                /// The exists.
                /// </summary>
                /// <returns>
                /// The binding.
                /// </returns>
                public Binding Exists() { return new Binding(this.destination.Name, this.destination.Type, this.exchange.Name, string.Empty, CreateMapForKeys(this.key)); }

                /// <summary>The matches.</summary>
                /// <param name="value">The value.</param>
                /// <returns>The binding.</returns>
                public Binding Matches(object value)
                {
                    IDictionary map = new Hashtable { { this.key, value } };
                    return new Binding(this.destination.Name, this.destination.Type, this.exchange.Name, string.Empty, map);
                }
            }
            #endregion
        }
        #endregion

        #region Nested type: TopicExchangeRoutingKeyConfigurer

        /// <summary>
        /// The topic exchange routing key configurer.
        /// </summary>
        public class TopicExchangeRoutingKeyConfigurer : AbstractRoutingKeyConfigurer<TopicExchange>
        {
            /// <summary>Initializes a new instance of the <see cref="TopicExchangeRoutingKeyConfigurer"/> class.</summary>
            /// <param name="destination">The destination.</param>
            /// <param name="exchange">The exchange.</param>
            internal TopicExchangeRoutingKeyConfigurer(DestinationConfigurer destination, TopicExchange exchange) : base(destination, exchange.Name) { }

            /// <summary>The with.</summary>
            /// <param name="routingKey">The routing key.</param>
            /// <returns>The binding.</returns>
            public Binding With(string routingKey) { return new Binding(this.destination.Name, this.destination.Type, this.exchange, routingKey, null); }

            /// <summary>The with.</summary>
            /// <param name="routingKeyEnum">The routing key.</param>
            /// <returns>The binding.</returns>
            public Binding With(Enum routingKeyEnum) { return new Binding(this.destination.Name, this.destination.Type, this.exchange, routingKeyEnum.ToString(), null); }
        }
        #endregion
    }
}
