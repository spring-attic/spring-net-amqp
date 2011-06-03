using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq.Language.Flow;

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// Moq Extension methods.
    /// </summary>
    /// <remarks></remarks>
    public static class MoqExtensions
    {
        /// <summary>
        /// Returnses the in order.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="setup">The setup.</param>
        /// <param name="results">The results.</param>
        /// <remarks></remarks>
        public static void ReturnsInOrder<T, TResult>(this ISetup<T, TResult> setup, params object[] results) where T : class
        {
            var queue = new Queue(results);
            setup.Returns(() =>
                              {
                                  var result = queue.Dequeue();
                                  if (result is Exception)
                                  {
                                      throw result as Exception;
                                  }
                                  return (TResult)result;
                              });
        }
    }
}
