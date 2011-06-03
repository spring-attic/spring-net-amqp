
#region License

/*
 * Copyright 2002-2010 the original author or authors.
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

using System.Collections;

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Simple container collecting information to describe a queue.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class Queue
    {
        /// <summary>
        /// The name of the queue.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// The durable flag.
        /// </summary>
        private readonly bool durable;

        /// <summary>
        /// The exclusive flag.
        /// </summary>
        private readonly bool exclusive;

        /// <summary>
        /// The auto delete flag.
        /// </summary>
        private readonly bool autoDelete;

        /// <summary>
        /// The arguments.
        /// </summary>
        private readonly IDictionary arguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class. The queue is non-durable, non-exclusive and non auto-delete.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public Queue(string name) : this(name, false, false, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class, given a name and durability flag. The queue is non-exclusive and non auto-delete.
        /// </summary>
        /// <param name="name">
        /// The name of the queue.
        /// </param>
        /// <param name="durable">
        /// The durable. True if we are declaring a durable queue (the queue will survive a server restart).
        /// </param>
        public Queue(string name, bool durable) : this(name, durable, false, false, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class, given a name, durability, exclusive and auto-delete flags.
        /// </summary>
        /// <param name="name">
        /// The name of the queue.
        /// </param>
        /// <param name="durable">
        /// The durable. True if we are declaring a durable queue (the queue will survive a server restart).
        /// </param>
        /// <param name="exclusive">
        /// The exclusive. True if we are declaring an exclusive queue (the queue will only be used by the declarer's connection).
        /// </param>
        /// <param name="autoDelete">
        /// The auto delete. True if the server should delete the queue when it is no longer in use.
        /// </param>
        public Queue(string name, bool durable, bool exclusive, bool autoDelete) : this(name, durable, exclusive, autoDelete, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class, given a name, durability flag, and auto-delete flag, and arguments.
        /// </summary>
        /// <param name="name">
        /// The name of the queue.
        /// </param>
        /// <param name="durable">
        /// The durable. True if we are declaring a durable queue (the queue will survive a server restart).
        /// </param>
        /// <param name="exclusive">
        /// The exclusive. True if we are declaring an exclusive queue (the queue will only be used by the declarer's connection).
        /// </param>
        /// <param name="autoDelete">
        /// The auto delete. True if the server should delete the queue when it is no longer in use.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to declare the queue.
        /// </param>
        public Queue(string name, bool durable, bool exclusive, bool autoDelete, IDictionary arguments)
        {
            this.name = name;
            this.durable = durable;
            this.exclusive = exclusive;
            this.autoDelete = autoDelete;
            this.arguments = arguments;
        }

        /// <summary>
        /// Gets Name.
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets a value indicating whether Durable. A durable queue will survive a server restart.
        /// </summary>
        public bool Durable
        {
            get { return this.durable; }
        }

        /// <summary>
        /// Gets a value indicating whether Exclusive. True if the server should only send messages to the declarer's connection.
        /// </summary>
        public bool Exclusive
        {
            get { return this.exclusive; }
        }

        /// <summary>
        /// Gets a value indicating whether AutoDelete. True if the server should delete the queue when it is no longer in use 
        /// (the last consumer is cancelled). A queue that never has any consumers will not be deleted automatically.
        /// </summary>
        public bool AutoDelete
        {
            get { return this.autoDelete; }
        }

        /// <summary>
        /// Gets Arguments.
        /// </summary>
        public IDictionary Arguments
        {
            get { return this.arguments; }
        }

        /// <summary>
        /// A String representation of the Queue.
        /// </summary>
        /// <returns>
        /// String description of the queue.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Name: {0}, Durable: {1}, Exclusive: {2}, AutoDelete: {3}, Arguments: {4}", this.name, this.durable, this.exclusive, this.autoDelete, this.arguments);
        }
    }
}