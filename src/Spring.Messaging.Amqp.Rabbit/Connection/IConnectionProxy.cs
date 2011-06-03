using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// The IConnectionProxy
    /// </summary>
    public interface IConnectionProxy
    {
        /// <summary>
        /// Return the target Channel of this proxy. This will typically be the native provider IConnection
        /// </summary>
        /// <returns>
        /// The underlying connection (never null).
        /// </returns>
        IConnection GetTargetConnection();
    }
}
