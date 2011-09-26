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
    /// A <see cref="bool"/> value that may be updated atomically. An <see cref="AtomicBoolean"/> 
    /// is used for instances of atomically updated flags, and cannot be used as a replacement for a <see cref="bool"/> value.
    /// <p/>
    /// Based on the on the back port of JCP JSR-166.
    /// </summary>
    /// <author>Doug Lea</author>
    /// <author>Griffin Caprio (.NET)</author>
    /// <author>Andreas Doehring (.NET)</author>
    /// <author>Kenneth Xu (Interlocked)</author>
    [Serializable]
    public class AtomicBoolean : IAtomic<bool>  //JDK_1_6
    {
        /// <summary>
        /// Holds a <see cref="Int32"/> representation of the flag value.
        /// </summary>
        private volatile int _booleanValue;

        /// <summary> 
        /// Creates a new <see cref="AtomicBoolean"/> with the given initial value.
        /// </summary>
        /// <param name="initialValue">
        /// The initial value
        /// </param>
        public AtomicBoolean(bool initialValue)
        {
            _booleanValue = initialValue ? 1 : 0;
        }

        /// <summary> 
        /// Creates a new <see cref="AtomicBoolean"/> with initial value of <c>false</c>.
        /// </summary>
        public AtomicBoolean()
            : this(false)
        {
        }

        /// <summary> 
        /// Gets and sets the current value.
        /// </summary>
        public bool Value
        {
            get { return _booleanValue != 0; }
            set { _booleanValue = value ? 1 : 0; }
        }

        /// <summary> 
        /// Atomically sets the value to <paramref name="newValue"/>
        /// if the current value == <paramref name="expectedValue"/>
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
        public bool CompareAndSet(bool expectedValue, bool newValue)
        {
            int e = expectedValue ? 1 : 0;
            int u = newValue ? 1 : 0;
            return Interlocked.CompareExchange(ref _booleanValue, u, e) == e;
        }

        /// <summary> 
        /// Atomically sets the value to <paramref name="newValue"/>
        /// if the current value == <paramref name="expectedValue"/>
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
        public virtual bool WeakCompareAndSet(bool expectedValue, bool newValue)
        {
            return CompareAndSet(expectedValue, newValue);
        }

        /// <summary> 
        /// Eventually sets to the given value.
        /// </summary>
        /// <param name="newValue">
        /// the new value
        /// </param>
        public void LazySet(bool newValue)
        {
            _booleanValue = newValue ? 1 : 0;
        }

        /// <summary> 
        /// Atomically sets the current value to <parmref name="newValue"/> and returns the previous value.
        /// </summary>
        /// <param name="newValue">
        /// The new value for the instance.
        /// </param>
        /// <returns> 
        /// the previous value of the instance.
        /// </returns>
        public bool Exchange(bool newValue)
        {
            int u = newValue ? 1 : 0;
            return Interlocked.Exchange(ref _booleanValue, u) == 1;
        }

        /// <summary> 
        /// Returns the String representation of the current value.
        /// </summary>
        /// <returns> 
        /// The String representation of the current value.
        /// </returns>
        public override String ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// Implicit convert <see cref="AtomicBoolean"/> to bool.
        /// </summary>
        /// <param name="atomicBoolean">
        /// Instance of <see cref="AtomicBoolean"/>
        /// </param>
        /// <returns>
        /// The boolean value of <paramref name="atomicBoolean"/>.
        /// </returns>
        public static implicit operator bool(AtomicBoolean atomicBoolean)
        {
            return atomicBoolean.Value;
        }

    }
}

#pragma warning restore 420