using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Tests.Core
{
    /// <summary>
    /// Binding builder tests.
    /// </summary>
    /// <remarks></remarks>
    public class BindingBuilderTests
    {
        /// <summary>
        /// Fanouts the binding.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void FanoutBinding()
        {
            var binding = BindingBuilder.Bind(new Queue("q")).To(new FanoutExchange("f"));
            Assert.NotNull(binding);
        }

        /// <summary>
        /// Directs the binding.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void DirectBinding()
        {
            var binding = BindingBuilder.Bind(new Queue("q")).To(new DirectExchange("d")).With("r");
            Assert.NotNull(binding);
        }

        /// <summary>
        /// Directs the name of the binding with queue.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void DirectBindingWithQueueName()
        {
            var binding = BindingBuilder.Bind(new Queue("q")).To(new DirectExchange("d")).WithQueueName();
            Assert.NotNull(binding);
        }

        /// <summary>
        /// Topics the binding.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        public void TopicBinding()
        {
            var binding = BindingBuilder.Bind(new Queue("q")).To(new TopicExchange("t")).With("r");
            Assert.NotNull(binding);
        }

        /// <summary>
        /// Customs the binding.
        /// </summary>
        /// <remarks></remarks>
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
        /// <remarks></remarks>
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
    /// <remarks></remarks>
    class CustomExchange : AbstractExchange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractExchange"/> class, given a name.
        /// </summary>
        /// <param name="name">The name of the exchange.</param>
        /// <remarks></remarks>
        public CustomExchange(string name) : base(name)
        {
        }

        /// <summary>
        /// Gets the type of the exchange.
        /// </summary>
        /// <remarks></remarks>
        public override string ExchangeType
        {
            get { return "x-custom"; }
        }
    }
}
