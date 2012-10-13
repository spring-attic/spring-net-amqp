// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitUtils.cs" company="The original author or authors.">
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
using Common.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// Utility methods for conversion between Amqp.Core and RabbitMQ
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class RabbitUtils
    {
        /// <summary>
        /// The default port.
        /// </summary>
        public static readonly int DEFAULT_PORT = Protocols.DefaultProtocol.DefaultPort;

        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>Closes the given Rabbit Connection and ignore any thrown exception.</summary>
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
                    Logger.Debug("Connection is already closed.", acex);
                }
                catch (Exception ex)
                {
                    Logger.Debug("Ignoring Connection exception - assuming already closed: ", ex);
                }
            }
        }

        /// <summary>Close the channel.</summary>
        /// <param name="channel">The channel.</param>
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
                    Logger.Debug("Could not close RabbitMQ Channel", ioex);
                }
                catch (Exception ex)
                {
                    Logger.Debug("Unexpected exception on closing RabbitMQ Channel", ex);
                }
            }
        }

        /// <summary>Commit the transaction if necessary.</summary>
        /// <param name="channel">The channel.</param>
        /// <exception cref="AmqpException"></exception>
        /// <exception cref="AmqpIOException"></exception>
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
            catch (Exception ex)
            {
                throw new AmqpException("An error occurred committing the transaction.", ex);
            }
        }

        /// <summary>Rollback the transaction if necessary.</summary>
        /// <param name="channel">The channel.</param>
        /// <exception cref="AmqpException"></exception>
        /// <exception cref="AmqpIOException"></exception>
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
            catch (Exception ex)
            {
                throw new AmqpException("An error occurred rolling back the transaction.", ex);
            }
        }

        /// <summary>Convert Rabbit Exceptions to Amqp Exceptions.</summary>
        /// <param name="ex">The ex.</param>
        /// <returns>The Exception.</returns>
        public static SystemException ConvertRabbitAccessException(Exception ex)
        {
            AssertUtils.ArgumentNotNull(ex, "Exception must not be null");
            if (ex is AmqpException)
            {
                return (AmqpException)ex;
            }

            if (ex is IOException)
            {
                return new AmqpIOException(ex);
            }

            if (ex is OperationInterruptedException)
            {
                return new AmqpIOException(new AmqpException(ex.Message, ex));
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
            return new UncategorizedAmqpException(ex);
        }

        /// <summary>Close the message consumer.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="consumerTag">The consumer tag.</param>
        /// <param name="transactional">The transactional.</param>
        /// <exception cref="SystemException"></exception>
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

        /// <summary>Declare to that broker that a channel is going to be used transactionally, and convert exceptions that arise.</summary>
        /// <param name="channel">The channel to use.</param>
        /// <exception cref="SystemException"></exception>
        public static void DeclareTransactional(IModel channel)
        {
            try
            {
                channel.TxSelect();
            }
            catch (Exception e)
            {
                throw ConvertRabbitAccessException(e);
            }
        }
    }
}
