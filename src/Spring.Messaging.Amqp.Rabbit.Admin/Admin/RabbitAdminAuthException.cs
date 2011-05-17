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
    /// <remarks></remarks>
    public class RabbitAdminAuthException : OtpAuthException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitAdminAuthException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cause">The cause.</param>
        /// <remarks></remarks>
        public RabbitAdminAuthException(string message, OtpAuthException cause) : base(message, cause)
        {
            //super(message, (com.ericsson.otp.erlang.OtpAuthException) cause.getCause());
        }

    }
}
