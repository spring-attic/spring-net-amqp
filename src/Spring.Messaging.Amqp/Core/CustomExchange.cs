
using System.Collections;

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Simple container collecting information to describe a custom exchange. Custom exchange types are allowed by the AMQP
    /// specification, and their names should start with "x-" (but this is not enforced here). Used in conjunction with
    /// administrative operations.
    /// </summary>
    /// <author>Joe Fitzgerald</author>
    public class CustomExchange : AbstractExchange 
    {
        /// <summary>
        /// The type.
        /// </summary>
        private readonly string type;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomExchange"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        public CustomExchange(string name, string type) : base(name)
        {
            this.type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomExchange"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="durable">
        /// The durable.
        /// </param>
        /// <param name="autoDelete">
        /// The auto delete.
        /// </param>
        public CustomExchange(string name, string type, bool durable, bool autoDelete) : base(name, durable, autoDelete)
        {
            this.type = type;
	    }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomExchange"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="durable">
        /// The durable.
        /// </param>
        /// <param name="autoDelete">
        /// The auto delete.
        /// </param>
        /// <param name="arguments">
        /// The arguments.
        /// </param>
        public CustomExchange(string name, string type, bool durable, bool autoDelete, IDictionary arguments) : base(name, durable, autoDelete, arguments)
        {
            this.type = type;
	    }

        #region Overrides of AbstractExchange

        /// <summary>
        /// Gets ExchangeType.
        /// </summary>
        public override string ExchangeType
        {
            get { return this.type; }
        }

        #endregion
    }
}
