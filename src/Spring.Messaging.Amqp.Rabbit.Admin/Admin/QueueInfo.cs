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
        /*{app.stock.request={transactions=0, acks_uncommitted=0, 
 * 					  consumers=0, pid=#Pid<rabbit@MARK6500.150.0>,
 *                    durable=false, messages=0, memory=2320, auto_delete=false, 
 *                    messages_ready=0, arguments=[], name=app.stock.request, 
 *                   messages_unacknowledged=0, messages_uncommitted=0}, 
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

        public long Transactions
        {
            get { return transactions; }
            set { transactions = value; }
        }

        public long AcksUncommitted
        {
            get { return acksUncommitted; }
            set { acksUncommitted = value; }
        }

        public long Consumers
        {
            get { return consumers; }
            set { consumers = value; }
        }

        public string Pid
        {
            get { return pid; }
            set { pid = value; }
        }

        public bool Durable
        {
            get { return durable; }
            set { durable = value; }
        }

        public long Messages
        {
            get { return messages; }
            set { messages = value; }
        }

        public long Memory
        {
            get { return memory; }
            set { memory = value; }
        }

        public bool AutoDelete
        {
            get { return autoDelete; }
            set { autoDelete = value; }
        }

        public long MessagesReady
        {
            get { return messagesReady; }
            set { messagesReady = value; }
        }

        public string[] Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public long MessagesUnacknowledged
        {
            get { return messagesUnacknowledged; }
            set { messagesUnacknowledged = value; }
        }

        public long MessageUncommitted
        {
            get { return messageUncommitted; }
            set { messageUncommitted = value; }
        }

        public override string ToString()
        {
            return string.Format("Transactions: {0}, AcksUncommitted: {1}, Consumers: {2}, Pid: {3}, Durable: {4}, Messages: {5}, Memory: {6}, AutoDelete: {7}, MessagesReady: {8}, Arguments: {9}, Name: {10}, MessagesUnacknowledged: {11}, MessageUncommitted: {12}", transactions, acksUncommitted, consumers, pid, durable, messages, memory, autoDelete, messagesReady, arguments, name, messagesUnacknowledged, messageUncommitted);
        }
    }

}