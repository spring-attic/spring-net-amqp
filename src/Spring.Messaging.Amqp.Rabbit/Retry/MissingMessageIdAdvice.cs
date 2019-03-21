// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MissingMessageIdAdvice.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using AopAlliance.Intercept;
using Common.Logging;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Retry
{
    /// <summary>
    /// *** NOTE: This cannot be used until Retry is implemented, perhaps as part of SPRNET 2.0 ***
    /// Advice that can be placed in the listener delegate's advice chain to
    /// enhance the message with an ID if not present.
    /// If an exception is caught on a redelivered message, rethrows it as an {@link AmqpRejectAndDontRequeueException}
    /// which signals the container to NOT requeue the message (otherwise we'd have infinite
    /// immediate retries).
    /// If so configured, the broker can send the message to a DLE/DLQ.
    /// Must be placed before the retry interceptor in the advice chain.
    /// </summary>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class MissingMessageIdAdvice : IMethodInterceptor
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        // private readonly RetryContextCache retryContextCache;

        /*
        public MissingMessageIdAdvice(RetryContextCache retryContextCache)
        {
            AssertUtils.ArgumentNotNull(retryContextCache, "RetryContextCache must not be null");
            this.retryContextCache = retryContextCache;
        }
        */

        /// <summary>The invoke.</summary>
        /// <param name="invocation">The invocation.</param>
        /// <returns>The System.Object.</returns>
        public object Invoke(IMethodInvocation invocation)
        {
            var id = string.Empty;
            var redelivered = false;
            try
            {
                var message = (Message)invocation.Arguments[1];
                var messageProperties = message.MessageProperties;
                if (string.IsNullOrWhiteSpace(messageProperties.MessageId))
                {
                    id = Guid.NewGuid().ToString();
                    messageProperties.MessageId = id;
                }

                redelivered = messageProperties.Redelivered;
                return invocation.Proceed();
            }
            catch (Exception t)
            {
                if (!string.IsNullOrWhiteSpace(id) && redelivered)
                {
                    Logger.Debug(m => m("Canceling delivery of retried message that has no ID"));
                    throw new ListenerExecutionFailedException("Cannot retry message without an ID", new AmqpRejectAndDontRequeueException(t));
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    // retryContextCache.remove(id);
                }
            }
        }
    }
}
