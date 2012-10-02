// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AbstractRetryOperationsInterceptorFactoryObject.cs" company="The original author or authors.">
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
using AopAlliance.Aop;
using Spring.Messaging.Amqp.Rabbit.Retry;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    // Spring Retry not implemented, so this is excluded from the solution.
    /// <summary>The abstract retry operations interceptor factory object.</summary>
    public abstract class AbstractRetryOperationsInterceptorFactoryObject : FactoryObject<IAdvice>
    {
        private IMessageRecoverer messageRecoverer;

        private RetryOperations retryTemplate;

        /// <summary>Gets or sets the retry operations.</summary>
        public RetryOperations RetryOperations { get { return this.retryTemplate; } set { this.retryTemplate = value; } }

        /// <summary>Gets or sets the message recoverer.</summary>
        public IMessageRecoverer MessageRecoverer { get { return this.messageRecoverer; } set { this.messageRecoverer = value; } }

        /// <summary>Gets a value indicating whether is singleton.</summary>
        public bool IsSingleton { get { return true; } }
    }
}
