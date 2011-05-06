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

#region

using System.IO;
using System.Text;
using NUnit.Framework;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Support.Converter;

#endregion

namespace Spring.Messaging.Amqp.Rabbit.Support.Converter
{
    [TestFixture]
    public class SimpleMessageConverterTests
    {
        private RabbitTemplate template;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
                      
            IConnectionFactory connectionFactory = new CachingConnectionFactory();


            template = new RabbitTemplate();
            template.ConnectionFactory = connectionFactory;
            template.IsChannelTransacted = true;
            template.AfterPropertiesSet();
        }

        [Test]
        public void BytesAsDefaultMessageBodyType()
        {
            SimpleMessageConverter converter = new SimpleMessageConverter();
            //template.CreateMessageProperties contains the default of using application/octet-stream as content-type
            Message message = new Message(Encoding.UTF8.GetBytes("test"), template.CreateMessageProperties());
            object result = converter.FromMessage(message);
            Assert.AreEqual(typeof (byte[]), result.GetType());
            Assert.AreEqual("test", ConvertToString((byte[]) result, SimpleMessageConverter.DEFAULT_CHARSET));
        }

        [Test]
        public void MessageToString()
        {
            SimpleMessageConverter converter = new SimpleMessageConverter();
            Message message = new Message(Encoding.UTF8.GetBytes("test"), template.CreateMessageProperties());
            message.MessageProperties.ContentType = MessageProperties.CONTENT_TYPE_TEXT_PLAIN;
            object result = converter.FromMessage(message);
            Assert.AreEqual(typeof (string), result.GetType());
            Assert.AreEqual("test", result);
        }

        [Test]
        public void MessageToBytes()
        {
            SimpleMessageConverter converter = new SimpleMessageConverter();
            Message message = new Message(new byte[] {1, 2, 3}, template.CreateMessageProperties());
            message.MessageProperties.ContentType = MessageProperties.CONTENT_TYPE_BYTES;
            object result = converter.FromMessage(message);
            Assert.AreEqual(typeof (byte[]), result.GetType());
            byte[] resultBytes = (byte[]) result;
            Assert.AreEqual(3, resultBytes.Length);
            Assert.AreEqual(1, resultBytes[0]);
            Assert.AreEqual(2, resultBytes[1]);
            Assert.AreEqual(3, resultBytes[2]);
        }

        [Test]
        public void StringToMessage()
        {
            SimpleMessageConverter converter = new SimpleMessageConverter();
            Message message = template.Execute(delegate(IModel channel) { return converter.ToMessage("test", new RabbitMessagePropertiesFactory(channel)); });
            Assert.AreEqual("text/plain", message.MessageProperties.ContentType);
            Assert.AreEqual("test", ConvertToString(message.Body, SimpleMessageConverter.DEFAULT_CHARSET));
        }

        [Test]
        public void BytesToMessage()
        {
            SimpleMessageConverter converter = new SimpleMessageConverter();
            Message message =
                template.Execute(delegate(IModel channel) { return converter.ToMessage(new byte[] { 1, 2, 3 }, new RabbitMessagePropertiesFactory(channel)); });
            string contentType = message.MessageProperties.ContentType;
            byte[] body = message.Body;

            Assert.AreEqual(MessageProperties.CONTENT_TYPE_BYTES, contentType);
            Assert.AreEqual(3, body.Length);
            Assert.AreEqual(1, body[0]);
            Assert.AreEqual(2, body[1]);
            Assert.AreEqual(3, body[2]);
        }

        private string ConvertToString(byte[] bytes, string encodingString)
        {
            MemoryStream ms = new MemoryStream(bytes);
            Encoding encoding = Encoding.GetEncoding(encodingString);
            //Last argument is to not autoDetectEncoding
            TextReader reader = new StreamReader(ms, encoding, false);
            string stringMessage = reader.ReadToEnd();
            return stringMessage;
        }
    }
}