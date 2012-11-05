// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoqExtensions.cs" company="The original author or authors.">
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
using System.Collections;
using Common.Logging;
using Moq.Language.Flow;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Support
{
    /// <summary>
    /// Moq Extension methods.
    /// </summary>
    public static class MoqExtensions
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        /// <summary>Returnses the in order.</summary>
        /// <typeparam name="T">Type T</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="setup">The setup.</param>
        /// <param name="results">The results.</param>
        public static void ReturnsInOrder<T, TResult>(this ISetup<T, TResult> setup, params object[] results) where T : class
        {
            var queue = new Queue(results);
            setup.Returns(
                () =>
                {
                    try
                    {
                        var result = queue.Dequeue();
                        if (result is Exception)
                        {
                            throw result as Exception;
                        }

                        queue.Enqueue(result);
                        return (TResult)result;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(m => m("Error occurred dequeuing object."), ex);
                        throw;
                    }
                });
        }
    }
}
