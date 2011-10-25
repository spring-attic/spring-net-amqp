
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Spring.Messaging.Amqp.Rabbit.Tests.Config
{
    /// <summary>
    /// Test Advice
    /// </summary>
    public class TestAdvice
    {
        /// <summary>
        /// Befores the specified method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="args">The args.</param>
        /// <param name="target">The target.</param>
        public void Before(MethodInfo method, object[] args, object target)
        {
		}
    }
}
