// -----------------------------------------------------------------------
// <copyright file="MessageRejectedWhileStoppingException.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MessageRejectedWhileStoppingException : AmqpException
    {
        public MessageRejectedWhileStoppingException() : base("Message listener container was stopping when a message was received")
        {
        }
    }
}
