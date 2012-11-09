// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TransactionTemplateFactory.cs" company="The original author or authors.">
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
using System.Data;
using Spring.Transaction;
using Spring.Transaction.Interceptor;
using Spring.Transaction.Support;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Transaction
{
    /// <summary>
    /// Transaction Template Factory
    /// </summary>
    public static class TransactionTemplateFactory
    {
        /// <summary>The new.</summary>
        /// <param name="transactionManager">The transaction manager.</param>
        /// <param name="transactionAttribute">The transaction attribute.</param>
        /// <returns>The Spring.Transaction.Support.TransactionTemplate.</returns>
        public static TransactionTemplate New(IPlatformTransactionManager transactionManager, ITransactionAttribute transactionAttribute)
        {
            var transactionTemplate = new TransactionTemplate(transactionManager);
            transactionTemplate.PropagationBehavior = transactionAttribute.PropagationBehavior;
            transactionTemplate.TransactionIsolationLevel = IsolationLevel.Unspecified; // TODO: revert to transactionAttribute once we take dependency on SPRNET 2.0
            transactionTemplate.TransactionTimeout = transactionAttribute.TransactionTimeout;
            transactionTemplate.ReadOnly = transactionAttribute.ReadOnly;
            transactionTemplate.Name = transactionAttribute.Name;
            return transactionTemplate;
        }
    }
}
