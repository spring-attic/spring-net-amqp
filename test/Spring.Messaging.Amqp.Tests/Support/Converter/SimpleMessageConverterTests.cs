
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using NUnit.Framework;

using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Support.Converter;

namespace Spring.Messaging.Amqp.Tests.Support.Converter
{
    /// <summary>
    /// Simple message converter tests.
    /// </summary>
    public class SimpleMessageConverterTests
    {
        [Test]
        public void BytesAsDefaultMessageBodyType()
        {
		    var converter = new SimpleMessageConverter();
		    var message = new Message("test".ToByteArrayWithEncoding("UTF-8"), new MessageProperties());
		    var result = converter.FromMessage(message);
		    Assert.AreEqual(typeof(byte[]), result.GetType());
		    Assert.AreEqual("test", ((byte[])result).ToStringWithEncoding("UTF-8"));
	    }

        [Test]
        public void NoMessageIdByDefault() 
        {
		    var converter = new SimpleMessageConverter();
		    var message = converter.ToMessage("foo", null);
		    Assert.IsNull(message.MessageProperties.MessageId);
	    }

        [Test]
        public void OptionalMessageId() 
        {
		    var converter = new SimpleMessageConverter();
		    converter.CreateMessageIds = true;
		    var message = converter.ToMessage("foo", null);
		    Assert.IsNotNull(message.MessageProperties.MessageId);
	    }

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

        [Test]
        public void MessageToBytes() 
        {
		    var converter = new SimpleMessageConverter();
		    var message = new Message(new byte[] { 1, 2, 3 }, new MessageProperties());
		    message.MessageProperties.ContentType = MessageProperties.CONTENT_TYPE_BYTES;
		    var result = converter.FromMessage(message);
		    Assert.AreEqual(typeof(byte[]), result.GetType());
		    byte[] resultBytes = (byte[]) result;
		    Assert.AreEqual(3, resultBytes.Length);
		    Assert.AreEqual(1, resultBytes[0]);
		    Assert.AreEqual(2, resultBytes[1]);
		    Assert.AreEqual(3, resultBytes[2]);
	    }

        [Test]
        public void MessageToSerializedObject() 
        {
		    var converter = new SimpleMessageConverter();
		    var properties = new MessageProperties();
		    properties.ContentType = MessageProperties.CONTENT_TYPE_SERIALIZED_OBJECT;
            var binaryFormatter = new BinaryFormatter();
            var byteStream = new MemoryStream();
            var testBean = new TestObject("foo");
            binaryFormatter.Serialize(byteStream, testBean);
		    var bytes = byteStream.ToArray();
		    var message = new Message(bytes, properties);
		    var result = converter.FromMessage(message);
		    Assert.AreEqual(typeof(TestObject), result.GetType());
		    Assert.AreEqual(testBean, result);
	    }

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

        [Test]
        public void SerializedObjectToMessage()
        {
		    var converter = new SimpleMessageConverter();
		    var testBean = new TestObject("foo");
		    var message = converter.ToMessage(testBean, new MessageProperties());
		    var contentType = message.MessageProperties.ContentType;
		    var body = message.Body;
		    Assert.AreEqual("application/x-java-serialized-object", contentType);
            var binaryFormatter = new BinaryFormatter();
            var byteStream = new MemoryStream(body);
            var deserializedObject = (TestObject)binaryFormatter.Deserialize(byteStream);
		    Assert.AreEqual(testBean, deserializedObject);
	    }

        [Serializable]
        private class TestObject
        {
            private readonly string text;

		    public TestObject(String text) 
            {
			    Assert.NotNull(text, "text must not be null");
			    this.text = text;
		    }

		    public override bool Equals(object other) 
            {
			    return (other is TestObject && this.text.Equals(((TestObject)other).text));
		    }

		    public override int GetHashCode() 
            {
			    return this.text.GetHashCode();
		    }
	    }
    }
}
