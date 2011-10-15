using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Erlang.NET;

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>
    /// A rabbit admin auth exception.
    /// </summary>
    public class RabbitAdminAuthException : OtpAuthException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitAdminAuthException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cause">The cause.</param>
        public RabbitAdminAuthException(string message, OtpAuthException cause) : base(message, cause)
        {
        }

    }
}
