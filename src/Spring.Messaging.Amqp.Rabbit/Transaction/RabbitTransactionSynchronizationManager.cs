// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitTransactionSynchronizationManager.cs" company="The original author or authors.">
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
using System.Threading;
using Common.Logging;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Transaction.Support;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Transaction
{
    /// <summary>
    /// Rabbit Transaction Synchronization Manager
    /// </summary>
    public class RabbitTransactionSynchronizationManager
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private static readonly ThreadLocal<Dictionary<object, object>> resources = new ThreadLocal<Dictionary<object, object>>(); // NamedThreadLocal<Map<object, object>>("Transactional resources");

        private static readonly ThreadLocal<LinkedList<ITransactionSynchronization>> synchronizations = new ThreadLocal<LinkedList<ITransactionSynchronization>>();

        // new NamedThreadLocal<List<TransactionSynchronization>>("Transaction synchronizations");
        private static readonly ThreadLocal<string> currentTransactionName = new ThreadLocal<string>(); // new NamedThreadLocal<string>("Current transaction name");

        private static readonly ThreadLocal<bool> currentTransactionReadOnly = new ThreadLocal<bool>(); // new NamedThreadLocal<bool>("Current transaction read-only status");

        private static readonly ThreadLocal<IsolationLevel> currentTransactionIsolationLevel = new ThreadLocal<IsolationLevel>(); // new NamedThreadLocal<Integer>("Current transaction isolation level");

        private static readonly ThreadLocal<bool> actualTransactionActive = new ThreadLocal<bool>(); // new NamedThreadLocal<bool>("Actual transaction active");

        // -------------------------------------------------------------------------
        // Management of transaction-associated resource handles
        // -------------------------------------------------------------------------

        /**
	 * Return all resources that are bound to the current thread.
	 * <p>Mainly for debugging purposes. Resource managers should always invoke
	 * <code>hasResource</code> for a specific resource key that they are interested in.
	 * @return a Map with resource keys (usually the resource factory) and resource
	 * values (usually the active resource object), or an empty Map if there are
	 * currently no resources bound
	 * @see #hasResource
	 */

        /// <summary>The resource dictionary.</summary>
        /// <returns>The System.Collections.Generic.IDictionary`2[TKey -&gt; System.Object, TValue -&gt; System.Object].</returns>
        public static IDictionary<object, object> ResourceDictionary()
        {
            var map = resources.Value;
            return map != null ? map : new Dictionary<object, object>();
        }

        /**
	 * Check if there is a resource for the given key bound to the current thread.
	 * @param key the key to check (usually the resource factory)
	 * @return if there is a value bound to the current thread
	 * @see ResourceTransactionManager#getResourceFactory() 
	 */

        /// <summary>The has resource.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The System.Boolean.</returns>
        public static bool HasResource(object key)
        {
            var actualKey = TransactionSynchronizationUtils.UnwrapResourceIfNecessary(key);
            var value = DoGetResource(actualKey);
            return value != null;
        }

        /**
	 * Retrieve a resource for the given key that is bound to the current thread.
	 * @param key the key to check (usually the resource factory)
	 * @return a value bound to the current thread (usually the active
	 * resource object), or <code>null</code> if none
	 * @see ResourceTransactionManager#getResourceFactory()
	 */

        /// <summary>The get resource.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The System.Object.</returns>
        public static object GetResource(object key)
        {
            var actualKey = TransactionSynchronizationUtils.UnwrapResourceIfNecessary(key);
            var value = DoGetResource(actualKey);
            if (value != null && Logger.IsTraceEnabled)
            {
                Logger.Trace(
                    "Retrieved value [" + value + "] for key [" + actualKey + "] bound to thread [" +
                    Thread.CurrentThread.Name + "]");
            }

            return value;
        }

        /**
	 * Actually check the value of the resource that is bound for the given key.
	 */
        private static object DoGetResource(object actualKey)
        {
            var map = resources.Value;
            if (map == null)
            {
                return null;
            }

            var value = map.Get(actualKey);

            // Transparently remove ResourceHolder that was marked as void...
            if (value is IResourceHolder && ((IResourceHolder)value).IsVoid())
            {
                map.Remove(actualKey);

                // Remove entire ThreadLocal if empty...
                if (map.Count < 1)
                {
                    resources.Value = default(Dictionary<object, object>);
                }

                value = null;
            }

            return value;
        }

        /**
	 * Bind the given resource for the given key to the current thread.
	 * @param key the key to bind the value to (usually the resource factory)
	 * @param value the value to bind (usually the active resource object)
	 * @throws IllegalStateException if there is already a value bound to the thread
	 * @see ResourceTransactionManager#getResourceFactory()
	 */

        /// <summary>The bind resource.</summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void BindResource(object key, object value)
        {
            var actualKey = TransactionSynchronizationUtils.UnwrapResourceIfNecessary(key);
            AssertUtils.ArgumentNotNull(value, "Value must not be null");
            var map = resources.Value;

            // set ThreadLocal Map if none found
            if (map == null)
            {
                map = new Dictionary<object, object>();
                resources.Value = map;
            }

            var oldValue = map.Get(actualKey);
            map.Add(actualKey, value);

            // Transparently suppress a ResourceHolder that was marked as void...
            if (oldValue is IResourceHolder && ((IResourceHolder)oldValue).IsVoid())
            {
                oldValue = null;
            }

            if (oldValue != null)
            {
                throw new InvalidOperationException("Already value [" + oldValue + "] for key [" + actualKey + "] bound to thread [" + Thread.CurrentThread.Name + "]");
            }

            if (Logger.IsTraceEnabled)
            {
                Logger.Trace(
                    "Bound value [" + value + "] for key [" + actualKey + "] to thread [" +
                    Thread.CurrentThread.Name + "]");
            }
        }

        /**
	 * Unbind a resource for the given key from the current thread.
	 * @param key the key to unbind (usually the resource factory)
	 * @return the previously bound value (usually the active resource object)
	 * @throws IllegalStateException if there is no value bound to the thread
	 * @see ResourceTransactionManager#getResourceFactory()
	 */

        /// <summary>The unbind resource.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The System.Object.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static object UnbindResource(object key)
        {
            var actualKey = TransactionSynchronizationUtils.UnwrapResourceIfNecessary(key);
            var value = DoUnbindResource(actualKey);
            if (value == null)
            {
                throw new InvalidOperationException("No value for key [" + actualKey + "] bound to thread [" + Thread.CurrentThread.Name + "]");
            }

            return value;
        }

        /**
	 * Unbind a resource for the given key from the current thread.
	 * @param key the key to unbind (usually the resource factory)
	 * @return the previously bound value, or <code>null</code> if none bound
	 */

        /// <summary>The unbind resource if possible.</summary>
        /// <param name="key">The key.</param>
        /// <returns>The System.Object.</returns>
        public static object UnbindResourceIfPossible(object key)
        {
            var actualKey = TransactionSynchronizationUtils.UnwrapResourceIfNecessary(key);
            return DoUnbindResource(actualKey);
        }

        /**
	 * Actually remove the value of the resource that is bound for the given key.
	 */
        private static object DoUnbindResource(object actualKey)
        {
            var map = resources.Value;
            if (map == null)
            {
                return null;
            }

            object value = map.Remove(actualKey);

            // Remove entire ThreadLocal if empty...
            if (map.Count < 1)
            {
                resources.Value = default(Dictionary<object, object>);
            }

            // Transparently suppress a ResourceHolder that was marked as void...
            if (value is IResourceHolder && ((IResourceHolder)value).IsVoid())
            {
                value = null;
            }

            if (value != null && Logger.IsTraceEnabled)
            {
                Logger.Trace("Removed value [" + value + "] for key [" + actualKey + "] from thread [" + Thread.CurrentThread.Name + "]");
            }

            return value;
        }

        // -------------------------------------------------------------------------
        // Management of transaction synchronizations
        // -------------------------------------------------------------------------

        /**
	 * Return if transaction synchronization is active for the current thread.
	 * Can be called before register to avoid unnecessary instance creation.
	 * @see #registerSynchronization
	 */

        /// <summary>Gets a value indicating whether synchronization active.</summary>
        public static bool SynchronizationActive { get { return synchronizations.Value != null; } }

        /// <summary>The is synchronization active.</summary>
        /// <returns>The System.Boolean.</returns>
        public static bool IsSynchronizationActive() { return synchronizations.Value != null; }

        /**
	 * Activate transaction synchronization for the current thread.
	 * Called by a transaction manager on transaction begin.
	 * @throws IllegalStateException if synchronization is already active
	 */

        /// <summary>The init synchronization.</summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static void InitSynchronization()
        {
            if (IsSynchronizationActive())
            {
                throw new InvalidOperationException("Cannot activate transaction synchronization - already active");
            }

            Logger.Trace("Initializing transaction synchronization");
            synchronizations.Value = new LinkedList<ITransactionSynchronization>();
        }

        /**
	 * Register a new transaction synchronization for the current thread.
	 * Typically called by resource management code.
	 * <p>Note that synchronizations can implement the
	 * {@link org.springframework.core.Ordered} interface.
	 * They will be executed in an order according to their order value (if any).
	 * @param synchronization the synchronization object to register
	 * @throws IllegalStateException if transaction synchronization is not active
	 * @see org.springframework.core.Ordered
	 */

        /// <summary>The register synchronization.</summary>
        /// <param name="synchronization">The synchronization.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void RegisterSynchronization(ITransactionSynchronization synchronization)
        {
            AssertUtils.ArgumentNotNull(synchronization, "TransactionSynchronization must not be null");
            if (!IsSynchronizationActive())
            {
                throw new InvalidOperationException("Transaction synchronization is not active");
            }

            synchronizations.Value.AddOrUpdate(synchronization);
        }

        /**
	 * Return an unmodifiable snapshot list of all registered synchronizations
	 * for the current thread.
	 * @return unmodifiable List of TransactionSynchronization instances
	 * @throws IllegalStateException if synchronization is not active
	 * @see TransactionSynchronization
	 */

        /// <summary>Gets the synchronizations.</summary>
        public static LinkedList<ITransactionSynchronization> Synchronizations { get { return GetSynchronizations(); } }

        /// <summary>The get synchronizations.</summary>
        /// <returns>The System.Collections.Generic.LinkedList`1[T -&gt; Spring.Transaction.Support.ITransactionSynchronization].</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static LinkedList<ITransactionSynchronization> GetSynchronizations()
        {
            var synchs = synchronizations.Value;
            if (synchs == null)
            {
                throw new InvalidOperationException("Transaction synchronization is not active");
            }

            // Return unmodifiable snapshot, to avoid ConcurrentModificationExceptions
            // while iterating and invoking synchronization callbacks that in turn
            // might register further synchronizations.
            if (synchs.Count < 1)
            {
                return synchs;
            }
            else
            {
                // Sort lazily here, not in registerSynchronization.
                // synchs.ToList().Sort(OrderComparator);
                // return Collections.unmodifiableList(new ArrayList<TransactionSynchronization>(synchs));
                return synchs;
            }
        }

        /**
	 * Deactivate transaction synchronization for the current thread.
	 * Called by the transaction manager on transaction cleanup.
	 * @throws IllegalStateException if synchronization is not active
	 */

        /// <summary>The clear synchronization.</summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static void ClearSynchronization()
        {
            if (!IsSynchronizationActive())
            {
                throw new InvalidOperationException("Cannot deactivate transaction synchronization - not active");
            }

            Logger.Trace("Clearing transaction synchronization");
            synchronizations.Value = default(LinkedList<ITransactionSynchronization>);
        }

        // -------------------------------------------------------------------------
        // Exposure of transaction characteristics
        // -------------------------------------------------------------------------

        /// <summary>Gets or sets the current transaction name.</summary>
        public static string CurrentTransactionName { get { return currentTransactionName.Value; } set { currentTransactionName.Value = value; } }

        /// <summary>Gets or sets a value indicating whether current transaction read only.</summary>
        public static bool CurrentTransactionReadOnly { get { return currentTransactionReadOnly.Value; } set { currentTransactionReadOnly.Value = value; } }

        /// <summary>Gets or sets the current transaction isolation level.</summary>
        public static IsolationLevel CurrentTransactionIsolationLevel { get { return currentTransactionIsolationLevel.Value; } set { currentTransactionIsolationLevel.Value = value; } }

        /// <summary>Gets or sets a value indicating whether actual transaction active.</summary>
        public static bool ActualTransactionActive { get { return actualTransactionActive.Value; } set { actualTransactionActive.Value = value; } }

        /**
	 * Clear the entire transaction synchronization state for the current thread:
	 * registered synchronizations as well as the various transaction characteristics.
	 * @see #clearSynchronization()
	 * @see #setCurrentTransactionName
	 * @see #setCurrentTransactionReadOnly
	 * @see #setCurrentTransactionIsolationLevel
	 * @see #setActualTransactionActive
	 */

        /// <summary>The clear.</summary>
        public static void Clear()
        {
            ClearSynchronization();
            CurrentTransactionName = default(string);
            CurrentTransactionReadOnly = false;
            CurrentTransactionIsolationLevel = default(IsolationLevel);
            ActualTransactionActive = false;
        }
    }
}
