using System;
using System.Text.RegularExpressions;

namespace Spring.Messaging.Amqp.Core
{

    /// <summary>
    /// Represents an address for publication of an AMQP message. The AMQP 0-8 and
    /// 0-9 specifications have an unstructured string that is used as a "reply to"
    /// address. There are however conventions in use and this class makes it easier
    /// to follow these conventions.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class Address
    {
        public static readonly Regex PSEUDO_URI_PARSER = new Regex("^([^:]+)://([^/]*)/(.*)$");


        private string unstructuredAddress;

        private ExchangeType exchangeType;

        private string exchangeName;

        private string routingKey;

        private bool structured;


        /// <summary>
        /// Initializes a new instance of the <see cref="Address"/> class from an unstructured string
        /// </summary>
        /// <param name="unstructuredAddress">The unstructured address.</param>
        public Address(string unstructuredAddress)
        {
            this.unstructuredAddress = unstructuredAddress;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Address"/> class given the exchange type,
        ///  exchange name and routing key.
        /// </summary>
        /// <param name="exchangeType">Type of the exchange.</param>
        /// <param name="exchangeName">Name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        public Address(ExchangeType exchangeType, string exchangeName, string routingKey)
        {
            this.exchangeType = exchangeType;
            this.exchangeName = exchangeName;
            this.routingKey = routingKey;
        }

        public static Address Parse(string address)
        {
            Match match = PSEUDO_URI_PARSER.Match(address);
            if (match.Success)
            {
                string exchangeTypeAsString = match.Groups[1].Value;
                ExchangeType exchangeType = (ExchangeType) Enum.Parse(typeof (ExchangeType), exchangeTypeAsString, true);
                return new Address(exchangeType, match.Groups[2].Value, match.Groups[3].Value);
            }
            return null;
        }

        public ExchangeType ExchangeType
        {
            get { return exchangeType; }
        }

        public string ExchangeName
        {
            get { return exchangeName; }
        }

        public string RoutingKey
        {
            get { return routingKey; }
        }

        public bool Structured
        {
            get { return structured; }
        }

        public override string ToString()
        {
            return string.Format("ExchangeType: {0}, ExchangeName: {1}, RoutingKey: {2}, Structured: {3}, UnstructuredAddress: {4}", exchangeType, exchangeName, routingKey, structured, unstructuredAddress);
        }
    }
}