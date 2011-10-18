
using System;
using System.Collections;
using System.Collections.Generic;
using Spring.Messaging.Amqp.Core;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// Default implementation of the {@link MessagePropertiesConverter} strategy.
    /// </summary>
    /// <author>Mark Fisher</author>
    /// <author>Joe Fitzgerald</author>
    public class DefaultMessagePropertiesConverter : IMessagePropertiesConverter
    {
        /// <summary>
        /// Converts from BasicProperties to MessageProperties.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="envelope">The envelope.</param>
        /// <param name="charset">The charset.</param>
        /// <returns>The message properties.</returns>
        public Amqp.Core.MessageProperties ToMessageProperties(RabbitMQ.Client.IBasicProperties source, RabbitMQ.Client.BasicGetResult envelope, string charset)
        {
            var target = new MessageProperties();
            var headers = source.Headers;
            if (headers != null && headers.Count > 0)
            {
                foreach (DictionaryEntry entry in headers)
                {
                    var value = entry.Value;

                    /*
                    if (value is LongString) 
                    {
                        value = this.convertLongString((LongString) value, charset);
                    }
                    */

                    target.Headers[(string)entry.Key] = value;
                }
            }

            target.Timestamp = source.Timestamp.ToDateTime();
            target.MessageId = source.MessageId;
            target.UserId = source.UserId;
            target.AppId = source.AppId;
            target.ClusterId = source.ClusterId;
            target.Type = source.Type;
            target.DeliveryMode = (MessageDeliveryMode)source.DeliveryMode;
            target.Expiration = source.Expiration;
            target.Priority = source.Priority;
            target.ContentType = source.ContentType;
            target.ContentEncoding = source.ContentEncoding;
            var correlationId = source.CorrelationId;
            if (correlationId != null)
            {
                try
                {
                    target.CorrelationId = source.CorrelationId.ToByteArrayWithEncoding(charset);
                }
                catch (Exception ex)
                {
                    throw new AmqpUnsupportedEncodingException(ex);
                }
            }

            var replyTo = source.ReplyTo;
            if (replyTo != null)
            {
                target.ReplyTo = replyTo;
            }

            if (envelope != null)
            {
                target.ReceivedExchange = envelope.Exchange;
                target.ReceivedRoutingKey = envelope.RoutingKey;
                target.Redelivered = envelope.Redelivered;
                target.DeliveryTag = (long)envelope.DeliveryTag;
            }

            return target;
        }

        /// <summary>
        /// Converts from message properties to basic properties.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="source">The source.</param>
        /// <param name="charset">The charset.</param>
        /// <returns>The basic properties.</returns>
        public RabbitMQ.Client.IBasicProperties FromMessageProperties(RabbitMQ.Client.IModel channel, Amqp.Core.MessageProperties source, string charset)
        {
            var target = channel.CreateBasicProperties();

            // target.Headers = this.ConvertHeadersIfNecessary(source.Headers == null ? target.Headers : source.Headers);
            target.Headers = this.ConvertHeadersIfNecessary(source.Headers);
            target.Timestamp = source.Timestamp.ToAmqpTimestamp();

            if (source.MessageId != null)
            {
                target.MessageId = source.MessageId;
            }

            if (source.UserId != null)
            {
                target.UserId = source.UserId;
            }

            if (source.AppId != null)
            {
                target.AppId = source.AppId;
            }

            if (source.ClusterId != null)
            {
                target.ClusterId = source.ClusterId;
            }

            if (source.Type != null)
            {
                target.Type = source.Type;
            }

            target.DeliveryMode = (byte)((int)source.DeliveryMode);

            if (source.Expiration != null)
            {
                target.Expiration = source.Expiration;
            }

            target.Priority = (byte)source.Priority;

            if (source.ContentType != null)
            {
                target.ContentType = source.ContentType;
            }

            if (source.ContentEncoding != null)
            {
                target.ContentEncoding = source.ContentEncoding;
            }

            if (source.CorrelationId != null && source.CorrelationId.Length > 0)
            {
                try
                {
                    target.CorrelationId = source.CorrelationId.ToStringWithEncoding(charset);
                }
                catch (Exception ex)
                {
                    throw new AmqpUnsupportedEncodingException(ex);
                }
            }

            if (source.ReplyTo != null)
            {
                target.ReplyTo = source.ReplyTo.ToString();
            }

            return target;
        }

        /// <summary>
        /// Converts the headers if necessary.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns>The converted headers.</returns>
        private IDictionary ConvertHeadersIfNecessary(IDictionary<string, object> headers)
        {
            if (CollectionUtils.IsEmpty(headers))
            {
                return new Dictionary<string, object>();
            }

            var writableHeaders = new Dictionary<string, object>();
            foreach (var entry in headers)
            {
                writableHeaders.Add((string)entry.Key, this.ConvertHeaderValueIfNecessary(entry.Value));
            }

            return writableHeaders;
        }

        /// <summary>
        /// Converts the header value if necessary.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The converted value.</returns>
        private object ConvertHeaderValueIfNecessary(object value)
        {
            var valid = (value is string)
                         || (value is byte[])
                         || (value is bool)

                         // || (value is LongString)
                         || (value is int)
                         || (value is long)
                         || (value is float)
                         || (value is double)
                         || (value is decimal) // BigDecimal doesn't exist...
                         || (value is short)
                         || (value is byte)
                         || (value is DateTime)
                         || (value is IList)
                         || (value is IDictionary);

            if (!valid && value != null)
            {
                value = value.ToString();
            }

            return value;
        }

        /**
        * Converts a LongString value to either a String or DataInputStream based on a length-driven threshold. If the
        * length is 1024 bytes or less, a String will be returned, otherwise a DataInputStream is returned.
        */
        /*private string ConvertLongString(string longString, string charset)
        {
            return longString;

            // Not sure we need this on the .NET side...
            
            try
            {
                if (longString.length() <= 1024)
                {
                    return new String(longString.getBytes(), charset);
                }
                else
                {
                    return longString.getStream();
                }
            }
            catch (Exception e)
            {
                throw RabbitUtils.ConvertRabbitAccessException(e);
            }
        }*/
    }
}
