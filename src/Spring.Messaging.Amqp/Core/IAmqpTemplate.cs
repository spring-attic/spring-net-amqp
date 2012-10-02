// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IAmqpTemplate.cs" company="The original author or authors.">
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
using Spring.Messaging.Amqp.Support.Converter;
#endregion

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
    /// <author>Mark Fisher</author>
    /// <author>Joe Fitzgerald</author>
    public interface IAmqpTemplate
    {
        #region Send methods for messages

        /// <summary>Send a message to a default exchange with a default routing key.</summary>
        /// <param name="message">The message to send.</param>
        void Send(Message message);

        /// <summary>Send a message to a default exchange with a specific routing key.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        void Send(string routingKey, Message message);

        /// <summary>Send a message to a specific exchange with a specific routing key.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        void Send(string exchange, string routingKey, Message message);
        #endregion

        #region Send methods with conversion

        /// <summary>Convert a .NET object to an Amqp <see cref="Spring.Messaging.Amqp.Core.Message"/> and send it to a default exchange with a default routing key.</summary>
        /// <param name="message">The message to send.</param>
        void ConvertAndSend(object message);

        /// <summary>Convert a .NET object to an Amqp <see cref="Spring.Messaging.Amqp.Core.Message"/> and send it to a default exchange with a specified routing key.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        void ConvertAndSend(string routingKey, object message);

        /// <summary>Convert a .NET object to an Amqp <see cref="Spring.Messaging.Amqp.Core.Message"/> and send it to a specified exchange with a specified routing key.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        void ConvertAndSend(string exchange, string routingKey, object message);

        /// <summary>Convert a .NET object to an Amqp <see cref="Spring.Messaging.Amqp.Core.Message"/> and send it to a default exchange with a default routing key.</summary>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        void ConvertAndSend(object message, Func<Message, Message> messagePostProcessor);

        /// <summary>Convert a .NET object to an Amqp <see cref="Spring.Messaging.Amqp.Core.Message"/> and send it to a default exchange with a default routing key.</summary>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor);

        /// <summary>Convert a .NET object to an Amqp <see cref="Spring.Messaging.Amqp.Core.Message"/> and send it to a default exchange with a specified routing key.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        void ConvertAndSend(string routingKey, object message, Func<Message, Message> messagePostProcessor);

        /// <summary>Convert a .NET object to an Amqp <see cref="Spring.Messaging.Amqp.Core.Message"/> and send it to a default exchange with a specified routing key.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        void ConvertAndSend(string routingKey, object message, IMessagePostProcessor messagePostProcessor);

        /// <summary>Convert a .NET object to an Amqp <see cref="Spring.Messaging.Amqp.Core.Message"/> and send it to a specified exchange with a specified routing key.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        void ConvertAndSend(string exchange, string routingKey, object message, Func<Message, Message> messagePostProcessor);

        /// <summary>Convert a .NET object to an Amqp <see cref="Spring.Messaging.Amqp.Core.Message"/> and send it to a specified exchange with a specified routing key.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor);
        #endregion

        #region Receive methods for messages

        /// <summary>
        /// Receive a message if there is one from a default queue. Returns immediately, possibly with a null value.
        /// </summary>
        /// <returns>A message or null if there is none waiting.</returns>
        Message Receive();

        /// <summary>Receive a message if there is one from a specific queue. Returns immediately, possibly with a null value.</summary>
        /// <param name="queueName">The queue name.</param>
        /// <returns>A message or null if there is none waiting.</returns>
        Message Receive(string queueName);
        #endregion

        #region Receive methods with conversion

        /// <summary>
        /// Receive a message if there is one from a default queue and convert it to a .NET object. Returns immediately,
        /// possibly with a null value.
        /// </summary>
        /// <returns>A message or null if there is none waiting.</returns>
        object ReceiveAndConvert();

        /// <summary>Receive a message if there is one from a specified queue and convert it to a .NET object. Returns immediately,
        /// possibly with a null value.</summary>
        /// <param name="queueName">The name of the queue to poll.</param>
        /// <returns>The message.</returns>
        object ReceiveAndConvert(string queueName);
        #endregion

        #region Send and receive methods for messages

        /// <summary>Basic RPC pattern. Send a message to a default exchange with a default routing key and attempt to receive a
        /// response. Implementations will normally set the reply-to header to an exclusive queue and wait up for some time
        /// limited by a timeout.</summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The response if there is one.</returns>
        Message SendAndReceive(Message message);

        /// <summary>Basic RPC pattern. Send a message to a default exchange with a specified routing key and attempt to receive a
        /// response. Implementations will normally set the reply-to header to an exclusive queue and wait up for some time
        /// limited by a timeout.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The response if there is one.</returns>
        Message SendAndReceive(string routingKey, Message message);

        /// <summary>Basic RPC pattern. Send a message to a specified exchange with a specified routing key and attempt to receive a
        /// response. Implementations will normally set the reply-to header to an exclusive queue and wait up for some time
        /// limited by a timeout.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The response if there is one.</returns>
        Message SendAndReceive(string exchange, string routingKey, Message message);
        #endregion

        #region Send and receive methods with conversion

        /// <summary>Basic RPC pattern with conversion. Send a .NET object converted to a message to a default exchange with a default
        /// routing key and attempt to receive a response, converting that to a .NET object. Implementations will normally
        /// set the reply-to header to an exclusive queue and wait up for some time limited by a timeout.</summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The response if there is one.</returns>
        object ConvertSendAndReceive(object message);

        /// <summary>Basic RPC pattern with conversion. Send a .NET object converted to a message to a default exchange with a specified
        /// routing key and attempt to receive a response, converting that to a .NET object. Implementations will normally
        /// set the reply-to header to an exclusive queue and wait up for some time limited by a timeout.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The response if there is one.</returns>
        object ConvertSendAndReceive(string routingKey, object message);

        /// <summary>Basic RPC pattern with conversion. Send a .NET object converted to a message to a specified exchange with a specified
        /// routing key and attempt to receive a response, converting that to a .NET object. Implementations will normally
        /// set the reply-to header to an exclusive queue and wait up for some time limited by a timeout.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>The response if there is one.</returns>
        object ConvertSendAndReceive(string exchange, string routingKey, object message);

        /// <summary>Basic RPC pattern with conversion. Send a .NET object converted to a message to a default exchange with a default
        /// routing key and attempt to receive a response, converting that to a .NET object. Implementations will normally
        /// set the reply-to header to an exclusive queue and wait up for some time limited by a timeout.</summary>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The response if there is one.</returns>
        object ConvertSendAndReceive(object message, Func<Message, Message> messagePostProcessor);

        /// <summary>Basic RPC pattern with conversion. Send a .NET object converted to a message to a default exchange with a default
        /// routing key and attempt to receive a response, converting that to a .NET object. Implementations will normally
        /// set the reply-to header to an exclusive queue and wait up for some time limited by a timeout.</summary>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The response if there is one.</returns>
        object ConvertSendAndReceive(object message, IMessagePostProcessor messagePostProcessor);

        /// <summary>Basic RPC pattern with conversion. Send a .NET object converted to a message to a default exchange with a specified
        /// routing key and attempt to receive a response, converting that to a .NET object. Implementations will normally
        /// set the reply-to header to an exclusive queue and wait up for some time limited by a timeout.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The response if there is one.</returns>
        object ConvertSendAndReceive(string routingKey, object message, Func<Message, Message> messagePostProcessor);

        /// <summary>Basic RPC pattern with conversion. Send a .NET object converted to a message to a default exchange with a specified
        /// routing key and attempt to receive a response, converting that to a .NET object. Implementations will normally
        /// set the reply-to header to an exclusive queue and wait up for some time limited by a timeout.</summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The response if there is one.</returns>
        object ConvertSendAndReceive(string routingKey, object message, IMessagePostProcessor messagePostProcessor);

        /// <summary>Basic RPC pattern with conversion. Send a .NET object converted to a message to a specified exchange with a specified
        /// routing key and attempt to receive a response, converting that to a .NET object. Implementations will normally
        /// set the reply-to header to an exclusive queue and wait up for some time limited by a timeout.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The response if there is one.</returns>
        object ConvertSendAndReceive(string exchange, string routingKey, object message, Func<Message, Message> messagePostProcessor);

        /// <summary>Basic RPC pattern with conversion. Send a .NET object converted to a message to a specified exchange with a specified
        /// routing key and attempt to receive a response, converting that to a .NET object. Implementations will normally
        /// set the reply-to header to an exclusive queue and wait up for some time limited by a timeout.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="messagePostProcessor">The message post processor.</param>
        /// <returns>The response if there is one.</returns>
        object ConvertSendAndReceive(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor);
        #endregion
    }
}
