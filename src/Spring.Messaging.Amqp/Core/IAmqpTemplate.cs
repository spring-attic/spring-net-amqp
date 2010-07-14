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
        void ConvertAndSend(object message);

        void ConvertAndSend(string routingKey, object message);

        void ConvertAndSend(string exchange, string routingKey, object message);

        void ConvertAndSend(object message, MessagePostProcessorDelegate messagePostProcessorDelegate);

        void ConvertAndSend(string routingKey, object message, MessagePostProcessorDelegate messagePostProcessorDelegate);

        void ConvertAndSend(string exchange, string routingKey, object message, MessagePostProcessorDelegate messagePostProcessorDelegate);

        Message Receive();

        Message Receive(string queueName);

        object ReceiveAndConvert();

        object ReceiveAndConvert(string queueName);
        IMessageProperties CreateMessageProperties();
    }

}