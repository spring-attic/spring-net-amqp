#region License

/*
 * Copyright 2002-2008 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.Threading;

#pragma warning disable 420
namespace Spring.Threading.AtomicTypes
{
    /// <summary>
    /// A object reference that may be updated atomically with the equality
    /// defined as reference equals.
    /// <p/>
    /// Based on the on the back port of JCP JSR-166.
    /// </summary>
    /// <author>Doug Lea</author>
    /// <author>Griffin Caprio (.NET)</author>
    /// <author>Andreas Doehring (.NET)</author>
    /// <author>Kenneth Xu (Interlocked)</author>
    [Serializable]
    public class AtomicReference<T> : IAtomic<T> where T : class //JDK_1_6
    {
        /// <summary>
        /// Holds the object reference.
        /// </summary>
        private volatile T _reference;

        /// <summary> 
        /// Creates a new <see cref="AtomicReference{T}"/> with the given initial value.
        /// </summary>
        /// <param name="initialValue">
        /// The initial value
        /// </param>
        public AtomicReference(T initialValue)
        {
            _reference = initialValue;
        }

        /// <summary> 
        /// Creates a new <see cref="Atomic{T}"/> with <c>default<typeparamref name="T"/></c> 
        /// as initial value.
        /// </summary>
        public AtomicReference()
            : this(default(T))
        {
        }

        /// <summary> 
        /// Gets and sets the current value.
        /// </summary>
        public T Value
        {
            get { return _reference; }
            set { _reference = value; }
        }
        /// <summary> 
        /// Eventually sets to the given value.
        /// </summary>
        /// <param name="newValue">
        /// the new value
        /// </param>
        public void LazySet(T newValue)
        {
            _reference = newValue;
        }
        /// <summary> 
        /// Atomically sets the value to the <paramref name="newValue"/>
        /// if the current value equals the <paramref name="expectedValue"/>.
        /// </summary>
        /// <param name="expectedValue">
        /// The expected value
        /// </param>
        /// <param name="newValue">
        /// The new value to use of the current value equals the expected value.
        /// </param>
        /// <returns> 
        /// <c>true</c> if the current value equaled the expected value, <c>false</c> otherwise.
        /// </returns>
        public bool CompareAndSet(T expectedValue, T newValue)
        {
            return ReferenceEquals(expectedValue,
                Interlocked.CompareExchange(ref _reference, newValue, expectedValue));
        }

        /// <summary> 
        /// Atomically sets the value to the <paramref name="newValue"/>
        /// if the current value equals the <paramref name="expectedValue"/>.
        /// May fail spuriously.
        /// </summary>
        /// <param name="expectedValue">
        /// The expected value
        /// </param>
        /// <param name="newValue">
        /// The new value to use of the current value equals the expected value.
        /// </param>
        /// <returns>
        /// <c>true</c> if the current value equaled the expected value, <c>false</c> otherwise.
        /// </returns>
        public virtual bool WeakCompareAndSet(T expectedValue, T newValue)
        {
            return ReferenceEquals(expectedValue,
                Interlocked.CompareExchange(ref _reference, newValue, expectedValue));
        }

        /// <summary> 
        /// Atomically sets to the given value and returns the previous value.
        /// </summary>
        /// <param name="newValue">
        /// The new value for the instance.
        /// </param>
        /// <returns> 
        /// the previous value of the instance.
        /// </returns>
        public T Exchange(T newValue)
        {
            return Interlocked.Exchange(ref _reference, newValue);
        }

        /// <summary> 
        /// Returns the String representation of the current value.
        /// </summary>
        /// <returns> 
        /// The String representation of the current value.
        /// </returns>
        public override string ToString()
        {
            return _reference.ToString();
        }


        /// <summary>
        /// Implicit converts <see cref="AtomicReference{T}"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="atomicReference">
        /// Instance of <see cref="AtomicReference{T}"/>.
        /// </param>
        /// <returns>
        /// The converted int value of <paramref name="atomicReference"/>.
        /// </returns>
        public static implicit operator T(AtomicReference<T> atomicReference)
        {
            return atomicReference.Value;
        }
    }
}
#pragma warning restore 420