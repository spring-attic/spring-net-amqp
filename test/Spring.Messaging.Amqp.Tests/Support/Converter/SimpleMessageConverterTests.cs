// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleMessageConverterTests.cs" company="The original author or authors.">
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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Support.Converter;
#endregion

namespace Spring.Messaging.Amqp.Tests.Support.Converter
{
    /// <summary>
    /// Simple message converter tests.
    /// </summary>
    public class SimpleMessageConverterTests
    {
        /// <summary>The bytes as default message body type.</summary>
        [Test]
        public void BytesAsDefaultMessageBodyType()
        {
            var converter = new SimpleMessageConverter();
            var message = new Message("test".ToByteArrayWithEncoding("UTF-8"), new MessageProperties());
            var result = converter.FromMessage(message);
            Assert.AreEqual(typeof(byte[]), result.GetType());
            Assert.AreEqual("test", ((byte[])result).ToStringWithEncoding("UTF-8"));
        }

        /// <summary>The no message id by default.</summary>
        [Test]
        public void NoMessageIdByDefault()
        {
            var converter = new SimpleMessageConverter();
            var message = converter.ToMessage("foo", null);
            Assert.IsNull(message.MessageProperties.MessageId);
        }

        /// <summary>The optional message id.</summary>
        [Test]
        public void OptionalMessageId()
        {
            var converter = new SimpleMessageConverter();
            converter.CreateMessageIds = true;
            var message = converter.ToMessage("foo", null);
            Assert.IsNotNull(message.MessageProperties.MessageId);
        }

        /// <summary>The message to string.</summary>
        [Test]
        public void MessageToString()
        {
            var converter = new SimpleMessageConverter();
            var message = new Message("test".ToByteArrayWithEncoding("UTF-8"), new MessageProperties());
            message.MessageProperties.ContentType = MessageProperties.CONTENT_TYPE_TEXT_PLAIN;
            var result = converter.FromMessage(message);
            Assert.AreEqual(typeof(string), result.GetType());
            Assert.AreEqual("test", result);
        }

        /// <summary>The message to bytes.</summary>
        [Test]
        public void MessageToBytes()
        {
            var converter = new SimpleMessageConverter();
            var message = new Message(new byte[] { 1, 2, 3 }, new MessageProperties());
            message.MessageProperties.ContentType = MessageProperties.CONTENT_TYPE_BYTES;
            var result = converter.FromMessage(message);
            Assert.AreEqual(typeof(byte[]), result.GetType());
            var resultBytes = (byte[])result;
            Assert.AreEqual(3, resultBytes.Length);
            Assert.AreEqual(1, resultBytes[0]);
            Assert.AreEqual(2, resultBytes[1]);
            Assert.AreEqual(3, resultBytes[2]);
        }

        /// <summary>The message to serialized object.</summary>
        [Test]
        public void MessageToSerializedObject()
        {
            var converter = new SimpleMessageConverter();
            var properties = new MessageProperties();
            properties.ContentType = MessageProperties.CONTENT_TYPE_SERIALIZED_OBJECT;
            var binaryFormatter = new BinaryFormatter();
            var byteStream = new MemoryStream();
            var testObject = new TestObject("foo");
            binaryFormatter.Serialize(byteStream, testObject);
            var bytes = byteStream.ToArray();
            var message = new Message(bytes, properties);
            var result = converter.FromMessage(message);
            Assert.AreEqual(typeof(TestObject), result.GetType());
            Assert.AreEqual(testObject, result);
        }

        /// <summary>The string to message.</summary>
        [Test]
        public void StringToMessage()
        {
            var converter = new SimpleMessageConverter();
            var message = converter.ToMessage("test", new MessageProperties());
            var contentType = message.MessageProperties.ContentType;
            var content = message.Body.ToStringWithEncoding(message.MessageProperties.ContentEncoding);
            Assert.AreEqual("text/plain", contentType);
            Assert.AreEqual("test", content);
        }

        /// <summary>The bytes to message.</summary>
        [Test]
        public void BytesToMessage()
        {
            var converter = new SimpleMessageConverter();
            var message = converter.ToMessage(new byte[] { 1, 2, 3 }, new MessageProperties());
            var contentType = message.MessageProperties.ContentType;
            var body = message.Body;
            Assert.AreEqual("application/octet-stream", contentType);
            Assert.AreEqual(3, body.Length);
            Assert.AreEqual(1, body[0]);
            Assert.AreEqual(2, body[1]);
            Assert.AreEqual(3, body[2]);
        }

        /// <summary>The serialized object to message.</summary>
        [Test]
        public void SerializedObjectToMessage()
        {
            var converter = new SimpleMessageConverter();
            var testObject = new TestObject("foo");
            var message = converter.ToMessage(testObject, new MessageProperties());
            var contentType = message.MessageProperties.ContentType;
            var body = message.Body;
            Assert.AreEqual("application/x-java-serialized-object", contentType);
            var binaryFormatter = new BinaryFormatter();
            var byteStream = new MemoryStream(body);
            var deserializedObject = (TestObject)binaryFormatter.Deserialize(byteStream);
            Assert.AreEqual(testObject, deserializedObject);
        }

        [Serializable]
        private class TestObject
        {
            private readonly string text;

            /// <summary>Initializes a new instance of the <see cref="TestObject"/> class.</summary>
            /// <param name="text">The text.</param>
            public TestObject(string text)
            {
                Assert.NotNull(text, "text must not be null");
                this.text = text;
            }

            /// <summary>The equals.</summary>
            /// <param name="other">The other.</param>
            /// <returns>The System.Boolean.</returns>
            public override bool Equals(object other) { return other is TestObject && this.text.Equals(((TestObject)other).text); }

            /// <summary>The get hash code.</summary>
            /// <returns>The System.Int32.</returns>
            public override int GetHashCode() { return this.text.GetHashCode(); }
        }
    }
}
