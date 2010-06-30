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
using Common.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Impl;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class RabbitUtils
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(RabbitUtils));

        public static int DEFAULT_PORT = 5672;

        public static void CloseChannel(IModel channel)
        {
            if (channel != null)
            {
                try
                {
                    channel.Close();
                }
                //TODO should this really be trace level?
                catch (Exception ex)
                {
                    logger.Trace("Unexpected exception on closing RabbitMQ Channel", ex);
                }
            }
        }

        public static void CommitIfNecessary(IModel channel){
            AssertUtils.ArgumentNotNull(channel, "Channel must not be null");
		    channel.TxCommit();
        }

        public static IBasicProperties ExtractBasicProperties(IModel channel, Message message)
        {
            MessageProperties properties = (MessageProperties) message.MessageProperties;
            return properties.BasicProperties;
            /*
            IBasicProperties bp = channel.CreateBasicProperties();
            IMessageProperties messageProperties = message.MessageProperties;

            if (messageProperties.AppId != null)
            {
                bp.AppId = messageProperties.AppId;
            }
            if (messageProperties.ContentEncoding != null)
            {
                bp.ContentEncoding = messageProperties.ContentEncoding;
            }
            if (messageProperties.ContentType != null)
            {
                bp.ContentType = messageProperties.ContentType;
            }
            if (messageProperties.CorrelationId != null)
            {
                bp.CorrelationId = messageProperties.CorrelationId;
            }
            if (messageProperties.DeliveryMode != null)
            {
                bp.DeliveryMode = messageProperties.DeliveryMode.GetValueOrDefault();
            }
            if (messageProperties.Expiration != null)
            {
                bp.Expiration = messageProperties.Expiration;
            }
            if (messageProperties.Headers != null)
            {
                bp.Headers = messageProperties.Headers;
            }
            if (messageProperties.Id != null)
            {
                bp.MessageId = messageProperties.Id;
            }
            if (messageProperties.Priority != null)
            {
                bp.Priority = messageProperties.Priority.GetValueOrDefault();
            }
            if (messageProperties.ReplyTo != null)
            {
                bp.ReplyTo = messageProperties.ReplyTo;
            }

            if (messageProperties.UserId != null)
            {
                bp.UserId = messageProperties.UserId;
            }
            */
            //TODO - copy in ClusterId and Type properties

            /*
            StringBuilder sb = new StringBuilder();
            bp.AppendPropertyDebugStringTo(sb);
            Console.WriteLine(sb.ToString());*/
            //return bp;
        }

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
                } catch (Exception ex)
                {
                    logger.Debug("Ignoring Connection exception - assuming already closed: ", ex);
                }


            }
        }

        public static void RollbackIfNecessary(IModel channel)
        {
            AssertUtils.ArgumentNotNull(channel, "Channel must not be null");
            try
            {
                channel.TxRollback();
                //TODO investiage if a more specific exception is thrown
            } catch (Exception ex)
            {
                logger.Error("Could not rollback Rabbit Channel", ex);
            }
        }

        public static void CloseMessageConsumer(IModel channel, string consumerTag)
        {
            channel.BasicCancel(consumerTag);            
        }
    }

}