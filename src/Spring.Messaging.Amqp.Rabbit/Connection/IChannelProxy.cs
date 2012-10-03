// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IChannelProxy.cs" company="The original author or authors.">
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
using RabbitMQ.Client;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A channel proxy interface.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald</author>
    public interface IChannelProxy : IModel
    {
        /// <summary>
        /// Return the target Channel (Model) of this proxy. This will typically be the native provider Channel (Model).
        /// </summary>
        /// <returns>
        /// The channel.
        /// </returns>
        IModel GetTargetChannel();

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <returns>The connection associated with the channel.</returns>
        RabbitMQ.Client.IConnection GetConnection();
    }
}
