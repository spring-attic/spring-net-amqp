// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PendingConfirm.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// Instances of this object track pending publisher confirms.
    /// The timestamp allows the pending confirmation to be
    /// expired. It also holds <see cref="CorrelationData"/> for
    /// the client to correlate a confirm with a sent message.
    /// </summary>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class PendingConfirm
    {
        private readonly CorrelationData correlationData;

        private readonly long timestamp;

        /// <summary>Initializes a new instance of the <see cref="PendingConfirm"/> class.</summary>
        /// <param name="correlationData">The correlation data.</param>
        /// <param name="timestamp">The timestamp.</param>
        public PendingConfirm(CorrelationData correlationData, long timestamp)
        {
            this.correlationData = correlationData;
            this.timestamp = timestamp;
        }

        /// <summary>Gets the correlation data.</summary>
        public CorrelationData CorrelationData { get { return this.correlationData; } }

        /// <summary>Gets the timestamp.</summary>
        public long Timestamp { get { return this.timestamp; } }

        /// <summary>The to string.</summary>
        /// <returns>The System.String.</returns>
        public override string ToString() { return "PendingConfirm [correlationData=" + this.correlationData + "]"; }
    }
}
