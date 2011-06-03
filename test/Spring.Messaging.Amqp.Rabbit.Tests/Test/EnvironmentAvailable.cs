
using System;
using Common.Logging;
using NUnit.Framework;

namespace Spring.Messaging.Amqp.Rabbit.Test
{
    /// <summary>
    /// Determines if the environment is available.
    /// </summary>
    /// <remarks></remarks>
    public class EnvironmentAvailable
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private static ILog logger = LogManager.GetLogger(typeof(EnvironmentAvailable));

        /// <summary>
        /// The default environment key.
        /// </summary>
        private static readonly string DEFAULT_ENVIRONMENT_KEY = "ENVIRONMENT";

        /// <summary>
        /// The key.
        /// </summary>
        private readonly string key;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentAvailable"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <remarks></remarks>
        public EnvironmentAvailable(string key)
        {
            this.key = key;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentAvailable"/> class. 
        /// </summary>
        /// <remarks>
        /// </remarks>
        public EnvironmentAvailable() : this(DEFAULT_ENVIRONMENT_KEY)
        {
        }

        /// <summary>
        /// Applies this instance.
        /// </summary>
        /// <remarks></remarks>
        public void Apply()
        {
            logger.Info("Evironment: " + this.key + " active=" + this.IsActive());
            Assume.That(this.IsActive());
        }

        /// <summary>
        /// Determines whether this instance is active.
        /// </summary>
        /// <returns><c>true</c> if this instance is active; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        public bool IsActive()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(this.key));
        }
    }
}
