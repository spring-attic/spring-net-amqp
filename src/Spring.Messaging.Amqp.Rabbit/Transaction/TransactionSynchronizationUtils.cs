// -----------------------------------------------------------------------
// <copyright file="TransactionSynchronizationUtils.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using Common.Logging;
using Spring.Collections;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Transaction.Support;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Transaction
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /**
     * Utility methods for triggering specific {@link TransactionSynchronization}
     * callback methods on all currently registered synchronizations.
     *
     * @author Juergen Hoeller
     * @since 2.0
     * @see TransactionSynchronization
     * @see TransactionSynchronizationManager#getSynchronizations()
     */

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
            //    resourceRef = ((InfrastructureProxy) resourceRef).getWrappedObject();
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
        public static void TriggerFlush()
        {
            // foreach (var synchronization in TransactionSynchronizationManager.Synchronizations.ToGenericList<ITransactionSynchronization>()) {
            //	synchronization.Flush();
            //}
        }

        /**
         * Trigger <code>beforeCommit</code> callbacks on all currently registered synchronizations.
         * @param readOnly whether the transaction is defined as read-only transaction
         * @throws RuntimeException if thrown by a <code>beforeCommit</code> callback
         * @see TransactionSynchronization#beforeCommit(boolean)
         */
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
        public static void TriggerAfterCommit() { InvokeAfterCommit(TransactionSynchronizationManager.Synchronizations.ToGenericList<ITransactionSynchronization>()); }

        /**
         * Actually invoke the <code>afterCommit</code> methods of the
         * given Spring TransactionSynchronization objects.
         * @param synchronizations List of TransactionSynchronization objects
         * @see TransactionSynchronization#afterCommit()
         */
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
            public static object UnwrapIfNecessary(object resource)
            {
                // if (resource is ScopedObject) {
                //   return ((ScopedObject) resource).getTargetObject();
                // }
                // else {
                return resource;
                // }
            }
        }
    }
}
