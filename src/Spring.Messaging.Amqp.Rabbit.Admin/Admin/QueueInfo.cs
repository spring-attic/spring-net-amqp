
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

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>
    /// This class represents a Queue that is configured on the RabbitMQ broker
    /// </summary>
    /// <author>Mark Pollack</author>
    public class QueueInfo
    {
        /*
         * {app.stock.request={transactions=0, acks_uncommitted=0, 
         * consumers=0, pid=#Pid<rabbit@MARK6500.150.0>,
         * durable=false, messages=0, memory=2320, auto_delete=false, 
         * messages_ready=0, arguments=[], name=app.stock.request, 
         * messages_unacknowledged=0, messages_uncommitted=0}, 
         */

        private long transactions;

        private long acksUncommitted;

        private long consumers;

        private string pid;

        private bool durable;

        private long messages;

        private long memory;

        private bool autoDelete;

        private long messagesReady;

        private string[] arguments;

        private string name;

        private long messagesUnacknowledged;

        private long messageUncommitted;

        /// <summary>
        /// Gets or sets the transactions.
        /// </summary>
        /// <value>The transactions.</value>
        /// <remarks></remarks>
        public long Transactions
        {
            get { return this.transactions; }
            set { this.transactions = value; }
        }

        /// <summary>
        /// Gets or sets the acks uncommitted.
        /// </summary>
        /// <value>The acks uncommitted.</value>
        /// <remarks></remarks>
        public long AcksUncommitted
        {
            get { return this.acksUncommitted; }
            set { this.acksUncommitted = value; }
        }

        /// <summary>
        /// Gets or sets the consumers.
        /// </summary>
        /// <value>The consumers.</value>
        /// <remarks></remarks>
        public long Consumers
        {
            get { return this.consumers; }
            set { this.consumers = value; }
        }

        /// <summary>
        /// Gets or sets the pid.
        /// </summary>
        /// <value>The pid.</value>
        /// <remarks></remarks>
        public string Pid
        {
            get { return this.pid; }
            set { this.pid = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="QueueInfo"/> is durable.
        /// </summary>
        /// <value><c>true</c> if durable; otherwise, <c>false</c>.</value>
        /// <remarks></remarks>
        public bool Durable
        {
            get { return this.durable; }
            set { this.durable = value; }
        }

        /// <summary>
        /// Gets or sets the messages.
        /// </summary>
        /// <value>The messages.</value>
        /// <remarks></remarks>
        public long Messages
        {
            get { return this.messages; }
            set { this.messages = value; }
        }

        /// <summary>
        /// Gets or sets the memory.
        /// </summary>
        /// <value>The memory.</value>
        /// <remarks></remarks>
        public long Memory
        {
            get { return this.memory; }
            set { this.memory = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [auto delete].
        /// </summary>
        /// <value><c>true</c> if [auto delete]; otherwise, <c>false</c>.</value>
        /// <remarks></remarks>
        public bool AutoDelete
        {
            get { return this.autoDelete; }
            set { this.autoDelete = value; }
        }

        /// <summary>
        /// Gets or sets the messages ready.
        /// </summary>
        /// <value>The messages ready.</value>
        /// <remarks></remarks>
        public long MessagesReady
        {
            get { return this.messagesReady; }
            set { this.messagesReady = value; }
        }

        /// <summary>
        /// Gets or sets the arguments.
        /// </summary>
        /// <value>The arguments.</value>
        /// <remarks></remarks>
        public string[] Arguments
        {
            get { return this.arguments; }
            set { this.arguments = value; }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        /// <remarks></remarks>
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        /// <summary>
        /// Gets or sets the messages unacknowledged.
        /// </summary>
        /// <value>The messages unacknowledged.</value>
        /// <remarks></remarks>
        public long MessagesUnacknowledged
        {
            get { return this.messagesUnacknowledged; }
            set { this.messagesUnacknowledged = value; }
        }

        /// <summary>
        /// Gets or sets the message uncommitted.
        /// </summary>
        /// <value>The message uncommitted.</value>
        /// <remarks></remarks>
        public long MessageUncommitted
        {
            get { return this.messageUncommitted; }
            set { this.messageUncommitted = value; }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        /// <remarks></remarks>
        public override string ToString()
        {
            return string.Format("Transactions: {0}, AcksUncommitted: {1}, Consumers: {2}, Pid: {3}, Durable: {4}, Messages: {5}, Memory: {6}, AutoDelete: {7}, MessagesReady: {8}, Arguments: {9}, Name: {10}, MessagesUnacknowledged: {11}, MessageUncommitted: {12}", this.transactions, this.acksUncommitted, this.consumers, this.pid, this.durable, this.messages, this.memory, this.autoDelete, this.messagesReady, this.arguments, this.name, this.messagesUnacknowledged, this.messageUncommitted);
        }
    }
}