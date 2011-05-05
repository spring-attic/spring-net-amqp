
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

using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// Rabbit specific methods for AMQP functionality.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Mark Fisher</author>
    /// <author>Joe Fitzgerald</author>
    public interface IRabbitOperations : IAmqpTemplate
    {
        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="messageCreator">
        /// The message creator.
        /// </param>
        void Send(MessageCreatorDelegate messageCreator);

        /// <summary>
        /// Send a message given a routing key.
        /// </summary>
        /// <param name="routingkey">
        /// The routingkey.
        /// </param>
        /// <param name="messageCreator">
        /// The message creator.
        /// </param>
        void Send(string routingkey, MessageCreatorDelegate messageCreator);

        /// <summary>
        /// Send a message given an exchange and a routing key.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="messageCreatorDelegate">
        /// The message creator delegate.
        /// </param>
        void Send(string exchange, string routingKey, MessageCreatorDelegate messageCreatorDelegate);

        /// <summary>
        /// Execute the action.
        /// </summary>
        /// <param name="action">
        /// The action.
        /// </param>
        /// <typeparam name="T">
        /// Type T
        /// </typeparam>
        /// <returns>
        /// Object of Type T
        /// </returns>
        T Execute<T>(IChannelCallback<T> action);

        /// <summary>
        /// Execute the action.
        /// </summary>
        /// <param name="action">
        /// The action.
        /// </param>
        /// <typeparam name="T">
        /// Type T
        /// </typeparam>
        /// <returns>
        /// Object of Type T
        /// </returns>
        T Execute<T>(ChannelCallbackDelegate<T> action);
    }
}