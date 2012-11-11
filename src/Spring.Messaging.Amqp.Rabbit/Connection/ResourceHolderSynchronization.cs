// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourceHolderSynchronization.cs" company="The original author or authors.">
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
using Spring.Transaction.Support;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary><para>Central helper that manages resources and transaction synchronizations per thread.
    /// To be used by resource management code but not by typical application code.</para>
    /// <para>Supports one resource per key without overwriting, that is, a resource needs
    /// to be removed before a new one can be set for the same key.
    /// Supports a list of transaction synchronizations if synchronization is active.</para>
    /// <para>Resource management code should check for thread-bound resources, e.g. JDBC
    /// Connections or Hibernate Sessions, via 
    /// <code>getResource</code>
    /// . Such code is
    /// normally not supposed to bind resources to threads, as this is the responsibility
    /// of transaction managers. A further option is to lazily bind on first use if
    /// transaction synchronization is active, for performing transactions that span
    /// an arbitrary number of resources.</para>
    /// <para>Transaction synchronization must be activated and deactivated by a transaction
    /// manager via {@link #initSynchronization()} and {@link #clearSynchronization()}.
    /// This is automatically supported by {@link AbstractPlatformTransactionManager},
    /// and thus by all standard Spring transaction managers, such as
    /// {@link org.springframework.transaction.jta.JtaTransactionManager} and
    /// {@link org.springframework.jdbc.datasource.DataSourceTransactionManager}.</para>
    /// <para>Resource management code should only register synchronizations when this
    /// manager is active, which can be checked via {@link #isSynchronizationActive};
    /// it should perform immediate resource cleanup else. If transaction synchronization
    /// isn't active, there is either no current transaction, or the transaction manager
    /// doesn't support transaction synchronization.</para>
    /// <para>Synchronization is for example used to always return the same resources
    /// within a JTA transaction, e.g. a JDBC Connection or a Hibernate Session for
    /// any given DataSource or SessionFactory, respectively.</para>
    /// </summary>
    /// <typeparam name="H">Type H, that implements IResourceHolder.</typeparam>
    /// <typeparam name="K">Type K.</typeparam>
    public class ResourceHolderSynchronization<H, K> : ITransactionSynchronization where H : IResourceHolder
    {
        private readonly H resourceHolder;

        private readonly K resourceKey;

        private volatile bool holderActive = true;

        /// <summary>Initializes a new instance of the <see cref="ResourceHolderSynchronization{H,K}"/> class. Creates a new ResourceHolderSynchronization for the given holder.</summary>
        /// <param name="resourceHolder">The resource holder to manage.</param>
        /// <param name="resourceKey">The key to bind the ResourceHolder for.</param>
        public ResourceHolderSynchronization(H resourceHolder, K resourceKey)
        {
            this.resourceHolder = resourceHolder;
            this.resourceKey = resourceKey;
        }

        /// <summary>The suspend.</summary>
        public virtual void Suspend()
        {
            if (this.holderActive)
            {
                TransactionSynchronizationManager.UnbindResource(this.resourceKey);
            }
        }

        /// <summary>The resume.</summary>
        public virtual void Resume()
        {
            if (this.holderActive)
            {
                TransactionSynchronizationManager.BindResource(this.resourceKey, this.resourceHolder);
            }
        }

        /// <summary>The flush.</summary>
        public virtual void Flush()
        {
            // TODO: This won't be called because SPRNET TX support hasn't been updated to support it yet.
            this.FlushResource(this.resourceHolder);
        }

        /// <summary>The before commit.</summary>
        /// <param name="readOnly">The read only.</param>
        public virtual void BeforeCommit(bool readOnly) { }

        /// <summary>The before completion.</summary>
        public virtual void BeforeCompletion()
        {
            if (this.ShouldUnbindAtCompletion())
            {
                TransactionSynchronizationManager.UnbindResource(this.resourceKey);
                this.holderActive = false;
                if (this.ShouldReleaseBeforeCompletion())
                {
                    this.ReleaseResource(this.resourceHolder, this.resourceKey);
                }
            }
        }

        /// <summary>The after commit.</summary>
        public virtual void AfterCommit()
        {
            if (!this.ShouldReleaseBeforeCompletion())
            {
                this.ProcessResourceAfterCommit(this.resourceHolder);
            }
        }

        /// <summary>The after completion.</summary>
        /// <param name="status">The status.</param>
        public virtual void AfterCompletion(TransactionSynchronizationStatus status)
        {
            if (this.ShouldUnbindAtCompletion())
            {
                var releaseNecessary = false;
                if (this.holderActive)
                {
                    // The thread-bound resource holder might not be available anymore,
                    // since afterCompletion might get called from a different thread.
                    this.holderActive = false;

                    // TransactionSynchronizationManager.UnbindResourceIfPossible(this.resourceKey);
                    this.UnbindResourceIfPossible(this.resourceKey);
                    this.resourceHolder.Unbound();
                    releaseNecessary = true;
                }
                else
                {
                    releaseNecessary = this.ShouldReleaseAfterCompletion(this.resourceHolder);
                }

                if (releaseNecessary)
                {
                    this.ReleaseResource(this.resourceHolder, this.resourceKey);
                }
            }
            else
            {
                // Probably a pre-bound resource...
                this.CleanupResource(this.resourceHolder, this.resourceKey, status == TransactionSynchronizationStatus.Committed);
            }

            this.resourceHolder.Reset();
        }

        private void UnbindResourceIfPossible(object key)
        {
            // This is required because SPRNET TX support hasn't caught up with the Java equivalent.
            try
            {
                TransactionSynchronizationManager.UnbindResource(key);
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.StartsWith("No value for key ["))
                {
                    throw;
                }
            }
        }

        /// <summary>Return whether this holder should be unbound at completion (or should rather be left bound to the thread after the transaction).</summary>
        /// <returns>The default implementation returns True.</returns>
        protected virtual bool ShouldUnbindAtCompletion() { return true; }

        /// <summary>Return whether this holder's resource should be released before transaction completion (<code>true</code>) or 
        /// rather after transaction completion (<code>false</code>). Note that resources will only be released when they are
        /// unbound from the thread <see cref="ShouldUnbindAtCompletion"/>.</summary>
        /// <returns>The default implementation returns True</returns>
        protected virtual bool ShouldReleaseBeforeCompletion() { return true; }

        /// <summary>Return whether this holder's resource should be released after transaction completion (
        /// <code>true</code>
        /// ).</summary>
        /// <param name="resourceHolder">The default implementation returns !<see cref="ShouldReleaseBeforeCompletion"/>, releasing after completion is no attempt was made before completion.</param>
        /// <returns>The System.Boolean.</returns>
        protected virtual bool ShouldReleaseAfterCompletion(H resourceHolder) { return !this.ShouldReleaseBeforeCompletion(); }

        /// <summary>Flush callback for the given resource holder.</summary>
        /// <param name="resourceHolder">The resource holder to flush.</param>
        protected virtual void FlushResource(H resourceHolder) { }

        /// <summary>After-commit callback for the given resource holder. Only called when the resource hasn't been released yet <see cref="ShouldReleaseBeforeCompletion"/>.</summary>
        /// <param name="resourceHolder">The resource holder to process.</param>
        protected virtual void ProcessResourceAfterCommit(H resourceHolder) { }

        /// <summary>Release the given resource (after it has been unbound from the thread).</summary>
        /// <param name="resourceHolder">The resource holder to process.</param>
        /// <param name="resourceKey">The key that the ResourceHolder was bound for.</param>
        protected virtual void ReleaseResource(H resourceHolder, K resourceKey) { }

        /**
         * Perform a cleanup on the given resource (which is left bound to the thread).
         * @param resourceHolder the resource holder to process
         * @param resourceKey the key that the ResourceHolder was bound for
         * @param committed whether the transaction has committed (<code>true</code>)
         * or rolled back (<code>false</code>)
         */

        /// <summary>Perform a cleanup on the given resource (which is left bound to the thread).</summary>
        /// <param name="resourceHolder">The resource holder to process.</param>
        /// <param name="resourceKey">The key that the ResourceHolder was bound for.</param>
        /// <param name="committed">Whether the transaction has committed (
        /// <value>True</value>
        /// ) or rolled back (
        /// <value>false</value>
        /// ).</param>
        protected virtual void CleanupResource(H resourceHolder, K resourceKey, bool committed) { }
    }
}
