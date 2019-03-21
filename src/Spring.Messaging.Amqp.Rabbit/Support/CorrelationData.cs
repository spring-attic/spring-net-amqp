// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CorrelationData.cs" company="The original author or authors.">
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
    /// Base class for correlating publisher confirms to sent messages.
    /// Use the {@link RabbitTemplate} methods that include one of
    /// these as a parameter; when the publisher confirm is received,
    /// the CorrelationData is returned with the ack/nack.
    /// </summary>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class CorrelationData
    {
        private readonly string id;

        /// <summary>Initializes a new instance of the <see cref="CorrelationData"/> class.</summary>
        /// <param name="id">The id.</param>
        public CorrelationData(string id) { this.id = id; }

        /// <summary>Gets the id.</summary>
        public string Id { get { return this.id; } }

        /// <summary>The to string.</summary>
        /// <returns>The System.String.</returns>
        public override string ToString() { return "CorrelationData [id=" + this.id + "]"; }
    }
}
