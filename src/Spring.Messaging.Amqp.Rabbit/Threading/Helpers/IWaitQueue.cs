// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IWaitQueue.cs" company="The original author or authors.">
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
using System.Collections.Generic;
using System.Threading;
#endregion

namespace Spring.Threading.Helpers
{
    /// <summary> 
    /// Interface for internal queue classes for semaphores, etc.
    /// Relies on implementations to actually implement queue mechanics.
    /// NOTE: this interface is NOT present in java.util.concurrent.
    /// </summary>	
    /// <author>Dawid Kurzyniec</author>
    /// <author>Griffin Caprio (.NET)</author>
    /// <changes>
    /// <list>
    /// <item>Renamed Insert to Enqueue</item>
    /// <item>Renamed Extract to Dequeue</item>
    /// </list>
    /// </changes>
    internal interface IWaitQueue
    {
        // Was WaitQueue class in BACKPORT_3_1
        /// <summary>Gets the length.</summary>
        int Length { get; }

        /// <summary>Gets the waiting threads.</summary>
        ICollection<Thread> WaitingThreads { get; }

        /// <summary>Gets a value indicating whether has nodes.</summary>
        bool HasNodes { get; }

        /// <summary>The is waiting.</summary>
        /// <param name="thread">The thread.</param>
        /// <returns>The System.Boolean.</returns>
        bool IsWaiting(Thread thread);

        /// <summary>The enqueue.</summary>
        /// <param name="w">The w.</param>
        void Enqueue(WaitNode w);

        // assumed not to block
        /// <summary>The dequeue.</summary>
        /// <returns>The Spring.Threading.Helpers.WaitNode.</returns>
        WaitNode Dequeue();

        // should return null if empty
        // In backport 3.1 but not used.
        // void PutBack(WaitNode w);
    }
}
