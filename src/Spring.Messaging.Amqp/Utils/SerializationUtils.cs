
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;

namespace Spring.Messaging.Amqp.Utils
{
    /// <summary>
    /// Utilities for serialization and deserialization.
    /// </summary>
    public static class SerializationUtils
    {
        /// <summary>
        /// Convert an object to a byte array.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <returns>
        /// The byte array.
        /// </returns>
        public static byte[] SerializeObject(ISerializable obj)
        {
            using (var stream = new MemoryStream())
            {
                var b = new BinaryFormatter();
                b.Serialize(stream, obj);
                var data = stream.ToArray();

                return data;
            }
        }

        /// <summary>
        /// Convert a byte array to an object.
        /// </summary>
        /// <param name="bytes">
        /// The bytes.
        /// </param>
        /// <returns>
        /// The object.
        /// </returns>
        public static object DeserializeObject(byte[] bytes)
        {
            using (var stream = new MemoryStream())
            {
                var b = new BinaryFormatter();
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                var obj = b.Deserialize(stream);
                return obj;
            }
        }

        /// <summary>
        /// Convert a string to a byte array.
        /// </summary>
        /// <param name="str">
        /// The str.
        /// </param>
        /// <param name="encodingString">
        /// The encoding string.
        /// </param>
        /// <returns>
        /// The byte array.
        /// </returns>
        public static byte[] SerializeString(string str, string encodingString)
        {
            var encoding = Encoding.GetEncoding(encodingString);
            return encoding.GetBytes(str);
        }

        /// <summary>
        /// Extension method to convert a string to a byte array with encoding.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="encodingString">The encoding string.</param>
        /// <returns>The byte array.</returns>
        public static byte[] ToByteArrayWithEncoding(this string str, string encodingString)
        {
            return SerializeString(str, encodingString);
        }

        /// <summary>
        /// Convert a byte array to a string.
        /// </summary>
        /// <param name="bytes">
        /// The bytes.
        /// </param>
        /// <param name="encodingString">
        /// The encoding string.
        /// </param>
        /// <returns>
        /// The string.
        /// </returns>
        public static string DeserializeString(byte[] bytes, string encodingString)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var encoding = Encoding.GetEncoding(encodingString);

                using (TextReader reader = new StreamReader(ms, encoding, false))
                {
                    var stringMessage = reader.ReadToEnd();
                    return stringMessage;
                }
            }
        }

        /// <summary>
        /// Extension method to convert a byte array to a string with encoding.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="encodingString">The encoding string.</param>
        /// <returns>The string.</returns>
        public static string ToStringWithEncoding(this byte[] bytes, string encodingString)
        {
            return DeserializeString(bytes, encodingString);
        }

        /// <summary>
        /// Serialize an object as Json.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <param name="encodingString">
        /// The encoding string.
        /// </param>
        /// <returns>
        /// A byte array of the object's Json representation
        /// </returns>
        public static byte[] SerializeJson(object obj, string encodingString)
        {
            var jsonString = JsonConvert.SerializeObject(obj);
            var encoding = Encoding.GetEncoding(encodingString);
            var bytes = encoding.GetBytes(jsonString);
            return bytes;
        }

        /// <summary>
        /// Deserialize an object from Json
        /// </summary>
        /// <param name="bytes">
        /// The bytes.
        /// </param>
        /// <param name="encodingString">
        /// The encoding string.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <returns>
        /// An object.
        /// </returns>
        public static object DeserializeJsonAsObject(byte[] bytes, string encodingString, Type targetType)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var encoding = Encoding.GetEncoding(encodingString);

                using (TextReader reader = new StreamReader(ms, encoding, false))
                {
                    using (var jsonTextReader = new JsonTextReader(reader))
                    {
                        var jsonSerializer = new JsonSerializer();
                        var result = jsonSerializer.Deserialize(jsonTextReader, targetType);
                        return result;
                    }
                }
            }
        }

        /// <summary>
        /// Deserialize an object from Json and return the string representation of that object.
        /// </summary>
        /// <param name="bytes">
        /// The bytes.
        /// </param>
        /// <param name="encodingString">
        /// The encoding string.
        /// </param>
        /// <returns>
        /// A string representation of a Json object.
        /// </returns>
        public static string DeserializeJsonAsString(byte[] bytes, string encodingString)
        {
            return DeserializeString(bytes, encodingString);
        }
    }
}
