    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
    using Common.Logging;
    using NUnit.Framework;
    using Spring.Messaging.Amqp.Core;
    using Spring.Messaging.Amqp.Rabbit.Connection;
    using Spring.Messaging.Amqp.Rabbit.Core;
    using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Test
{
    public class BrokerRunning
    {
        /// <summary>
        /// The default queue name.
        /// </summary>
        private static readonly string DEFAULT_QUEUE_NAME = typeof(BrokerRunning).Name;

        /// <summary>
        /// The logger.
        /// </summary>
        private static readonly ILog logger = LogManager.GetLogger(typeof(BrokerRunning));

        // Static so that we only test once on failure: speeds up test suite
        private static IDictionary<int, bool> brokerOnline = new Dictionary<int, bool>();

        /// <summary>
        /// The broker offline flag. Static so that we only test once on failure.
        /// </summary>
        private static IDictionary<int, bool> brokerOffline = new Dictionary<int, bool>();

        /// <summary>
        /// The assume online flag.
        /// </summary>
        private readonly bool assumeOnline;

        /// <summary>
        /// The purge flag.
        /// </summary>
        private readonly bool purge;

        /// <summary>
        /// The queues.
        /// </summary>
        private Queue[] queues;

        /// <summary>
        /// The default port.
        /// </summary>
        private int DEFAULT_PORT = BrokerTestUtils.GetPort();

        /// <summary>
        /// The port.
        /// </summary>
        private int port;

        /// <summary>
        /// The host name.
        /// </summary>
        private string hostName = null;

        /// <summary>
        /// Determines whether [is running with empty queues] [the specified names].
        /// </summary>
        /// <param name="names">The names.</param>
        /// Ensure the broker is running and has an empty queue with the specified name in the default exchange.
        /// @return a new rule that assumes an existing running broker
        /// <remarks></remarks>
        public static BrokerRunning IsRunningWithEmptyQueues(params string[] names)
        {
            var queues = new Queue[names.Length];
            for (int i = 0; i < queues.Length; i++)
            {
                queues[i] = new Queue(names[i]);
            }
            return new BrokerRunning(true, true, queues);
        }

        /// <summary>
        /// Determines whether [is running with empty queues] [the specified queues].
        /// </summary>
        /// <param name="queues">The queues.</param>
        /// Ensure the broker is running and has an empty queue (which can be addressed via the default exchange).
        /// @return a new rule that assumes an existing running broker
        /// <remarks></remarks>
        public static BrokerRunning IsRunningWithEmptyQueues(params Queue[] queues)
        {
            return new BrokerRunning(true, true, queues);
        }

        /// <summary>
        /// Determines whether this instance is running.
        /// </summary>
        /// @return a new rule that assumes an existing running broker
        /// <remarks></remarks>
        public static BrokerRunning IsRunning()
        {
            return new BrokerRunning(true);
        }

        /// <summary>
        /// Determines whether [is not running].
        /// </summary>
        /// @return a new rule that assumes there is no existing broker
        /// <remarks></remarks>
        public static BrokerRunning IsNotRunning()
        {
            return new BrokerRunning(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerRunning"/> class. 
        /// Prevents a default instance of the <see cref="BrokerRunning"/> class from being created.
        /// </summary>
        /// <param name="assumeOnline">
        /// if set to <c>true</c> [assume online].
        /// </param>
        /// <param name="purge">
        /// if set to <c>true</c> [purge].
        /// </param>
        /// <param name="queues">
        /// The queues.
        /// </param>
        /// <remarks>
        /// </remarks>
        private BrokerRunning(bool assumeOnline, bool purge, params Queue[] queues)
        {
            this.assumeOnline = assumeOnline;
            this.queues = queues;
            this.purge = purge;
            this.Port = this.DEFAULT_PORT;
            this.Apply();
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="BrokerRunning"/> class from being created.
        /// </summary>
        /// <param name="assumeOnline">if set to <c>true</c> [assume online].</param>
        /// <param name="queues">The queues.</param>
        /// <remarks></remarks>
        private BrokerRunning(bool assumeOnline, params Queue[] queues): this(assumeOnline, false, queues)
        {
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="BrokerRunning"/> class from being created.
        /// </summary>
        /// <param name="assumeOnline">if set to <c>true</c> [assume online].</param>
        /// <remarks></remarks>
        private BrokerRunning(bool assumeOnline) : this(assumeOnline, new Queue(DEFAULT_QUEUE_NAME))
        {
        }

        /// <summary>
        /// Sets a value indicating whether this <see cref="BrokerRunning"/> is port.
        /// </summary>
        /// <value><c>true</c> if port; otherwise, <c>false</c>.</value>
        /// @param port the port to set
        /// <remarks></remarks>
        public int Port
        {
            set
            {
                this.port = value;
                if (!brokerOffline.ContainsKey(this.port))
                {
                    brokerOffline.Add(this.port, true);
                }
                if (!brokerOnline.ContainsKey(this.port))
                {
                    brokerOnline.Add(this.port, true);
                }
            }
        }

        /// <summary>
        /// Sets the name of the host.
        /// </summary>
        /// <value>The name of the host.</value>
        /// @param hostName the hostName to set
        /// <remarks></remarks>
        public string HostName
        {
            set { this.hostName = value; }
        }

        /// <summary>
        /// Applies this instance.
        /// </summary>
        /// <returns>Something here.</returns>
        /// <remarks></remarks>
        public bool Apply()
        {
            // Check at the beginning, so this can be used as a static field
            if (this.assumeOnline)
            {
                Assume.That(brokerOnline[this.port] == true);
            }
            else
            {
                Assume.That(brokerOffline[this.port] == true);
            }

            var connectionFactory = new CachingConnectionFactory();

            try
            {
                connectionFactory.Port = this.port;
                if (StringUtils.HasText(this.hostName))
                {
                    connectionFactory.Host = this.hostName;
                }

                var admin = new RabbitAdmin(connectionFactory);

                foreach (var queue in this.queues)
                {
                    var queueName = queue.Name;

                    if (this.purge)
                    {
                        logger.Debug("Deleting queue: " + queueName);

                        // Delete completely - gets rid of consumers and bindings as well
                        admin.DeleteQueue(queueName);
                    }

                    if (this.IsDefaultQueue(queueName))
                    {
                        // Just for test probe.
                        admin.DeleteQueue(queueName);
                    }
                    else
                    {
                        admin.DeclareQueue(queue);
                    }
                }

                if (brokerOffline.ContainsKey(this.port))
                {
                    brokerOffline[this.port] = false;
                }
                else
                {
                    brokerOffline.Add(this.port, false);
                }
                
                if (!this.assumeOnline)
                {
                    Assume.That(brokerOffline[this.port] == true);
                }

            }
            catch (Exception e)
            {
                logger.Warn("Not executing tests because basic connectivity test failed", e);
                if (brokerOnline.ContainsKey(this.port))
                {
                    brokerOnline[this.port] = false;
                }
                else
                {
                    brokerOnline.Add(this.port, false);
                }
                    
                if (this.assumeOnline)
                {
                    Assume.That(!(e is Exception));
                }
            }
            finally
            {
                connectionFactory.Dispose();
            }

            return true;
            // return base.Apply(base, method, target);
        }

        /// <summary>
        /// Determines whether [is default queue] [the specified queue].
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <returns><c>true</c> if [is default queue] [the specified queue]; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        private bool IsDefaultQueue(string queue)
        {
            return DEFAULT_QUEUE_NAME.Equals(queue);
        }

    }
}
