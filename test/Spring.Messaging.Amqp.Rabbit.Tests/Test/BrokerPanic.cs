
using System;

using Common.Logging;

using NUnit.Framework;

using Spring.Messaging.Amqp.Rabbit.Admin;

namespace Spring.Messaging.Amqp.Rabbit.Test
{
    /// <summary>
    /// A broker panic rule.
    /// </summary>
    public class BrokerPanic
    {
        private readonly ILog logger = LogManager.GetCurrentClassLogger();

        private RabbitBrokerAdmin brokerAdmin;

        /// <summary>
        /// Sets the broker admin.
        /// </summary>
        /// <value>The broker admin.</value>
        public RabbitBrokerAdmin BrokerAdmin
        {
            set
            {
                this.brokerAdmin = value;
            }
        }

        /// <summary>
        /// Applies this instance.
        /// </summary>
        public void Apply()
        {
            if (brokerAdmin != null)
            {
                try
                {
                    brokerAdmin.StopNode();
                }
                catch (Exception e)
                {
                    // don't hide original error (so ignored)
                    this.logger.Error("Error occurred stopping node", e);
                }
            }
        }
    }
}
