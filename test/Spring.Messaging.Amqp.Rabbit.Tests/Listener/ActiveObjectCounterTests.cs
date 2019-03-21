// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActiveObjectCounterTests.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Spring.Messaging.Amqp.Rabbit.Listener;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Tests.Listener
{
    /// <summary>
    /// Active object counter tests.
    /// </summary>
    /// @author Dave Syer
    [TestFixture]
    [Category(TestCategory.Unit)]
    public class ActiveObjectCounterTests
    {
        /// <summary>
        /// The counter.
        /// </summary>
        private ActiveObjectCounter<object> counter;

        /// <summary>
        /// Sets up.
        /// </summary>
        [SetUp]
        public void SetUp() { this.counter = new ActiveObjectCounter<object>(); }

        /// <summary>
        /// Tests the active count.
        /// </summary>
        [Test]
        public void TestActiveCount()
        {
            var object1 = new object();
            var object2 = new object();
            this.counter.Add(object1);
            this.counter.Add(object2);
            Assert.AreEqual(2, this.counter.GetCount());
            this.counter.Release(object2);
            Assert.AreEqual(1, this.counter.GetCount());
            this.counter.Release(object1);
            this.counter.Release(object1);
            Assert.AreEqual(0, this.counter.GetCount());
        }

        /// <summary>
        /// Tests the wait for locks.
        /// </summary>
        [Test]
        public void TestWaitForLocks()
        {
            var object1 = new object();
            var object2 = new object();
            this.counter.Add(object1);
            this.counter.Add(object2);
            var future = Task.Factory.StartNew(
                () =>
                {
                    this.counter.Release(object1);
                    this.counter.Release(object2);
                    this.counter.Release(object2);
                    return true;
                });

            Assert.AreEqual(true, this.counter.Await(new TimeSpan(0, 0, 0, 0, 1000)));
            Assert.AreEqual(true, future.Result);
        }

        /// <summary>
        /// Tests the timeout wait for locks.
        /// </summary>
        [Test]
        public void TestTimeoutWaitForLocks()
        {
            var object1 = new object();
            this.counter.Add(object1);
            Assert.AreEqual(false, this.counter.Await(new TimeSpan(0, 0, 0, 0, 200)));
        }
    }
}
