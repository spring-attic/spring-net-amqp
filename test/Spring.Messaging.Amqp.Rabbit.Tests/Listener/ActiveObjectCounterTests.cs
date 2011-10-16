using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    using Spring.Messaging.Amqp.Rabbit.Test;

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
        /// <remarks></remarks>
        [SetUp]
        public void SetUp()
        {
            this.counter = new ActiveObjectCounter<object>();
        }

        /// <summary>
        /// Tests the active count.
        /// </summary>
        /// <remarks></remarks>
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
        /// <remarks></remarks>
        [Test]
        public void TestWaitForLocks()
        {
            var object1 = new object();
            var object2 = new object();
            this.counter.Add(object1);
            this.counter.Add(object2);
            var future = Task.Factory.StartNew(() =>
                                                   {
                                                       counter.Release(object1);
                                                       counter.Release(object2);
                                                       counter.Release(object2);
                                                       return true;
                                                   });

            Assert.AreEqual(true, this.counter.Await(new TimeSpan(0, 0, 0, 0, 1000)));
            Assert.AreEqual(true, future.Result);
        }

        /// <summary>
        /// Tests the timeout wait for locks.
        /// </summary>
        /// <remarks></remarks>
        [Test]
        //[Ignore("Need to fix this test")]
        public void TestTimeoutWaitForLocks()
        {
            var object1 = new object();
            this.counter.Add(object1);
            Assert.AreEqual(false, this.counter.Await(new TimeSpan(0, 0, 0, 0, 200)));
        }
    }
}
