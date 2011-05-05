
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

using Spring.Messaging.Amqp.Support.Converter;

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Specifies a basic set of AMQP operations
    /// </summary>
    /// <remarks>
    /// Provides synchronous send an receive methods.  The ConvertAndSend and ReceiveAndConvert
    /// methods allow let you send and receive POCO objects.  Implementations are expected to
    /// delegate to an instance of <see cref="IMessageConverter"/> to perform
    /// conversion to and from AMQP byte[] payload type.
    /// </remarks>
    /// <author>Mark Pollack</author>
    public interface IAmqpTemplate
    {
        #region Send methods for messages

        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        void Send(Message message);

        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        void Send(string routingKey, Message message);

        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        void Send(string exchange, string routingKey, Message message);

        #endregion

        #region Send methods with conversion

        /// <summary>
        /// Send a message with conversion.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        void ConvertAndSend(object message);

        /// <summary>
        /// Send a message with conversion.
        /// </summary>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        void ConvertAndSend(string routingKey, object message);

        /// <summary>
        /// Send a message with conversion.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        void ConvertAndSend(string exchange, string routingKey, object message);

        /// <summary>
        /// Send a message with conversion.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="messagePostProcessorDelegate">
        /// The message post processor delegate.
        /// </param>
        void ConvertAndSend(object message, MessagePostProcessorDelegate messagePostProcessorDelegate);

        /// <summary>
        /// Send a message with conversion.
        /// </summary>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="messagePostProcessorDelegate">
        /// The message post processor delegate.
        /// </param>
        void ConvertAndSend(string routingKey, object message, MessagePostProcessorDelegate messagePostProcessorDelegate);

        /// <summary>
        /// Send a message with conversion.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="messagePostProcessorDelegate">
        /// The message post processor delegate.
        /// </param>
        void ConvertAndSend(string exchange, string routingKey, object message, MessagePostProcessorDelegate messagePostProcessorDelegate);
        #endregion

        #region Receive methods for messages

        /// <summary>
        /// Receive a message.
        /// </summary>
        /// <returns>
        /// The message.
        /// </returns>
        Message Receive();

        /// <summary>
        /// Receive a message.
        /// </summary>
        /// <param name="queueName">
        /// The queue name.
        /// </param>
        /// <returns>
        /// The message.
        /// </returns>
        Message Receive(string queueName);

        #endregion

        #region Receive methods with conversion

        /// <summary>
        /// Receive a message with conversion.
        /// </summary>
        /// <returns>
        /// The message.
        /// </returns>
        object ReceiveAndConvert();

        /// <summary>
        /// Receive a message with conversion.
        /// </summary>
        /// <param name="queueName">
        /// The queue name.
        /// </param>
        /// <returns>
        /// The message.
        /// </returns>
        object ReceiveAndConvert(string queueName);

        #endregion

        #region Send and receive methods for messages

        /// <summary>
        /// Send a message and receive a response.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The response.
        /// </returns>
        Message SendAndReceive(Message message);

        /// <summary>
        /// Send a message and receive a response.
        /// </summary>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The response.
        /// </returns>
        Message SendAndReceive(string routingKey, Message message);

        /// <summary>
        /// Send a message and receive a response.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The response.
        /// </returns>
        Message SendAndReceive(string exchange, string routingKey, Message message);
        #endregion

        #region Send and receive methods with conversion

        /// <summary>
        /// Send a message and receive a response with conversion.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The response.
        /// </returns>
        object ConvertSendAndReceive(object message);

        /// <summary>
        /// Send a message and receive a response with conversion.
        /// </summary>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The response.
        /// </returns>
        object ConvertSendAndReceive(string routingKey, object message);

        /// <summary>
        /// Send a message and receive a response with conversion.
        /// </summary>
        /// <param name="exchange">
        /// The exchange.
        /// </param>
        /// <param name="routingKey">
        /// The routing key.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The response.
        /// </returns>
        object ConvertSendAndReceive(string exchange, string routingKey, object message);
        #endregion
    }
}