// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitAbstractPlatformTransactionManager.cs" company="The original author or authors.">
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
using System.Data;
using Common.Logging;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Transaction;
using Spring.Transaction.Support;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Transaction
{
    /// <summary>
    /// <para>
    /// Abstract base class that implements Spring's standard transaction workflow,
    /// serving as basis for concrete platform transaction managers like
    /// {@link org.springframework.transaction.jta.JtaTransactionManager}.
    /// </para>
    /// <para>
    /// This base class provides the following workflow handling:
    /// * determines if there is an existing transaction;
    /// * applies the appropriate propagation behavior;
    /// * suspends and resumes transactions if necessary;
    /// * checks the rollback-only flag on commit;
    /// * applies the appropriate modification on rollback (actual rollback or setting rollback-only);
    /// * triggers registered synchronization callbacks (if transaction synchronization is active).
    /// </para>
    /// <para>
    /// Subclasses have to implement specific template methods for specific
    /// states of a transaction, e.g.: begin, suspend, resume, commit, rollback.
    /// The most important of them are abstract and must be provided by a concrete
    /// implementation; for the rest, defaults are provided, so overriding is optional.
    /// </para>
    /// <para>
    /// Transaction synchronization is a generic mechanism for registering callbacks
    /// that get invoked at transaction completion time. This is mainly used internally
    /// by the data access support classes for JDBC, Hibernate, JPA, etc when running
    /// within a JTA transaction: They register resources that are opened within the
    /// transaction for closing at transaction completion time, allowing e.g. for reuse
    /// of the same Hibernate Session within the transaction. The same mechanism can
    /// also be leveraged for custom synchronization needs in an application.
    /// </para>
    /// <para> 
    /// The state of this class is serializable, to allow for serializing the
    /// transaction strategy along with proxies that carry a transaction interceptor.
    /// It is up to subclasses if they wish to make their state to be serializable too.
    /// They should implement the <code>java.io.Serializable</code> marker interface in
    /// that case, and potentially a private <code>readObject()</code> method (according
    /// to Java serialization rules) if they need to restore any transient state.
    /// </para>
    /// </summary>
    /// <author>Juergen Hoeller</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public abstract class RabbitAbstractPlatformTransactionManager : IPlatformTransactionManager
    {
        [NonSerialized]
        protected static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private TransactionSynchronizationState transactionSynchronization = TransactionSynchronizationState.Always;

        private int defaultTimeout = -1; // DefaultTransactionDefinition.TIMEOUT_DEFAULT

        private bool nestedTransactionAllowed;

        private bool validateExistingTransaction;

        private bool globalRollbackOnParticipationFailure = true;

        private bool failEarlyOnGlobalRollbackOnly;

        private bool rollbackOnCommitFailure;
        
        /// <summary>Gets or sets the transaction synchronization.</summary>
        public TransactionSynchronizationState TransactionSynchronization { get { return this.transactionSynchronization; } set { this.transactionSynchronization = value; } }

        /// <summary>Gets or sets the default timeout.</summary>
        public int DefaultTimeout
        {
            get { return this.defaultTimeout; }
            set
            {
                if (this.defaultTimeout < -1)
                {
                    throw new InvalidTimeoutException("Invalid default timeout", this.defaultTimeout);
                }

                this.defaultTimeout = value;
            }
        }

        /// <summary>Gets or sets a value indicating whether nested transaction allowed.</summary>
        public bool NestedTransactionAllowed { get { return this.nestedTransactionAllowed; } set { this.nestedTransactionAllowed = value; } }

        /// <summary>Gets or sets a value indicating whether validate existing transaction.</summary>
        public bool ValidateExistingTransaction { get { return this.validateExistingTransaction; } set { this.validateExistingTransaction = value; } }

        /// <summary>Gets or sets a value indicating whether global rollback on participation failure.</summary>
        public bool GlobalRollbackOnParticipationFailure { get { return this.globalRollbackOnParticipationFailure; } set { this.globalRollbackOnParticipationFailure = value; } }

        /// <summary>Gets or sets a value indicating whether fail early on global rollback only.</summary>
        public bool FailEarlyOnGlobalRollbackOnly { get { return this.failEarlyOnGlobalRollbackOnly; } set { this.failEarlyOnGlobalRollbackOnly = value; } }

        /// <summary>Gets or sets a value indicating whether rollback on commit failure.</summary>
        public bool RollbackOnCommitFailure { get { return this.rollbackOnCommitFailure; } set { this.rollbackOnCommitFailure = value; } }

        /// <summary>The get transaction.</summary>
        /// <param name="definition">The definition.</param>
        /// <returns>The Spring.Transaction.ITransactionStatus.</returns>
        public ITransactionStatus GetTransaction(ITransactionDefinition definition)
        {
            var transaction = this.DoGetTransaction();

            // Cache debug flag to avoid repeated checks.
            var debugEnabled = Logger.IsDebugEnabled;

            if (definition == null)
            {
                // Use defaults if no transaction definition given.
                definition = new DefaultTransactionDefinition();
            }

            if (this.IsExistingTransaction(transaction))
            {
                // Existing transaction found -> check propagation behavior to find out how to behave.
                return this.HandleExistingTransaction(definition, transaction, debugEnabled);
            }

            // Check definition settings for new transaction.
            if (definition.TransactionTimeout < -1)
            {
                throw new InvalidTimeoutException("Invalid transaction timeout", definition.TransactionTimeout);
            }

            // No existing transaction found -> check propagation behavior to find out how to proceed.
            if (definition.PropagationBehavior == TransactionPropagation.Mandatory)
            {
                throw new IllegalTransactionStateException("No existing transaction found for transaction marked with propagation 'mandatory'");
            }
            else if (definition.PropagationBehavior == TransactionPropagation.Required ||
                     definition.PropagationBehavior == TransactionPropagation.RequiresNew ||
                     definition.PropagationBehavior == TransactionPropagation.Nested)
            {
                var suspendedResources = this.Suspend(null);
                if (debugEnabled)
                {
                    Logger.Debug("Creating new transaction with name [" + definition.Name + "]: " + definition);
                }

                try
                {
                    var newSynchronization = this.transactionSynchronization != TransactionSynchronizationState.Never;
                    var status = this.NewTransactionStatus(definition, transaction, true, newSynchronization, debugEnabled, suspendedResources);
                    this.DoBegin(transaction, definition);
                    this.PrepareSynchronization(status, definition);
                    return status;
                }
                catch (Exception ex)
                {
                    this.Resume(null, suspendedResources);
                    throw ex;
                }
            }
            else
            {
                // Create "empty" transaction: no actual transaction, but potentially synchronization.
                var newSynchronization = this.transactionSynchronization == TransactionSynchronizationState.Always;
                return this.PrepareTransactionStatus(definition, null, true, newSynchronization, debugEnabled, null);
            }
        }

        /// <summary>Create a TransactionStatus for an existing transaction.</summary>
        /// <param name="definition">The transaction definition.</param>
        /// <param name="transaction">The transaction.</param>
        /// <param name="debugEnabled">Flag indicating debug is enabled.</param>
        /// <returns>The transaction status.</returns>
        private ITransactionStatus HandleExistingTransaction(ITransactionDefinition definition, object transaction, bool debugEnabled)
        {
            if (definition.PropagationBehavior == TransactionPropagation.Never)
            {
                throw new IllegalTransactionStateException(
                    "Existing transaction found for transaction marked with propagation 'never'");
            }

            if (definition.PropagationBehavior == TransactionPropagation.NotSupported)
            {
                if (debugEnabled)
                {
                    Logger.Debug("Suspending current transaction");
                }

                var suspendedResources = this.Suspend(transaction);
                var newSynchronization1 = this.transactionSynchronization == TransactionSynchronizationState.Always;
                return this.PrepareTransactionStatus(definition, null, false, newSynchronization1, debugEnabled, suspendedResources);
            }

            if (definition.PropagationBehavior == TransactionPropagation.RequiresNew)
            {
                if (debugEnabled)
                {
                    Logger.Debug(
                        "Suspending current transaction, creating new transaction with name [" +
                        definition.Name + "]");
                }

                var suspendedResources = this.Suspend(transaction);
                try
                {
                    var newSynchronization2 = this.transactionSynchronization != TransactionSynchronizationState.Never;
                    var status = this.NewTransactionStatus(definition, transaction, true, newSynchronization2, debugEnabled, suspendedResources);
                    this.DoBegin(transaction, definition);
                    this.PrepareSynchronization(status, definition);
                    return status;
                }
                catch (Exception beginEx)
                {
                    this.ResumeAfterBeginException(transaction, suspendedResources, beginEx);
                    throw;
                }
            }

            if (definition.PropagationBehavior == TransactionPropagation.Nested)
            {
                if (!this.nestedTransactionAllowed)
                {
                    throw new NestedTransactionNotSupportedException(
                        "Transaction manager does not allow nested transactions by default - " +
                        "specify 'nestedTransactionAllowed' property with value 'true'");
                }

                if (debugEnabled)
                {
                    Logger.Debug("Creating nested transaction with name [" + definition.Name + "]");
                }

                if (this.UseSavepointForNestedTransaction())
                {
                    // Create savepoint within existing Spring-managed transaction,
                    // through the SavepointManager API implemented by TransactionStatus.
                    // Usually uses JDBC 3.0 savepoints. Never activates Spring synchronization.
                    var status = this.PrepareTransactionStatus(definition, transaction, false, false, debugEnabled, null);
                    status.CreateAndHoldSavepoint(Guid.NewGuid().ToString()); // TODO: Java equivalent does not require name
                    return status;
                }
                else
                {
                    // Nested transaction through nested begin and commit/rollback calls.
                    // Usually only for JTA: Spring synchronization might get activated here
                    // in case of a pre-existing JTA transaction.
                    var newSynchronization3 = this.transactionSynchronization != TransactionSynchronizationState.Never;
                    var status = this.NewTransactionStatus(
                        definition, transaction, true, newSynchronization3, debugEnabled, null);
                    this.DoBegin(transaction, definition);
                    this.PrepareSynchronization(status, definition);
                    return status;
                }
            }

            // Assumably PROPAGATION_SUPPORTS or PROPAGATION_REQUIRED.
            if (debugEnabled)
            {
                Logger.Debug("Participating in existing transaction");
            }

            if (this.validateExistingTransaction)
            {
                if (definition.TransactionIsolationLevel != IsolationLevel.Unspecified)
                {
                    var currentIsolationLevel = TransactionSynchronizationManager.CurrentTransactionIsolationLevel;
                    if (currentIsolationLevel == null || currentIsolationLevel != definition.TransactionIsolationLevel)
                    {
                        // Constants isoConstants = DefaultTransactionDefinition.constants;
                        throw new IllegalTransactionStateException("Participating transaction with definition [" + definition + "] specifies isolation level which is incompatible with existing transaction: ");

                        // + (currentIsolationLevel != null ? isoConstants.toCode(currentIsolationLevel, DefaultTransactionDefinition.PREFIX_ISOLATION) : "(unknown)"));
                    }
                }

                if (!definition.ReadOnly)
                {
                    if (TransactionSynchronizationManager.CurrentTransactionReadOnly)
                    {
                        throw new IllegalTransactionStateException(
                            "Participating transaction with definition [" +
                            definition + "] is not marked as read-only but existing transaction is");
                    }
                }
            }

            var newSynchronization = this.transactionSynchronization != TransactionSynchronizationState.Never;
            return this.PrepareTransactionStatus(definition, transaction, false, newSynchronization, debugEnabled, null);
        }

        /// <summary>Create a new TransactionStatus for the given arguments, also initializing transaction synchronization as appropriate.</summary>
        /// <param name="definition">The definition.</param>
        /// <param name="transaction">The transaction.</param>
        /// <param name="newTransaction">The new transaction.</param>
        /// <param name="newSynchronization">The new synchronization.</param>
        /// <param name="debug">The debug.</param>
        /// <param name="suspendedResources">The suspended resources.</param>
        /// <returns>The Spring.Transaction.Support.DefaultTransactionStatus.</returns>
        protected DefaultTransactionStatus PrepareTransactionStatus(
            ITransactionDefinition definition, 
            object transaction, 
            bool newTransaction, 
            bool newSynchronization, 
            bool debug, 
            object suspendedResources)
        {
            var status = this.NewTransactionStatus(definition, transaction, newTransaction, newSynchronization, debug, suspendedResources);
            this.PrepareSynchronization(status, definition);
            return status;
        }

        /**
         * Create a rae TransactionStatus instance for the given arguments.
         */

        /// <summary>The new transaction status.</summary>
        /// <param name="definition">The definition.</param>
        /// <param name="transaction">The transaction.</param>
        /// <param name="newTransaction">The new transaction.</param>
        /// <param name="newSynchronization">The new synchronization.</param>
        /// <param name="debug">The debug.</param>
        /// <param name="suspendedResources">The suspended resources.</param>
        /// <returns>The Spring.Transaction.Support.DefaultTransactionStatus.</returns>
        protected DefaultTransactionStatus NewTransactionStatus(ITransactionDefinition definition, object transaction, bool newTransaction, bool newSynchronization, bool debug, object suspendedResources)
        {
            var actualNewSynchronization = newSynchronization && !TransactionSynchronizationManager.SynchronizationActive;
            return new DefaultTransactionStatus(transaction, newTransaction, actualNewSynchronization, definition.ReadOnly, debug, suspendedResources);
        }

        /**
         * Initialize transaction synchronization as appropriate.
         */

        /// <summary>The prepare synchronization.</summary>
        /// <param name="status">The status.</param>
        /// <param name="definition">The definition.</param>
        protected void PrepareSynchronization(DefaultTransactionStatus status, ITransactionDefinition definition)
        {
            if (status.NewSynchronization)
            {
                TransactionSynchronizationManager.ActualTransactionActive = status.HasTransaction();
                TransactionSynchronizationManager.CurrentTransactionIsolationLevel = (definition.TransactionIsolationLevel != IsolationLevel.Unspecified) ? definition.TransactionIsolationLevel : IsolationLevel.Unspecified;
                TransactionSynchronizationManager.CurrentTransactionReadOnly = definition.ReadOnly;
                TransactionSynchronizationManager.CurrentTransactionName = definition.Name;
                TransactionSynchronizationManager.InitSynchronization();
            }
        }

        /**
         * Determine the actual timeout to use for the given definition.
         * Will fall back to this manager's default timeout if the
         * transaction definition doesn't specify a non-default value.
         * @param definition the transaction definition
         * @return the actual timeout to use
         * @see org.springframework.transaction.TransactionDefinition#getTimeout()
         * @see #setDefaultTimeout
         */

        /// <summary>The determine timeout.</summary>
        /// <param name="definition">The definition.</param>
        /// <returns>The System.Int32.</returns>
        protected int DetermineTimeout(ITransactionDefinition definition)
        {
            if (definition.TransactionTimeout != -1)
            {
                return definition.TransactionTimeout;
            }

            return this.defaultTimeout;
        }

        /**
         * Suspend the given transaction. Suspends transaction synchronization first,
         * then delegates to the <code>doSuspend</code> template method.
         * @param transaction the current transaction object
         * (or <code>null</code> to just suspend active synchronizations, if any)
         * @return an object that holds suspended resources
         * (or <code>null</code> if neither transaction nor synchronization active)
         * @see #doSuspend
         * @see #resume
         */

        /// <summary>The suspend.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <returns>The Spring.Messaging.Amqp.Rabbit.Transaction.SuspendedResourcesHolder.</returns>
        protected SuspendedResourcesHolder Suspend(object transaction)
        {
            if (TransactionSynchronizationManager.SynchronizationActive)
            {
                var suspendedSynchronizations = this.DoSuspendSynchronization();
                try
                {
                    object suspendedResources = null;
                    if (transaction != null)
                    {
                        suspendedResources = this.DoSuspend(transaction);
                    }

                    var name = TransactionSynchronizationManager.CurrentTransactionName;
                    TransactionSynchronizationManager.CurrentTransactionName = null;
                    var readOnly = TransactionSynchronizationManager.CurrentTransactionReadOnly;
                    TransactionSynchronizationManager.CurrentTransactionReadOnly = false;
                    var isolationLevel = TransactionSynchronizationManager.CurrentTransactionIsolationLevel;
                    TransactionSynchronizationManager.CurrentTransactionIsolationLevel = default(IsolationLevel);
                    var wasActive = TransactionSynchronizationManager.ActualTransactionActive;
                    TransactionSynchronizationManager.ActualTransactionActive = false;
                    return new SuspendedResourcesHolder(
                        suspendedResources, suspendedSynchronizations, name, readOnly, isolationLevel, wasActive);
                }
                catch (Exception ex)
                {
                    // doSuspend failed - original transaction is still active...
                    this.DoResumeSynchronization(suspendedSynchronizations);
                    throw;
                }
            }
            else if (transaction != null)
            {
                // Transaction active but no synchronization active.
                object suspendedResources = this.DoSuspend(transaction);
                return new SuspendedResourcesHolder(suspendedResources);
            }
            else
            {
                // Neither transaction nor synchronization active.
                return null;
            }
        }

        /**
         * Resume the given transaction. Delegates to the <code>doResume</code>
         * template method first, then resuming transaction synchronization.
         * @param transaction the current transaction object
         * @param resourcesHolder the object that holds suspended resources,
         * as returned by <code>suspend</code> (or <code>null</code> to just
         * resume synchronizations, if any)
         * @see #doResume
         * @see #suspend
         */

        /// <summary>The resume.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="resourcesHolder">The resources holder.</param>
        protected void Resume(object transaction, SuspendedResourcesHolder resourcesHolder)
        {
            if (resourcesHolder != null)
            {
                object suspendedResources = resourcesHolder.suspendedResources;
                if (suspendedResources != null)
                {
                    this.DoResume(transaction, suspendedResources);
                }

                var suspendedSynchronizations = resourcesHolder.suspendedSynchronizations;
                if (suspendedSynchronizations != null)
                {
                    TransactionSynchronizationManager.ActualTransactionActive = resourcesHolder.wasActive;
                    TransactionSynchronizationManager.CurrentTransactionIsolationLevel = resourcesHolder.isolationLevel;
                    TransactionSynchronizationManager.CurrentTransactionReadOnly = resourcesHolder.readOnly;
                    TransactionSynchronizationManager.CurrentTransactionName = resourcesHolder.name;
                    this.DoResumeSynchronization(suspendedSynchronizations);
                }
            }
        }
        
        // Resume outer transaction after inner transaction begin failed.
        private void ResumeAfterBeginException(object transaction, SuspendedResourcesHolder suspendedResources, Exception beginEx)
        {
            var exceptionMessage = "Inner transaction begin exception overridden by outer transaction resume exception";
            try
            {
                this.Resume(transaction, suspendedResources);
            }
            catch (Exception resumeEx)
            {
                Logger.Error(exceptionMessage, beginEx);
                throw;
            }
        }

        /**
         * Suspend all current synchronizations and deactivate transaction
         * synchronization for the current thread.
         * @return the List of suspended TransactionSynchronization objects
         */

        private IList<ITransactionSynchronization> DoSuspendSynchronization()
        {
            var suspendedSynchronizations = TransactionSynchronizationManager.Synchronizations.ToGenericList<ITransactionSynchronization>();
            foreach (var synchronization in suspendedSynchronizations)
            {
                synchronization.Suspend();
            }

            TransactionSynchronizationManager.ClearSynchronization();
            return suspendedSynchronizations;
        }

        /**
         * Reactivate transaction synchronization for the current thread
         * and resume all given synchronizations.
         * @param suspendedSynchronizations List of TransactionSynchronization objects
         */

        private void DoResumeSynchronization(IList<ITransactionSynchronization> suspendedSynchronizations)
        {
            TransactionSynchronizationManager.InitSynchronization();
            foreach (var synchronization in suspendedSynchronizations)
            {
                synchronization.Resume();
                TransactionSynchronizationManager.RegisterSynchronization(synchronization);
            }
        }

        /// <summary>The commit.</summary>
        /// <param name="status">The status.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void Commit(ITransactionStatus status)
        {
            if (status.Completed)
            {
                throw new IllegalTransactionStateException(
                    "Transaction is already completed - do not call commit or rollback more than once per transaction");
            }

            var defStatus = (DefaultTransactionStatus)status;
            if (defStatus.LocalRollbackOnly)
            {
                if (defStatus.Debug)
                {
                    Logger.Debug("Transactional code has requested rollback");
                }

                this.ProcessRollback(defStatus);
                return;
            }

            if (!this.ShouldCommitOnGlobalRollbackOnly() && defStatus.GlobalRollbackOnly)
            {
                if (defStatus.Debug)
                {
                    Logger.Debug("Global transaction is marked as rollback-only but transactional code requested commit");
                }

                this.ProcessRollback(defStatus);

                // Throw UnexpectedRollbackException only at outermost transaction boundary
                // or if explicitly asked to.
                if (status.IsNewTransaction || this.failEarlyOnGlobalRollbackOnly)
                {
                    throw new UnexpectedRollbackException("Transaction rolled back because it has been marked as rollback-only");
                }

                return;
            }

            this.ProcessCommit(defStatus);
        }

        /**
	 * Process an actual commit.
	 * Rollback-only flags have already been checked and applied.
	 * @param status object representing the transaction
	 * @throws TransactionException in case of commit failure
	 */

        private void ProcessCommit(DefaultTransactionStatus status)
        {
            try
            {
                var beforeCompletionInvoked = false;
                try
                {
                    this.PrepareForCommit(status);
                    this.TriggerBeforeCommit(status);
                    this.TriggerBeforeCompletion(status);
                    beforeCompletionInvoked = true;
                    var globalRollbackOnly = false;
                    if (status.IsNewTransaction || this.failEarlyOnGlobalRollbackOnly)
                    {
                        globalRollbackOnly = status.GlobalRollbackOnly;
                    }

                    if (status.HasSavepoint)
                    {
                        if (status.Debug)
                        {
                            Logger.Debug("Releasing transaction savepoint");
                        }

                        status.ReleaseHeldSavepoint();
                    }
                    else if (status.IsNewTransaction)
                    {
                        if (status.Debug)
                        {
                            Logger.Debug("Initiating transaction commit");
                        }

                        this.DoCommit(status);
                    }

                    // Throw UnexpectedRollbackException if we have a global rollback-only
                    // marker but still didn't get a corresponding exception from commit.
                    if (globalRollbackOnly)
                    {
                        throw new UnexpectedRollbackException(
                            "Transaction silently rolled back because it has been marked as rollback-only");
                    }
                }
                catch (UnexpectedRollbackException ex)
                {
                    // can only be caused by doCommit
                    this.TriggerAfterCompletion(status, TransactionSynchronizationStatus.Rolledback);
                    throw;
                }
                catch (TransactionException ex)
                {
                    // can only be caused by doCommit
                    if (this.rollbackOnCommitFailure)
                    {
                        this.DoRollbackOnCommitException(status, ex);
                    }
                    else
                    {
                        this.TriggerAfterCompletion(status, TransactionSynchronizationStatus.Unknown);
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    if (!beforeCompletionInvoked)
                    {
                        this.TriggerBeforeCompletion(status);
                    }

                    this.DoRollbackOnCommitException(status, ex);
                    throw;
                }

                // Trigger afterCommit callbacks, with an exception thrown there
                // propagated to callers but the transaction still considered as committed.
                try
                {
                    this.TriggerAfterCommit(status);
                }
                finally
                {
                    this.TriggerAfterCompletion(status, TransactionSynchronizationStatus.Committed);
                }
            }
            finally
            {
                this.CleanupAfterCompletion(status);
            }
        }

        /// <summary>The rollback.</summary>
        /// <param name="transactionStatus">The transaction status.</param>
        public void Rollback(ITransactionStatus transactionStatus)
        {
            if (transactionStatus.Completed)
            {
                throw new IllegalTransactionStateException("Transaction is already completed - do not call commit or rollback more than once per transaction");
            }

            var defStatus = (DefaultTransactionStatus)transactionStatus;
            this.ProcessRollback(defStatus);
        }

        /**
	 * Process an actual rollback.
	 * The completed flag has already been checked.
	 * @param status object representing the transaction
	 * @throws TransactionException in case of rollback failure
	 */

        private void ProcessRollback(DefaultTransactionStatus status)
        {
            try
            {
                try
                {
                    this.TriggerBeforeCompletion(status);
                    if (status.HasSavepoint)
                    {
                        if (status.Debug)
                        {
                            Logger.Debug("Rolling back transaction to savepoint");
                        }

                        status.RollbackToHeldSavepoint();
                    }
                    else if (status.IsNewTransaction)
                    {
                        if (status.Debug)
                        {
                            Logger.Debug("Initiating transaction rollback");
                        }

                        this.DoRollback(status);
                    }
                    else if (status.HasTransaction())
                    {
                        if (status.LocalRollbackOnly || this.globalRollbackOnParticipationFailure)
                        {
                            if (status.Debug)
                            {
                                Logger.Debug(
                                    "Participating transaction failed - marking existing transaction as rollback-only");
                            }

                            this.DoSetRollbackOnly(status);
                        }
                        else
                        {
                            if (status.Debug)
                            {
                                Logger.Debug(
                                    "Participating transaction failed - letting transaction originator decide on rollback");
                            }
                        }
                    }
                    else
                    {
                        Logger.Debug("Should roll back transaction but cannot - no transaction available");
                    }
                }
                catch (Exception ex)
                {
                    this.TriggerAfterCompletion(status, TransactionSynchronizationStatus.Unknown);
                    throw;
                }

                this.TriggerAfterCompletion(status, TransactionSynchronizationStatus.Rolledback);
            }
            finally
            {
                this.CleanupAfterCompletion(status);
            }
        }

        /**
         * Invoke <code>doRollback</code>, handling rollback exceptions properly.
         * @param status object representing the transaction
         * @param ex the thrown application exception or error
         * @throws TransactionException in case of rollback failure
         * @see #doRollback
         */

        private void DoRollbackOnCommitException(DefaultTransactionStatus status, Exception ex)
        {
            try
            {
                if (status.IsNewTransaction)
                {
                    if (status.Debug)
                    {
                        Logger.Debug("Initiating transaction rollback after commit exception", ex);
                    }

                    this.DoRollback(status);
                }
                else if (status.HasTransaction() && this.globalRollbackOnParticipationFailure)
                {
                    if (status.Debug)
                    {
                        Logger.Debug("Marking existing transaction as rollback-only after commit exception", ex);
                    }

                    this.DoSetRollbackOnly(status);
                }
            }
            catch (Exception rbex)
            {
                Logger.Error("Commit exception overridden by rollback exception", ex);
                this.TriggerAfterCompletion(status, TransactionSynchronizationStatus.Unknown);
                throw;
            }

            this.TriggerAfterCompletion(status, TransactionSynchronizationStatus.Rolledback);
        }

        /**
	 * Trigger <code>beforeCommit</code> callbacks.
	 * @param status object representing the transaction
	 */

        /// <summary>The trigger before commit.</summary>
        /// <param name="status">The status.</param>
        protected void TriggerBeforeCommit(DefaultTransactionStatus status)
        {
            if (status.NewSynchronization)
            {
                if (status.Debug)
                {
                    Logger.Trace("Triggering beforeCommit synchronization");
                }

                TransactionSynchronizationUtils.TriggerBeforeCommit(status.ReadOnly);
            }
        }

        /**
         * Trigger <code>beforeCompletion</code> callbacks.
         * @param status object representing the transaction
         */

        /// <summary>The trigger before completion.</summary>
        /// <param name="status">The status.</param>
        protected void TriggerBeforeCompletion(DefaultTransactionStatus status)
        {
            if (status.NewSynchronization)
            {
                if (status.Debug)
                {
                    Logger.Trace("Triggering beforeCompletion synchronization");
                }

                TransactionSynchronizationUtils.TriggerBeforeCompletion();
            }
        }

        /**
         * Trigger <code>afterCommit</code> callbacks.
         * @param status object representing the transaction
         */

        private void TriggerAfterCommit(DefaultTransactionStatus status)
        {
            if (status.NewSynchronization)
            {
                if (status.Debug)
                {
                    Logger.Trace("Triggering afterCommit synchronization");
                }

                TransactionSynchronizationUtils.TriggerAfterCommit();
            }
        }

        /**
         * Trigger <code>afterCompletion</code> callbacks.
         * @param status object representing the transaction
         * @param completionStatus completion status according to TransactionSynchronization constants
         */

        private void TriggerAfterCompletion(DefaultTransactionStatus status, TransactionSynchronizationStatus completionStatus)
        {
            if (status.NewSynchronization)
            {
                var synchronizations = TransactionSynchronizationManager.Synchronizations.ToGenericList<ITransactionSynchronization>();
                if (synchronizations != null)
                {
                    if (!status.HasTransaction() || status.NewSynchronization)
                    {
                        if (status.Debug)
                        {
                            Logger.Trace("Triggering afterCompletion synchronization");
                        }

                        // No transaction or new transaction for the current scope ->
                        // invoke the afterCompletion callbacks immediately
                        this.InvokeAfterCompletion(synchronizations, completionStatus);
                    }
                    else if (!(synchronizations.Count < 1))
                    {
                        // Existing transaction that we participate in, controlled outside
                        // of the scope of this Spring transaction manager -> try to register
                        // an afterCompletion callback with the existing (JTA) transaction.
                        this.RegisterAfterCompletionWithExistingTransaction(status.Transaction, synchronizations);
                    }
                }
            }
        }

        /**
	 * Actually invoke the <code>afterCompletion</code> methods of the
	 * given Spring TransactionSynchronization objects.
	 * <p>To be called by this abstract manager itself, or by special implementations
	 * of the <code>registerAfterCompletionWithExistingTransaction</code> callback.
	 * @param synchronizations List of TransactionSynchronization objects
	 * @param completionStatus the completion status according to the
	 * constants in the TransactionSynchronization interface
	 * @see #registerAfterCompletionWithExistingTransaction(Object, java.util.List)
	 * @see TransactionSynchronization#STATUS_COMMITTED
	 * @see TransactionSynchronization#STATUS_ROLLED_BACK
	 * @see TransactionSynchronization#STATUS_UNKNOWN
	 */

        /// <summary>The invoke after completion.</summary>
        /// <param name="synchronizations">The synchronizations.</param>
        /// <param name="completionStatus">The completion status.</param>
        protected void InvokeAfterCompletion(IList<ITransactionSynchronization> synchronizations, TransactionSynchronizationStatus completionStatus) { TransactionSynchronizationUtils.InvokeAfterCompletion(synchronizations, completionStatus); }

        /**
	 * Clean up after completion, clearing synchronization if necessary,
	 * and invoking doCleanupAfterCompletion.
	 * @param status object representing the transaction
	 * @see #doCleanupAfterCompletion
	 */

        private void CleanupAfterCompletion(DefaultTransactionStatus status)
        {
            status.Completed = true;
            if (status.IsNewTransaction)
            {
                TransactionSynchronizationManager.Clear();
            }

            if (status.IsNewTransaction)
            {
                this.DoCleanupAfterCompletion(status.Transaction);
            }

            if (status.SuspendedResources != null)
            {
                if (status.Debug)
                {
                    Logger.Debug("Resuming suspended transaction after completion of inner transaction");
                }

                this.Resume(status.Transaction, (SuspendedResourcesHolder)status.SuspendedResources);
            }
        }

        // ---------------------------------------------------------------------
        // Template methods to be implemented in subclasses
        // ---------------------------------------------------------------------

        /**
         * Return a transaction object for the current transaction state.
         * <p>The returned object will usually be specific to the concrete transaction
         * manager implementation, carrying corresponding transaction state in a
         * modifiable fashion. This object will be passed into the other template
         * methods (e.g. doBegin and doCommit), either directly or as part of a
         * DefaultTransactionStatus instance.
         * <p>The returned object should contain information about any existing
         * transaction, that is, a transaction that has already started before the
         * current <code>getTransaction</code> call on the transaction manager.
         * Consequently, a <code>doGetTransaction</code> implementation will usually
         * look for an existing transaction and store corresponding state in the
         * returned transaction object.
         * @return the current transaction object
         * @throws org.springframework.transaction.CannotCreateTransactionException
         * if transaction support is not available
         * @throws TransactionException in case of lookup or system errors
         * @see #doBegin
         * @see #doCommit
         * @see #doRollback
         * @see DefaultTransactionStatus#getTransaction
         */

        /// <summary>The do get transaction.</summary>
        /// <returns>The System.Object.</returns>
        protected abstract object DoGetTransaction();

        /**
         * Check if the given transaction object indicates an existing transaction
         * (that is, a transaction which has already started).
         * <p>The result will be evaluated according to the specified propagation
         * behavior for the new transaction. An existing transaction might get
         * suspended (in case of PROPAGATION_REQUIRES_NEW), or the new transaction
         * might participate in the existing one (in case of PROPAGATION_REQUIRED).
         * <p>The default implementation returns <code>false</code>, assuming that
         * participating in existing transactions is generally not supported.
         * Subclasses are of course encouraged to provide such support.
         * @param transaction transaction object returned by doGetTransaction
         * @return if there is an existing transaction
         * @throws TransactionException in case of system errors
         * @see #doGetTransaction
         */

        /// <summary>The is existing transaction.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <returns>The System.Boolean.</returns>
        protected virtual bool IsExistingTransaction(object transaction) { return false; }

        /**
         * Return whether to use a savepoint for a nested transaction.
         * <p>Default is <code>true</code>, which causes delegation to DefaultTransactionStatus
         * for creating and holding a savepoint. If the transaction object does not implement
         * the SavepointManager interface, a NestedTransactionNotSupportedException will be
         * thrown. Else, the SavepointManager will be asked to create a new savepoint to
         * demarcate the start of the nested transaction.
         * <p>Subclasses can override this to return <code>false</code>, causing a further
         * call to <code>doBegin</code> - within the context of an already existing transaction.
         * The <code>doBegin</code> implementation needs to handle this accordingly in such
         * a scenario. This is appropriate for JTA, for example.
         * @see DefaultTransactionStatus#createAndHoldSavepoint
         * @see DefaultTransactionStatus#rollbackToHeldSavepoint
         * @see DefaultTransactionStatus#releaseHeldSavepoint
         * @see #doBegin
         */

        /// <summary>The use savepoint for nested transaction.</summary>
        /// <returns>The System.Boolean.</returns>
        protected virtual bool UseSavepointForNestedTransaction() { return true; }

        /**
         * Begin a new transaction with semantics according to the given transaction
         * definition. Does not have to care about applying the propagation behavior,
         * as this has already been handled by this abstract manager.
         * <p>This method gets called when the transaction manager has decided to actually
         * start a new transaction. Either there wasn't any transaction before, or the
         * previous transaction has been suspended.
         * <p>A special scenario is a nested transaction without savepoint: If
         * <code>useSavepointForNestedTransaction()</code> returns "false", this method
         * will be called to start a nested transaction when necessary. In such a context,
         * there will be an active transaction: The implementation of this method has
         * to detect this and start an appropriate nested transaction.
         * @param transaction transaction object returned by <code>doGetTransaction</code>
         * @param definition TransactionDefinition instance, describing propagation
         * behavior, isolation level, read-only flag, timeout, and transaction name
         * @throws TransactionException in case of creation or system errors
         */

        /// <summary>The do begin.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="definition">The definition.</param>
        protected abstract void DoBegin(object transaction, ITransactionDefinition definition);

        /**
         * Suspend the resources of the current transaction.
         * Transaction synchronization will already have been suspended.
         * <p>The default implementation throws a TransactionSuspensionNotSupportedException,
         * assuming that transaction suspension is generally not supported.
         * @param transaction transaction object returned by <code>doGetTransaction</code>
         * @return an object that holds suspended resources
         * (will be kept unexamined for passing it into doResume)
         * @throws org.springframework.transaction.TransactionSuspensionNotSupportedException
         * if suspending is not supported by the transaction manager implementation
         * @throws TransactionException in case of system errors
         * @see #doResume
         */

        /// <summary>The do suspend.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <returns>The System.Object.</returns>
        /// <exception cref="TransactionSuspensionNotSupportedException"></exception>
        protected virtual object DoSuspend(object transaction) { throw new TransactionSuspensionNotSupportedException("Transaction manager [" + this.GetType().Name + "] does not support transaction suspension"); }

        /**
         * Resume the resources of the current transaction.
         * Transaction synchronization will be resumed afterwards.
         * <p>The default implementation throws a TransactionSuspensionNotSupportedException,
         * assuming that transaction suspension is generally not supported.
         * @param transaction transaction object returned by <code>doGetTransaction</code>
         * @param suspendedResources the object that holds suspended resources,
         * as returned by doSuspend
         * @throws org.springframework.transaction.TransactionSuspensionNotSupportedException
         * if resuming is not supported by the transaction manager implementation
         * @throws TransactionException in case of system errors
         * @see #doSuspend
         */

        /// <summary>The do resume.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="suspendedResources">The suspended resources.</param>
        /// <exception cref="TransactionSuspensionNotSupportedException"></exception>
        protected virtual void DoResume(object transaction, object suspendedResources) { throw new TransactionSuspensionNotSupportedException("Transaction manager [" + this.GetType().Name + "] does not support transaction suspension"); }

        /**
         * Return whether to call <code>doCommit</code> on a transaction that has been
         * marked as rollback-only in a global fashion.
         * <p>Does not apply if an application locally sets the transaction to rollback-only
         * via the TransactionStatus, but only to the transaction itself being marked as
         * rollback-only by the transaction coordinator.
         * <p>Default is "false": Local transaction strategies usually don't hold the rollback-only
         * marker in the transaction itself, therefore they can't handle rollback-only transactions
         * as part of transaction commit. Hence, AbstractPlatformTransactionManager will trigger
         * a rollback in that case, throwing an UnexpectedRollbackException afterwards.
         * <p>Override this to return "true" if the concrete transaction manager expects a
         * <code>doCommit</code> call even for a rollback-only transaction, allowing for
         * special handling there. This will, for example, be the case for JTA, where
         * <code>UserTransaction.commit</code> will check the read-only flag itself and
         * throw a corresponding RollbackException, which might include the specific reason
         * (such as a transaction timeout).
         * <p>If this method returns "true" but the <code>doCommit</code> implementation does not
         * throw an exception, this transaction manager will throw an UnexpectedRollbackException
         * itself. This should not be the typical case; it is mainly checked to cover misbehaving
         * JTA providers that silently roll back even when the rollback has not been requested
         * by the calling code.
         * @see #doCommit
         * @see DefaultTransactionStatus#isGlobalRollbackOnly()
         * @see DefaultTransactionStatus#isLocalRollbackOnly()
         * @see org.springframework.transaction.TransactionStatus#setRollbackOnly()
         * @see org.springframework.transaction.UnexpectedRollbackException
         * @see javax.transaction.UserTransaction#commit()
         * @see javax.transaction.RollbackException
         */

        /// <summary>The should commit on global rollback only.</summary>
        /// <returns>The System.Boolean.</returns>
        protected virtual bool ShouldCommitOnGlobalRollbackOnly() { return false; }

        /**
         * Make preparations for commit, to be performed before the
         * <code>beforeCommit</code> synchronization callbacks occur.
         * <p>Note that exceptions will get propagated to the commit caller
         * and cause a rollback of the transaction.
         * @param status the status representation of the transaction
         * @throws RuntimeException in case of errors; will be <b>propagated to the caller</b>
         * (note: do not throw TransactionException subclasses here!)
         */

        /// <summary>The prepare for commit.</summary>
        /// <param name="status">The status.</param>
        protected virtual void PrepareForCommit(DefaultTransactionStatus status) { }

        /**
         * Perform an actual commit of the given transaction.
         * <p>An implementation does not need to check the "new transaction" flag
         * or the rollback-only flag; this will already have been handled before.
         * Usually, a straight commit will be performed on the transaction object
         * contained in the passed-in status.
         * @param status the status representation of the transaction
         * @throws TransactionException in case of commit or system errors
         * @see DefaultTransactionStatus#getTransaction
         */

        /// <summary>The do commit.</summary>
        /// <param name="status">The status.</param>
        protected abstract void DoCommit(DefaultTransactionStatus status);

        /**
         * Perform an actual rollback of the given transaction.
         * <p>An implementation does not need to check the "new transaction" flag;
         * this will already have been handled before. Usually, a straight rollback
         * will be performed on the transaction object contained in the passed-in status.
         * @param status the status representation of the transaction
         * @throws TransactionException in case of system errors
         * @see DefaultTransactionStatus#getTransaction
         */

        /// <summary>The do rollback.</summary>
        /// <param name="status">The status.</param>
        protected abstract void DoRollback(DefaultTransactionStatus status);

        /**
         * Set the given transaction rollback-only. Only called on rollback
         * if the current transaction participates in an existing one.
         * <p>The default implementation throws an IllegalTransactionStateException,
         * assuming that participating in existing transactions is generally not
         * supported. Subclasses are of course encouraged to provide such support.
         * @param status the status representation of the transaction
         * @throws TransactionException in case of system errors
         */

        /// <summary>The do set rollback only.</summary>
        /// <param name="status">The status.</param>
        /// <exception cref="IllegalTransactionStateException"></exception>
        protected virtual void DoSetRollbackOnly(DefaultTransactionStatus status) { throw new IllegalTransactionStateException("Participating in existing transactions is not supported - when 'isExistingTransaction' returns true, appropriate 'doSetRollbackOnly' behavior must be provided"); }

        /**
         * Register the given list of transaction synchronizations with the existing transaction.
         * <p>Invoked when the control of the Spring transaction manager and thus all Spring
         * transaction synchronizations end, without the transaction being completed yet. This
         * is for example the case when participating in an existing JTA or EJB CMT transaction.
         * <p>The default implementation simply invokes the <code>afterCompletion</code> methods
         * immediately, passing in "STATUS_UNKNOWN". This is the best we can do if there's no
         * chance to determine the actual outcome of the outer transaction.
         * @param transaction transaction object returned by <code>doGetTransaction</code>
         * @param synchronizations List of TransactionSynchronization objects
         * @throws TransactionException in case of system errors
         * @see #invokeAfterCompletion(java.util.List, int)
         * @see TransactionSynchronization#afterCompletion(int)
         * @see TransactionSynchronization#STATUS_UNKNOWN
         */

        /// <summary>The register after completion with existing transaction.</summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="synchronizations">The synchronizations.</param>
        protected virtual void RegisterAfterCompletionWithExistingTransaction(object transaction, IList<ITransactionSynchronization> synchronizations)
        {
            Logger.Debug(m => m("Cannot register Spring after-completion synchronization with existing transaction - processing Spring after-completion callbacks immediately, with outcome status 'unknown'"));
            this.InvokeAfterCompletion(synchronizations, TransactionSynchronizationStatus.Unknown);
        }

        /**
         * Cleanup resources after transaction completion.
         * <p>Called after <code>doCommit</code> and <code>doRollback</code> execution,
         * on any outcome. The default implementation does nothing.
         * <p>Should not throw any exceptions but just issue warnings on errors.
         * @param transaction transaction object returned by <code>doGetTransaction</code>
         */

        /// <summary>The do cleanup after completion.</summary>
        /// <param name="transaction">The transaction.</param>
        protected virtual void DoCleanupAfterCompletion(object transaction) { }
    }

    /**
	 * Holder for suspended resources.
	 * Used internally by <code>suspend</code> and <code>resume</code>.
	 */

    /// <summary>The suspended resources holder.</summary>
    public class SuspendedResourcesHolder
    {
        internal readonly object suspendedResources;
        internal IList<ITransactionSynchronization> suspendedSynchronizations;
        internal string name;
        internal bool readOnly;
        internal IsolationLevel isolationLevel;
        internal bool wasActive;

        /// <summary>Initializes a new instance of the <see cref="SuspendedResourcesHolder"/> class.</summary>
        /// <param name="suspendedResources">The suspended resources.</param>
        public SuspendedResourcesHolder(object suspendedResources) { this.suspendedResources = suspendedResources; }

        /// <summary>Initializes a new instance of the <see cref="SuspendedResourcesHolder"/> class.</summary>
        /// <param name="suspendedResources">The suspended resources.</param>
        /// <param name="suspendedSynchronizations">The suspended synchronizations.</param>
        /// <param name="name">The name.</param>
        /// <param name="readOnly">The read only.</param>
        /// <param name="isolationLevel">The isolation level.</param>
        /// <param name="wasActive">The was active.</param>
        public SuspendedResourcesHolder(
            object suspendedResources, 
            IList<ITransactionSynchronization> suspendedSynchronizations, 
            string name, 
            bool readOnly, 
            IsolationLevel isolationLevel, 
            bool wasActive)
        {
            this.suspendedResources = suspendedResources;
            this.suspendedSynchronizations = suspendedSynchronizations;
            this.name = name;
            this.readOnly = readOnly;
            this.isolationLevel = isolationLevel;
            this.wasActive = wasActive;
        }
    }
}
