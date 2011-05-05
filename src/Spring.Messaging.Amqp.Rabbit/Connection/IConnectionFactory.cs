#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// An interface based ConnectionFactory for creating <see cref="IConnection"/>s.
    /// </summary>
    /// <author>Mark Pollack</author>
    public interface IConnectionFactory : IDisposable
    {
        /// <summary>
        /// Gets Host.
        /// </summary>
        string Host { get; }

        /// <summary>
        /// Gets Port.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Gets VirtualHost.
        /// </summary>
        string VirtualHost { get; }

        /// <summary>
        /// Create a connection.
        /// </summary>
        /// <returns>
        /// The connection.
        /// </returns>
        IConnection CreateConnection();

        /// <summary>
        /// Add a connection listener.
        /// </summary>
        /// <param name="listener">
        /// The listener.
        /// </param>
        void AddConnectionListener(IConnectionListener listener);
    }
}