using System;
using System.Text;
using System.Text.RegularExpressions;
using Spring.Util;

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
        //	private static final Pattern pattern = Pattern.compile("^([^:]+)://([^/]*)/?(.*)$");
        public static readonly Regex pattern = new Regex("^([^:]+)://([^/]*)/?(.*)$");

        private string exchangeType;

        private string exchangeName;

        private string routingKey;


        /// <summary>
        /// Initializes a new instance of the <see cref="Address"/> class from an unstructured string
        /// </summary>
        /// <param name="address">The unstructured address.</param>
        public Address(string address)
        {
            if (address == null)
            {
                this.exchangeType = ExchangeTypes.Direct;                                
                this.exchangeName = "";
                this.routingKey = "";
            } else
            {
                Match match = pattern.Match(address);
                if (match.Success)
                {
                    exchangeType = match.Groups[1].Value;
                    exchangeName = match.Groups[2].Value;
                    routingKey = match.Groups[3].Value;
                } else
                {
                    exchangeType = ExchangeTypes.Direct;
                    exchangeName = "";
                    routingKey = address;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Address"/> class given the exchange type,
        ///  exchange name and routing key. This will set the exchange type, name and the routing key explicitly.
        /// </summary>
        /// <param name="exchangeType">Type of the exchange.</param>
        /// <param name="exchangeName">Name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        public Address(string exchangeType, string exchangeName, string routingKey)
        {
            this.exchangeType = exchangeType;
            this.exchangeName = exchangeName;
            this.routingKey = routingKey;
        }

        public string ExchangeType
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(exchangeType.ToString().ToLower() + "://" + this.exchangeName + "/");
            if (StringUtils.HasText(routingKey))
            {
                sb.Append(routingKey);
            }
            return sb.ToString();
        }
    }
}