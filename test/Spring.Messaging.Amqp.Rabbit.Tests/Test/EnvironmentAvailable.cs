// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnvironmentAvailable.cs" company="The original author or authors.">
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
using Common.Logging;
using NUnit.Framework;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Test
{
    /// <summary>
    /// Determines if the environment is available.
    /// </summary>
    public class EnvironmentAvailable
    {
        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The default environment key.
        /// </summary>
        private static readonly string DEFAULT_ENVIRONMENT_KEY = "ENVIRONMENT";

        /// <summary>
        /// The key.
        /// </summary>
        private readonly string key;

        /// <summary>Initializes a new instance of the <see cref="EnvironmentAvailable"/> class.</summary>
        /// <param name="key">The key.</param>
        public EnvironmentAvailable(string key) { this.key = key; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentAvailable"/> class. 
        /// </summary>
        /// <remarks>
        /// </remarks>
        public EnvironmentAvailable() : this(DEFAULT_ENVIRONMENT_KEY) { }

        /// <summary>
        /// Applies this instance.
        /// </summary>
        public void Apply()
        {
            Logger.Info("Environment: " + this.key + " active=" + this.IsActive());
            Assume.That(this.IsActive());
        }

        /// <summary>
        /// Determines whether this instance is active.
        /// </summary>
        /// <returns><c>true</c> if this instance is active; otherwise, <c>false</c>.</returns>
        public bool IsActive() { return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(this.key)); }
    }
}
