// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TransactionSynchronizationUtils.cs" company="The original author or authors.">
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
using System.Collections.Generic;
using Common.Logging;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Transaction.Support;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Transaction
{
    /**
     * Utility methods for triggering specific {@link TransactionSynchronization}
     * callback methods on all currently registered synchronizations.
     *
     * @author Juergen Hoeller
     * @since 2.0
     * @see TransactionSynchronization
     * @see TransactionSynchronizationManager#getSynchronizations()
     */

    /// <summary>The transaction synchronization utils.</summary>
    public abstract class TransactionSynchronizationUtils
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private static readonly bool aopAvailable = true; // ClassUtils.isPresent("org.springframework.aop.scope.ScopedObject", TransactionSynchronizationUtils.class.getClassLoader());

        /**
         * Check whether the given resource transaction managers refers to the given
         * (underlying) resource factory.
         * @see ResourceTransactionManager#getResourceFactory()
         * @see org.springframework.core.InfrastructureProxy#getWrappedObject()
         */

        /// <summary>The same resource factory.</summary>
        /// <param name="tm">The tm.</param>
        /// <param name="resourceFactory">The resource factory.</param>
        /// <returns>The System.Boolean.</returns>
        public static bool SameResourceFactory(IResourceTransactionManager tm, object resourceFactory) { return UnwrapResourceIfNecessary(tm.ResourceFactory).Equals(UnwrapResourceIfNecessary(resourceFactory)); }

        /**
         * Unwrap the given resource handle if necessary; otherwise return
         * the given handle as-is.
         * @see org.springframework.core.InfrastructureProxy#getWrappedObject()
         */
        internal static object UnwrapResourceIfNecessary(object resource)
        {
            AssertUtils.ArgumentNotNull(resource, "Resource must not be null");
            var resourceRef = resource;

            // unwrap infrastructure proxy
            // if (resourceRef is IInfrastructureProxy) 
            // {
            // resourceRef = ((InfrastructureProxy) resourceRef).getWrappedObject();
            // }
            if (aopAvailable)
            {
                // now unwrap scoped proxy
                resourceRef = ScopedProxyUnwrapper.UnwrapIfNecessary(resourceRef);
            }

            return resourceRef;
        }

        /**
         * Trigger <code>flush</code> callbacks on all currently registered synchronizations.
         * @throws RuntimeException if thrown by a <code>flush</code> callback
         * @see TransactionSynchronization#flush()
         */

        /// <summary>The trigger flush.</summary>
        public static void TriggerFlush()
        {
            // foreach (var synchronization in TransactionSynchronizationManager.Synchronizations.ToGenericList<ITransactionSynchronization>()) {
            // 	synchronization.Flush();
            // }
        }

        /**
         * Trigger <code>beforeCommit</code> callbacks on all currently registered synchronizations.
         * @param readOnly whether the transaction is defined as read-only transaction
         * @throws RuntimeException if thrown by a <code>beforeCommit</code> callback
         * @see TransactionSynchronization#beforeCommit(boolean)
         */

        /// <summary>The trigger before commit.</summary>
        /// <param name="readOnly">The read only.</param>
        public static void TriggerBeforeCommit(bool readOnly)
        {
            foreach (var synchronization in TransactionSynchronizationManager.Synchronizations.ToGenericList<ITransactionSynchronization>())
            {
                synchronization.BeforeCommit(readOnly);
            }
        }

        /**
         * Trigger <code>beforeCompletion</code> callbacks on all currently registered synchronizations.
         * @see TransactionSynchronization#beforeCompletion()
         */

        /// <summary>The trigger before completion.</summary>
        public static void TriggerBeforeCompletion()
        {
            foreach (var synchronization in TransactionSynchronizationManager.Synchronizations.ToGenericList<ITransactionSynchronization>())
            {
                try
                {
                    synchronization.BeforeCompletion();
                }
                catch (Exception tsex)
                {
                    Logger.Error("TransactionSynchronization.beforeCompletion threw exception", tsex);
                }
            }
        }

        /**
         * Trigger <code>afterCommit</code> callbacks on all currently registered synchronizations.
         * @throws RuntimeException if thrown by a <code>afterCommit</code> callback
         * @see TransactionSynchronizationManager#getSynchronizations()
         * @see TransactionSynchronization#afterCommit()
         */

        /// <summary>The trigger after commit.</summary>
        public static void TriggerAfterCommit() { InvokeAfterCommit(TransactionSynchronizationManager.Synchronizations.ToGenericList<ITransactionSynchronization>()); }

        /**
         * Actually invoke the <code>afterCommit</code> methods of the
         * given Spring TransactionSynchronization objects.
         * @param synchronizations List of TransactionSynchronization objects
         * @see TransactionSynchronization#afterCommit()
         */

        /// <summary>The invoke after commit.</summary>
        /// <param name="synchronizations">The synchronizations.</param>
        public static void InvokeAfterCommit(IList<ITransactionSynchronization> synchronizations)
        {
            if (synchronizations != null)
            {
                foreach (var synchronization in synchronizations)
                {
                    synchronization.AfterCommit();
                }
            }
        }

        /**
         * Trigger <code>afterCompletion</code> callbacks on all currently registered synchronizations.
         * @see TransactionSynchronizationManager#getSynchronizations()
         * @param completionStatus the completion status according to the
         * constants in the TransactionSynchronization interface
         * @see TransactionSynchronization#afterCompletion(int)
         * @see TransactionSynchronization#STATUS_COMMITTED
         * @see TransactionSynchronization#STATUS_ROLLED_BACK
         * @see TransactionSynchronization#STATUS_UNKNOWN
         */

        /// <summary>The trigger after completion.</summary>
        /// <param name="completionStatus">The completion status.</param>
        public static void TriggerAfterCompletion(TransactionSynchronizationStatus completionStatus)
        {
            var synchronizations = TransactionSynchronizationManager.Synchronizations.ToGenericList<ITransactionSynchronization>();
            InvokeAfterCompletion(synchronizations, completionStatus);
        }

        /**
         * Actually invoke the <code>afterCompletion</code> methods of the
         * given Spring TransactionSynchronization objects.
         * @param synchronizations List of TransactionSynchronization objects
         * @param completionStatus the completion status according to the
         * constants in the TransactionSynchronization interface
         * @see TransactionSynchronization#afterCompletion(int)
         * @see TransactionSynchronization#STATUS_COMMITTED
         * @see TransactionSynchronization#STATUS_ROLLED_BACK
         * @see TransactionSynchronization#STATUS_UNKNOWN
         */

        /// <summary>The invoke after completion.</summary>
        /// <param name="synchronizations">The synchronizations.</param>
        /// <param name="completionStatus">The completion status.</param>
        public static void InvokeAfterCompletion(IList<ITransactionSynchronization> synchronizations, TransactionSynchronizationStatus completionStatus)
        {
            if (synchronizations != null)
            {
                foreach (var synchronization in synchronizations)
                {
                    try
                    {
                        synchronization.AfterCompletion(completionStatus);
                    }
                    catch (Exception tsex)
                    {
                        Logger.Error("TransactionSynchronization.afterCompletion threw exception", tsex);
                    }
                }
            }
        }

        /**
         * Inner class to avoid hard-coded dependency on AOP module.
         */
        private static class ScopedProxyUnwrapper
        {
            /// <summary>The unwrap if necessary.</summary>
            /// <param name="resource">The resource.</param>
            /// <returns>The System.Object.</returns>
            public static object UnwrapIfNecessary(object resource)
            {
                // if (resource is ScopedObject) {
                // return ((ScopedObject) resource).getTargetObject();
                // }
                // else {
                return resource;

                // }
            }
        }
    }
}
