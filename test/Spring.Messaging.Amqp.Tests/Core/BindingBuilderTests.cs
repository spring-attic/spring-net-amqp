// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BindingBuilderTests.cs" company="The original author or authors.">
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
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
#endregion

namespace Spring.Messaging.Amqp.Tests.Core
{
    /// <summary>
    /// Binding builder tests.
    /// </summary>
    public class BindingBuilderTests
    {
        /// <summary>
        /// Fanouts the binding.
        /// </summary>
        [Test]
        public void FanoutBinding()
        {
            var binding = BindingBuilder.Bind(new Queue("q")).To(new FanoutExchange("f"));
            Assert.NotNull(binding);
        }

        /// <summary>
        /// Directs the binding.
        /// </summary>
        [Test]
        public void DirectBinding()
        {
            var binding = BindingBuilder.Bind(new Queue("q")).To(new DirectExchange("d")).With("r");
            Assert.NotNull(binding);
        }

        /// <summary>
        /// Directs the name of the binding with queue.
        /// </summary>
        [Test]
        public void DirectBindingWithQueueName()
        {
            var binding = BindingBuilder.Bind(new Queue("q")).To(new DirectExchange("d")).WithQueueName();
            Assert.NotNull(binding);
        }

        /// <summary>
        /// Topics the binding.
        /// </summary>
        [Test]
        public void TopicBinding()
        {
            var binding = BindingBuilder.Bind(new Queue("q")).To(new TopicExchange("t")).With("r");
            Assert.NotNull(binding);
        }

        /// <summary>
        /// Customs the binding.
        /// </summary>
        [Test]
        public void CustomBinding()
        {
            var dict = new Dictionary<string, object>();
            dict.Add("k", new object());

            var binding = BindingBuilder.Bind(new Queue("q")).To(new CustomExchange("f")).With("r").And(dict);
            Assert.NotNull(binding);
        }

        /// <summary>
        /// Exchanges the binding.
        /// </summary>
        [Test]
        public void ExchangeBinding()
        {
            var binding = BindingBuilder.Bind(new DirectExchange("q")).To(new FanoutExchange("f"));
            Assert.NotNull(binding);
        }
    }

    /// <summary>
    /// A custom exchange.
    /// </summary>
    internal class CustomExchange : AbstractExchange
    {
        /// <summary>Initializes a new instance of the <see cref="CustomExchange"/> class. Initializes a new instance of the <see cref="AbstractExchange"/> class, given a name.</summary>
        /// <param name="name">The name of the exchange.</param>
        public CustomExchange(string name) : base(name) { }

        /// <summary>
        /// Gets the type of the exchange.
        /// </summary>
        public override string Type { get { return "x-custom"; } }
    }
}
