// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitNamespaceUtils.cs" company="The original author or authors.">
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
using System.Xml;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// Rabbit Namespace Utilities
    /// </summary>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class RabbitNamespaceUtils
    {
        private static readonly string CONNECTION_FACTORY_ATTRIBUTE = "connection-factory";

        private static readonly string TASK_EXECUTOR_ATTRIBUTE = "task-executor";

        private static readonly string ERROR_HANDLER_ATTRIBUTE = "error-handler";

        private static readonly string ACKNOWLEDGE_ATTRIBUTE = "acknowledge";

        private static readonly string ACKNOWLEDGE_AUTO = "auto";

        private static readonly string ACKNOWLEDGE_MANUAL = "manual";

        private static readonly string ACKNOWLEDGE_NONE = "none";

        private static readonly string TRANSACTION_MANAGER_ATTRIBUTE = "transaction-manager";

        private static readonly string CONCURRENCY_ATTRIBUTE = "concurrency";

        private static readonly string PREFETCH_ATTRIBUTE = "prefetch";

        private static readonly string CHANNEL_TRANSACTED_ATTRIBUTE = "channel-transacted";

        private static readonly string TRANSACTION_SIZE_ATTRIBUTE = "transaction-size";

        private static readonly string PHASE_ATTRIBUTE = "phase";

        private static readonly string AUTO_STARTUP_ATTRIBUTE = "auto-startup";

        private static readonly string ADVICE_CHAIN_ATTRIBUTE = "advice-chain";

        private static readonly string REQUEUE_REJECTED_ATTRIBUTE = "requeue-rejected";

        /// <summary>The parse container.</summary>
        /// <param name="containerEle">The container ele.</param>
        /// <param name="parserContext">The parser context.</param>
        /// <returns>The Spring.Objects.Factory.Config.IObjectDefinition.</returns>
        public static IObjectDefinition ParseContainer(XmlElement containerEle, ParserContext parserContext)
        {
            var containerDef = new RootObjectDefinition(typeof(SimpleMessageListenerContainer));

            // containerDef.setSource(parserContext.extractSource(containerEle));
            var connectionFactoryObjectName = "RabbitConnectionFactory";
            if (containerEle.HasAttribute(CONNECTION_FACTORY_ATTRIBUTE))
            {
                connectionFactoryObjectName = containerEle.GetAttribute(CONNECTION_FACTORY_ATTRIBUTE);
                if (string.IsNullOrWhiteSpace(connectionFactoryObjectName))
                {
                    parserContext.ReaderContext.ReportFatalException(containerEle, "Listener container 'connection-factory' attribute contains empty value.");
                }
            }

            if (!string.IsNullOrWhiteSpace(connectionFactoryObjectName))
            {
                containerDef.PropertyValues.Add("ConnectionFactory", new RuntimeObjectReference(connectionFactoryObjectName));
            }

            var taskExecutorObjectName = containerEle.GetAttribute(TASK_EXECUTOR_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(taskExecutorObjectName))
            {
                containerDef.PropertyValues.Add("TaskExecutor", new RuntimeObjectReference(taskExecutorObjectName));
            }

            var errorHandlerObjectName = containerEle.GetAttribute(ERROR_HANDLER_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(errorHandlerObjectName))
            {
                containerDef.PropertyValues.Add("ErrorHandler", new RuntimeObjectReference(errorHandlerObjectName));
            }

            var acknowledgeMode = ParseAcknowledgeMode(containerEle, parserContext);
            if (acknowledgeMode != null)
            {
                containerDef.PropertyValues.Add("AcknowledgeMode", acknowledgeMode);
            }

            var transactionManagerObjectName = containerEle.GetAttribute(TRANSACTION_MANAGER_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(transactionManagerObjectName))
            {
                containerDef.PropertyValues.Add("TransactionManager", new RuntimeObjectReference(transactionManagerObjectName));
            }

            var concurrency = containerEle.GetAttribute(CONCURRENCY_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(concurrency))
            {
                containerDef.PropertyValues.Add("ConcurrentConsumers", new TypedStringValue(concurrency));
            }

            var prefetch = containerEle.GetAttribute(PREFETCH_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(prefetch))
            {
                containerDef.PropertyValues.Add("PrefetchCount", new TypedStringValue(prefetch));
            }

            var channelTransacted = containerEle.GetAttribute(CHANNEL_TRANSACTED_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(channelTransacted))
            {
                // Note: a placeholder will pass this test, but if it resolves to true, it will be caught during container initialization
                if (acknowledgeMode.IsAutoAck() && channelTransacted.ToLower().Equals("true"))
                {
                    parserContext.ReaderContext.ReportFatalException(containerEle, "Listener Container - cannot set channel-transacted with acknowledge='NONE'");
                }

                containerDef.PropertyValues.Add("ChannelTransacted", new TypedStringValue(channelTransacted));
            }

            var transactionSize = containerEle.GetAttribute(TRANSACTION_SIZE_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(transactionSize))
            {
                containerDef.PropertyValues.Add("TxSize", new TypedStringValue(transactionSize));
            }

            var requeueRejected = containerEle.GetAttribute(REQUEUE_REJECTED_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(requeueRejected))
            {
                containerDef.PropertyValues.Add("DefaultRequeueRejected", new TypedStringValue(requeueRejected));
            }

            var phase = containerEle.GetAttribute(PHASE_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(phase))
            {
                containerDef.PropertyValues.Add("Phase", phase);
            }

            var autoStartup = containerEle.GetAttribute(AUTO_STARTUP_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(autoStartup))
            {
                containerDef.PropertyValues.Add("AutoStartup", new TypedStringValue(autoStartup));
            }

            var adviceChain = containerEle.GetAttribute(ADVICE_CHAIN_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(adviceChain))
            {
                containerDef.PropertyValues.Add("AdviceChain", new RuntimeObjectReference(adviceChain));
            }

            return containerDef;
        }

        private static AcknowledgeModeUtils.AcknowledgeMode ParseAcknowledgeMode(XmlElement element, ParserContext parserContext)
        {
            var acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
            var acknowledge = element.GetAttribute(ACKNOWLEDGE_ATTRIBUTE);
            if (!string.IsNullOrWhiteSpace(acknowledge))
            {
                if (ACKNOWLEDGE_AUTO.Equals(acknowledge))
                {
                    acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Auto;
                }
                else if (ACKNOWLEDGE_MANUAL.Equals(acknowledge))
                {
                    acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.Manual;
                }
                else if (ACKNOWLEDGE_NONE.Equals(acknowledge))
                {
                    acknowledgeMode = AcknowledgeModeUtils.AcknowledgeMode.None;
                }
                else
                {
                    parserContext.ReaderContext.ReportFatalException(element, "Invalid listener container 'acknowledge' setting [" + acknowledge + "]: only \"auto\", \"manual\", and \"none\" supported.");
                }

                return acknowledgeMode;
            }
            else
            {
                return AcknowledgeModeUtils.AcknowledgeMode.Auto;
            }
        }
    }
}
