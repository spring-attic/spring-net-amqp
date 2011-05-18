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
using System.Collections;
using System.IO;
using System.Text;
using Common.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Utils;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// Utility methods for conversion between Amqp.Core and RabbitMQ
    /// </summary>
    /// <author>Mark Pollack</author>
	/// <author>Joe Fitzgerald</author>
    public class RabbitUtils
    {
        /// <summary>
        /// The default port.
        /// </summary>
        public static readonly int DEFAULT_PORT = RabbitMQ.Client.Protocols.DefaultProtocol.DefaultPort;

        /// <summary>
        /// The logger.
        /// </summary>
        private static readonly ILog logger = LogManager.GetLogger(typeof(RabbitUtils));

        /// <summary>
        /// Closes the given Rabbit Connection and ignore any thrown exception.
        /// </summary>
        /// <remarks>This is useful for typical 'finally' blocks in manual Rabbit
        /// code</remarks>
        /// <param name="connection">The connection to close (may be nul).</param>
        public static void CloseConnection(IConnection connection)
        {
            if (connection != null)
            {
                try
                {
                    connection.Close();
                }
                catch (AlreadyClosedException acex)
                {
                    logger.Debug("Connection is already closed.", acex);
                }
                catch (Exception ex)
                {
                    logger.Debug("Ignoring Connection exception - assuming already closed: ", ex);
                }


            }
        }

        /// <summary>
        /// Close the channel.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        public static void CloseChannel(IModel channel)
        {
            if (channel != null && channel.IsOpen)
            {
                try
                {
                    channel.Close();
                }
                catch (IOException ioex)
                {
                    logger.Debug("Could not close RabbitMQ Channel", ioex);
                }
                catch (Exception ex)
                {
                    logger.Debug("Unexpected exception on closing RabbitMQ Channel", ex);
                }
            }
        }

        /// <summary>
        /// Commit the transaction if necessary.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <exception cref="AmqpException">
        /// </exception>
        /// <exception cref="AmqpIOException">
        /// </exception>
        public static void CommitIfNecessary(IModel channel)
        {
            AssertUtils.ArgumentNotNull(channel, "Channel must not be null");
            try
            {
                channel.TxCommit();
            }
            catch (OperationInterruptedException oiex)
            {
                throw new AmqpException("An error occurred committing the transaction.", oiex);
            }
            catch (IOException ioex)
            {
                throw new AmqpIOException("An error occurred committing the transaction.", ioex);
            }
        }

        /// <summary>
        /// Rollback the transaction if necessary.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <exception cref="AmqpException">
        /// </exception>
        /// <exception cref="AmqpIOException">
        /// </exception>
        public static void RollbackIfNecessary(IModel channel)
        {
            AssertUtils.ArgumentNotNull(channel, "Channel must not be null");
            try
            {
                channel.TxRollback();
            }
            catch (OperationInterruptedException oiex)
            {
                throw new AmqpException("An error occurred rolling back the transaction.", oiex);
            }
            catch (IOException ex)
            {
                throw new AmqpIOException("An error occurred rolling back the transaction.", ex);
            }
        }

        /// <summary>
        /// Convert Rabbit Exceptions to Amqp Exceptions.
        /// </summary>
        /// <param name="ex">
        /// The ex.
        /// </param>
        /// <returns>
        /// The Exception.
        /// </returns>
        public static SystemException ConvertRabbitAccessException(Exception ex)
        {
            AssertUtils.ArgumentNotNull(ex, "Exception must not be null");
            if (ex is AmqpException)
            {
                return (AmqpException)ex;
            }

            if (ex is IOException)
            {
                return new AmqpIOException(string.Empty, (IOException)ex);
            }

            /*
            if (ex is ShutdownSignalException)
            {
                return new AmqpConnectException((ShutdownSignalException)ex);
            }
            
            if (ex is ConnectException)
            {
                return new AmqpConnectException((ConnectException)ex);
            }
            
            if (ex is UnsupportedEncodingException)
            {
                return new AmqpUnsupportedEncodingException(ex);
            }
            */

            // fallback
            return new UncategorizedAmqpException(string.Empty, ex);
        }


        /// <summary>
        /// Close the message consumer.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="consumerTag">
        /// The consumer tag.
        /// </param>
        /// <param name="transactional">
        /// The transactional.
        /// </param>
        /// <exception cref="SystemException">
        /// </exception>
        public static void CloseMessageConsumer(IModel channel, string consumerTag, bool transactional)
        {
            if (!channel.IsOpen)
            {
                return;
            }

            try
            {
                channel.BasicCancel(consumerTag);
                if (transactional)
                {
                    /*
                     * Re-queue in-flight messages if any (after the consumer is cancelled to prevent the broker from simply
                     * sending them back to us). Does not require a tx.commit.
                     */
                    channel.BasicRecover(true);
                }
            }
            catch (Exception ex)
            {
                throw ConvertRabbitAccessException(ex);
            }
        }

        /// <summary>
        /// Create MessageProperties.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="envelope">
        /// The envelope.
        /// </param>
        /// <param name="charset">
        /// The charset.
        /// </param>
        /// <returns>
        /// The MessageProperties.
        /// </returns>
        /// <exception cref="AmqpUnsupportedEncodingException">
        /// </exception>
        public static MessageProperties CreateMessageProperties(IBasicProperties source, BasicGetResult envelope, string charset)
        {
            var target = new MessageProperties();
            var headers = source.Headers;
            if (!CollectionUtils.IsEmpty(headers))
            {
                foreach (DictionaryEntry entry in headers)
                {
                    target.Headers[entry.Key] = entry.Value;
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
                    // TODO: Get the encoding from the ContentEncoding string.
                    target.CorrelationId = Encoding.UTF8.GetBytes(source.CorrelationId);
                }
                catch (Exception ex)
                {
                    throw new AmqpUnsupportedEncodingException(ex);
                }
            }

            var replyTo = source.ReplyTo;
            if (replyTo != null)
            {
                target.ReplyTo = new Address(replyTo);
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
        /// Extract BasicProperties from Message MessageProperties.
        /// </summary>
        /// <param name="channel">
        /// The channel.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="charset">
        /// The charset.
        /// </param>
        /// <returns>
        /// The BasicProperties.
        /// </returns>
        /// <exception cref="AmqpUnsupportedEncodingException">
        /// </exception>
        public static IBasicProperties ExtractBasicProperties(IModel channel, Message message, string charset)
        {
            if (message == null || message.MessageProperties == null)
            {
                return null;
            }

            var source = message.MessageProperties;
            
            var target = channel.CreateBasicProperties();
            target.Headers = source.Headers == null ? target.Headers : source.Headers;
            target.Timestamp = source.Timestamp == null ? target.Timestamp : source.Timestamp.ToAmqpTimestamp();
            target.MessageId = string.IsNullOrEmpty(source.MessageId) ? target.MessageId : source.MessageId;
            target.UserId = string.IsNullOrEmpty(source.UserId) ? target.UserId : source.UserId;
            target.AppId = string.IsNullOrEmpty(source.AppId) ? target.AppId : source.AppId;
            target.ClusterId = string.IsNullOrEmpty(source.ClusterId) ? target.ClusterId : source.ClusterId;
            target.Type = string.IsNullOrEmpty(source.Type) ? target.Type : source.Type;
            target.DeliveryMode = source.DeliveryMode == null ? target.DeliveryMode : (byte)((int)source.DeliveryMode);
            target.Expiration = string.IsNullOrEmpty(source.Expiration) ? target.Expiration : source.Expiration;
            target.Priority = source.Priority == null ? target.Priority : (byte)source.Priority;
            target.ContentType = string.IsNullOrEmpty(source.ContentType) ? target.ContentType : source.ContentType;
            target.ContentEncoding = string.IsNullOrEmpty(source.ContentEncoding) ? target.ContentEncoding : source.ContentEncoding;
            var correlationId = source.CorrelationId;
            if (correlationId != null && correlationId.Length > 0)
            {
                try
                {
                    target.CorrelationId = SerializationUtils.DeserializeString(correlationId, charset);
                }
                catch (Exception ex)
                {
                    throw new AmqpUnsupportedEncodingException(ex);
                }
            }

            var replyTo = source.ReplyTo;
            if (replyTo != null)
            {
                target.ReplyTo = replyTo.ToString();
            }

            return target;
        }

        /// <summary>
        /// Declare to that broker that a channel is going to be used transactionally, and convert exceptions that arise.
        /// </summary>
        /// <param name="channel">
        /// The channel to use.
        /// </param>
        /// <exception cref="SystemException">
        /// </exception>
        public static void DeclareTransactional(IModel channel)
        {
            try
            {
                channel.TxSelect();
            }
            catch (Exception e)
            {
                throw RabbitUtils.ConvertRabbitAccessException(e);
            }
        }
    }
}