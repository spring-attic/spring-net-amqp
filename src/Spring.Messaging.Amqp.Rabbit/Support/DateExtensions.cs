// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DateExtensions.cs" company="The original author or authors.">
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
using System;
using RabbitMQ.Client;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// Date Extension Methods
    /// </summary>
    public static class DateExtensions
    {
        /// <summary>Helper method to convert from DateTime to AmqpTimestamp.</summary>
        /// <param name="datetime">The datetime.</param>
        /// <returns>The AmqpTimestamp.</returns>
        internal static AmqpTimestamp ToAmqpTimestamp(this DateTime datetime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixTime = (datetime.ToUniversalTime() - epoch).TotalSeconds;
            var timestamp = new AmqpTimestamp((long)unixTime);
            return timestamp;
        }

        /// <summary>Helper method to convert from AmqpTimestamp.UnixTime to a DateTime (for the local machine).</summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns>The DateTime.</returns>
        internal static DateTime ToDateTime(this AmqpTimestamp timestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(timestamp.UnixTime).ToLocalTime();
        }

        /// <summary>The to milliseconds.</summary>
        /// <param name="datetime">The datetime.</param>
        /// <returns>The System.Int64.</returns>
        public static long ToMilliseconds(this DateTime datetime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixTime = (datetime.ToUniversalTime() - epoch).TotalMilliseconds;
            return (long)unixTime;
        }
    }
}
