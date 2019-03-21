#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using log4net;
using org.apache.qpid.client;
using org.apache.qpid.transport;
using Spring.Util;

namespace Spring.Messaging.Amqp.Qpid.Support
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class QpidUtils
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(QpidUtils));

        public static int DEFAULT_PORT = 5672;

        public static void CloseChannel(IClientSession channel)
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
                    logger.Debug("Unexpected exception on closing RabbitMQ Channel", ex);
                }
            }
        }

        public static void CommitIfNecessary(IClientSession channel){
            AssertUtils.ArgumentNotNull(channel, "Channel must not be null");
            channel.TxCommit();
        }

        /*
        public static IBasicProperties ExtractBasicProperties(IClientSession channel, Message message)
        {
            MessageProperties properties = (MessageProperties) message.MessageProperties;
            return properties.BasicProperties;
           
        }*/

        public static DeliveryProperties ExtractDeliveryProperties(Spring.Messaging.Amqp.Core.Message message)
        {
            Spring.Messaging.Amqp.Qpid.Core.MessageProperties properties = (Spring.Messaging.Amqp.Qpid.Core.MessageProperties)message.MessageProperties;
            return properties.DeliveryProperites;
        }

        /// <summary>
        /// Closes the given Rabbit Connection and ignore any thrown exception.
        /// </summary>
        /// <remarks>This is useful for typical 'finally' blocks in manual Rabbit
        /// code</remarks>
        /// <param name="connection">The connection to close (may be nul).</param>
        public static void CloseClient(IClient connection)
        {
            if (connection != null)
            {
                try
                {
                    connection.Close();
                } catch (Exception ex)
                {
                    logger.Debug("Ignoring Client exception - assuming already closed: ", ex);
                }


            }
        }

        public static void RollbackIfNecessary(IClientSession channel)
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

        /*
        public static void CloseMessageConsumer(IClientSession channel, string consumerTag)
        {
            //TODO - find equivalent in qpid
            channel.BasicCancel(consumerTag);            
        }*/
    }
}