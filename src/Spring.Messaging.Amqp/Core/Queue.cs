using System.Collections;

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Simple container collecting information to describe a queue.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class Queue
    {
        private readonly string name;

        private volatile bool durable;

        private volatile bool exclusive;

        private volatile bool autoDelete;

        private volatile IDictionary arguments;

        public Queue(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }
        }

        public bool Durable
        {
            get { return durable; }
            set { durable = value; }
        }

        public bool Exclusive
        {
            get { return exclusive; }
            set { exclusive = value; }
        }

        public bool AutoDelete
        {
            get { return autoDelete; }
            set { autoDelete = value; }
        }

        public IDictionary Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, Durable: {1}, Exclusive: {2}, AutoDelete: {3}, Arguments: {4}", name, durable, exclusive, autoDelete, arguments);
        }
    }
}