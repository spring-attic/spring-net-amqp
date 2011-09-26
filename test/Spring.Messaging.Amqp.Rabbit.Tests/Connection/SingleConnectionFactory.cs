
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A single connection factory.
    /// </summary>
    public class SingleConnectionFactory : AbstractConnectionFactory
    {
        /// <summary>
        /// The connection proxy.
        /// </summary>
        private SharedConnectionProxy connection;

        /// <summary>
        /// A synchronization monitor.
        /// </summary>
        private readonly object connectionMonitor = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.
        /// </summary>
        public SingleConnectionFactory() : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        public SingleConnectionFactory(int port) : this(string.Empty, port)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        public SingleConnectionFactory(string hostname) : this(hostname, RabbitMQ.Client.Protocols.DefaultProtocol.DefaultPort)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        public SingleConnectionFactory(string hostname, int port) : base(new ConnectionFactory())
        {
            if (!string.IsNullOrWhiteSpace(hostname))
            {
                hostname = this.GetDefaultHostName();
            }
            this.Host = hostname;
            this.Port = port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnectionFactory"/> class.
        /// </summary>
        /// <param name="rabbitConnectionFactory">The rabbit connection factory.</param>
        public SingleConnectionFactory(ConnectionFactory rabbitConnectionFactory) : base(rabbitConnectionFactory)
        {
        }

        /// <summary>
        /// Sets the connection listeners.
        /// </summary>
        /// <value>The connection listeners.</value>
        public new IList<IConnectionListener> ConnectionListeners
        {
            set
            {
                base.ConnectionListeners = value;
                if (this.connection != null)
                {
                    this.ConnectionListener.OnCreate(this.connection);
                }
            }
        }

        /// <summary>
        /// Add a connection listener.
        /// </summary>
        /// <param name="listener">The listener.</param>
        public new void AddConnectionListener(IConnectionListener listener)
        {
            base.AddConnectionListener(listener);

            // If the connection is already alive we assume that the new listener wants to be notified
            if (this.connection != null)
            {
                listener.OnCreate(this.connection);
            }
        }

        /// <summary>
        /// Create a connection.
        /// </summary>
        /// <returns>The connection.</returns>
        public override IConnection CreateConnection()
        {
            lock (this.connectionMonitor)
            {
                if (this.connection == null)
                {
                    var target = this.DoCreateConnection();
                    this.connection = new SharedConnectionProxy(target, this);

                    // invoke the listener *after* this.connection is assigned
                    this.ConnectionListener.OnCreate(target);
                }
            }
            return this.connection;
        }

        /// <summary>
        /// Close the underlying shared connection.
        /// </summary>
        public override void Dispose()
        {
            lock (this.connectionMonitor)
            {
                if (this.connection != null)
                {
                    this.connection.Dispose();
                    this.connection = null;
                }
            }
        }

        /// <summary>
        /// Does the create connection.
        /// </summary>
        /// <returns>The connection.</returns>
        protected IConnection DoCreateConnection()
        {
            var connection = this.CreateBareConnection();
            return connection;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString() 
        {
            return "SingleConnectionFactory [host=" + this.Host + ", port=" + this.Port + "]";
        }
    }
}
