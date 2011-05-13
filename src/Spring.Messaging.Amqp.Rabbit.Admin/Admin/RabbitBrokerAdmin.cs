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

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Erlang.Connection;
using Spring.Erlang.Core;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Utils;
using Spring.Threading;
using Spring.Threading.Execution;
using Spring.Util;
using IConnectionFactory=Spring.Messaging.Amqp.Rabbit.Connection.IConnectionFactory;

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>
    /// Rabbit broker administration implementation
    /// </summary>
    /// <author>Mark Pollack</author>
    public class RabbitBrokerAdmin : IRabbitBrokerOperations
    {
        /// <summary>
        /// The default vhost.
        /// </summary>
        private static readonly string DEFAULT_VHOST = "/";

        /// <summary>
        /// The default node name.
        /// </summary>
        private static string DEFAULT_NODE_NAME;

        /// <summary>
        /// The default port.
        /// </summary>
        private static readonly int DEFAULT_PORT = 5672;

        /// <summary>
        /// The default encoding.
        /// </summary>
        private static readonly string DEFAULT_ENCODING = "UTF-8";

        #region Logging Definition

        private readonly ILog logger = LogManager.GetLogger(typeof(RabbitBrokerAdmin));

        #endregion

        /// <summary>
        /// The erlang template.
        /// </summary>
        private ErlangTemplate erlangTemplate;

        private string encoding = DEFAULT_ENCODING;

        /// <summary>
        /// The timeout.
        /// </summary>
        private long timeout = 0;

        /// <summary>
        /// The executor.
        /// </summary>
        private IExecutor executor;

        /// <summary>
        /// The node name.
        /// </summary>
        private readonly string nodeName = GetDefaultNodeName();

        /// <summary>
        /// The cookie.
        /// </summary>
        private readonly string cookie;

        /// <summary>
        /// The port.
        /// </summary>
        private readonly int port;

        /// <summary>
        /// The rabbit log base directory.
        /// </summary>
        private string rabbitLogBaseDirectory;

        /// <summary>
        /// The rabbit mnesia base directory.
        /// </summary>
        private string rabbitMnesiaBaseDirectory;

        /// <summary>
        /// The module adapter.
        /// </summary>
        private IDictionary<String, String> moduleAdapter = new Dictionary<String, String>();

        /// <summary>
        /// Gets the default name of the node.
        /// </summary>
        /// <returns>The default node name.</returns>
        /// <remarks></remarks>
        private static string GetDefaultNodeName()
        {
            try
            {
                var hostName = Dns.GetHostName();
                return "rabbit@" + hostName;
            }
            catch (Exception e)
            {
                return "rabbit@localhost";
            }
        }

        public RabbitBrokerAdmin() : this(DEFAULT_NODE_NAME)
        {
        }

        /**
         * Create an instance by supplying the erlang node name (e.g. "rabbit@myserver"), or simply the hostname (if the
         * alive name is "rabbit").
         * 
         * @param nodeName the node name or hostname to use
         */
        public RabbitBrokerAdmin(string nodeName) : this(nodeName, null)
        {
        }

        /**
         * Create an instance by supplying the erlang node name and cookie (unique string).
         * 
         * @param nodeName the node name or hostname to use
         * 
         * @param cookie the cookie value to use
         */
        public RabbitBrokerAdmin(string nodeName, string cookie) : this(nodeName, DEFAULT_PORT, cookie)
        {
        }

        /**
         * Create an instance by supplying the erlang node name and port number. Use this on a UN*X system if you want to
         * run the broker as a user without root privileges, supplying values that do not clash with the default broker
         * (usually "rabbit@&lt;servername&gt;" and 5672). If, as well as managing an existing broker, you need to start the
         * broker process, you will also need to set {@link #setRabbitLogBaseDirectory(String) RABBITMQ_LOG_BASE} and
         * {@link #setRabbitMnesiaBaseDirectory(String) RABBITMQ_MNESIA_BASE} to point to writable directories).
         * 
         * @param nodeName the node name or hostname to use
         * @param port the port number (overriding the default which is 5672)
         */
        public RabbitBrokerAdmin(string nodeName, int port) : this(nodeName, port, null)
        {
        }

        /**
	 * Create an instance by supplying the erlang node name, port number and cookie (unique string). If the node name
	 * does not contain an <code>@</code> character, it will be prepended with an alivename <code>rabbit@</code>
	 * (interpreting the supplied value as just the hostname).
	 * 
	 * @param nodeName the node name or hostname to use
	 * @param port the port number (overriding the default which is 5672)
	 * @param cookie the cookie value to use
	 */
        public RabbitBrokerAdmin(string nodeName, int port, string cookie)
        {

            if (!nodeName.Contains("@"))
            {
                nodeName = "rabbit@" + nodeName; // it was just the host
            }

            var parts = nodeName.Split("@");
            AssertUtils.State(parts.Length == 2, "The node name should be in the form alivename@host, e.g. rabbit@myserver");
            if (/*Os.isFamily("windows")*/ true && !DEFAULT_NODE_NAME.Equals(nodeName))
            {
                nodeName = parts[0] + "@" + parts[1].ToUpper();
            }

            this.port = port;
            this.cookie = cookie;
            this.nodeName = nodeName;

            // TODO: This doesn't look right... is it right?
            var executor = Executors.NewCachedThreadPool();

            //executor.setDaemon(true);
            this.executor = executor;
        }

        /**
	 * An async task executor for launching background processing when starting or stopping the broker.
	 * 
	 * @param executor the executor to set
	 */
	public IExecutor Executor
    {
        set { this.executor = value; }
	}

	/**
	 * The location of <code>RABBITMQ_LOG_BASE</code> to override the system default (which may be owned by another
	 * user). Only needed for launching the broker process. Can also be set as a system property.
	 * 
	 * @param rabbitLogBaseDirectory the rabbit log base directory to set
	 */
	public string RabbitLogBaseDirectory
    {
		set { this.rabbitLogBaseDirectory = value; }
    }
	/**
	 * The location of <code>RABBITMQ_MNESIA_BASE</code> to override the system default (which may be owned by another
	 * user). Only needed for launching the broker process. Can also be set as a system property.
	 * 
	 * @param rabbitMnesiaBaseDirectory the rabbit Mnesia base directory to set
	 */
	public string RabbitMnesiaBaseDirectory
    {
		set { this.rabbitMnesiaBaseDirectory = value; }
    }
	/**
	 * The encoding to use for converting host names to byte arrays (which is needed on the remote side).
	 * @param encoding the encoding to use (default UTF-8)
	 */
	public string Encoding
    {
		set { this.encoding = value; }
    }

	/**
	 * Timeout (milliseconds) to wait for the broker to come up. If the provided timeout is greater than zero then we
	 * wait for that period for the broker to be ready. If it is not ready after that time the process is stopped.
	 * Defaults to 0 (no wait).
	 * 
	 * @param timeout the timeout value to set in milliseconds
	 */
	public long StartupTimeout
    {
		set { this.timeout = value; }
    }

	/**
	 * Allows users to adapt Erlang RPC <code>(module, function)</code> pairs to older, or different, versions of the
	 * broker than the current target. The map is from String to String in the form
	 * <code>input_module%input_function -> output_module%output_function</code> (using a <code>%</code> separator).
	 * 
	 * @param moduleAdapter the module adapter to set
	 */
	public IDictionary<string, string> ModuleAdapter
    {
		set { this.moduleAdapter = value; }
	}

	public List<QueueInfo> GetQueues() {
        return (List<QueueInfo>)this.erlangTemplate.ExecuteAndConvertRpc("rabbit_amqqueue", "info_all", this.GetBytes(DEFAULT_VHOST));
	}

	public List<QueueInfo> GetQueues(string virtualHost) {
        return (List<QueueInfo>)this.erlangTemplate.ExecuteAndConvertRpc("rabbit_amqqueue", "info_all", this.GetBytes(virtualHost));
	}

	// User management

	public void AddUser(string username, string password) {
        this.erlangTemplate.ExecuteAndConvertRpc("rabbit_auth_backend_internal", "add_user", this.GetBytes(username), this.GetBytes(password));
	}

	public void DeleteUser(string username) {
        this.erlangTemplate.ExecuteAndConvertRpc("rabbit_auth_backend_internal", "delete_user", this.GetBytes(username));
	}

	public void ChangeUserPassword(string username, string newPassword) {
		this.erlangTemplate.ExecuteAndConvertRpc("rabbit_auth_backend_internal", "change_password", this.GetBytes(username), this.GetBytes(newPassword));
	}

	public List<string> ListUsers() {
        return (List<string>)this.erlangTemplate.ExecuteAndConvertRpc("rabbit_auth_backend_internal", "list_users");
	}

    public int AddVhost(string vhostPath)
    {
		// TODO Auto-generated method stub
		return 0;
	}

    public int DeleteVhost(string vhostPath)
    {
		// TODO Auto-generated method stub
		return 0;
	}

    public void SetPermissions(string username, Regex configure, Regex read, Regex write)
    {
		// TODO Auto-generated method stub
	}

    public void SetPermissions(string username, Regex configure, Regex read, Regex write, string vhostPath)
    {
		// TODO Auto-generated method stub
	}

    public void ClearPermissions(string username)
    {
		// TODO Auto-generated method stub
	}

    public void ClearPermissions(string username, string vhostPath)
    {
		// TODO Auto-generated method stub
	}

    public List<string> ListPermissions()
    {
		// TODO Auto-generated method stub
		return null;
	}

    public List<string> ListPermissions(string vhostPath)
    {
		// TODO Auto-generated method stub
		return null;
	}

    public List<string> ListUserPermissions(string username)
    {
		// TODO Auto-generated method stub
		return null;
	}

	public void StartBrokerApplication() {
		var status = this.Status;
		if (status.IsReady) {
			logger.Info("Rabbit Application already running.");
			return;
		}
		if (!status.IsAlive) {
			logger.Info("Rabbit Process not running.");
			this.StartNode();
			return;
		}
		logger.Info("Starting Rabbit Application.");

		// This call in particular seems to be prone to hanging, so do it in the background...
		var latch = new CountDownLatch(1);
		var result = executor.Execute(delegate(){
			
				try {
					return this.erlangTemplate.ExecuteAndConvertRpc("rabbit", "start");
				} finally {
					latch.CountDown();
				}
		});
		bool started = false;
		try {
			started = latch.Await(new TimeSpan(0,0,0,0, (int)timeout));
		} catch (ThreadInterruptedException e) {
			Thread.CurrentThread.Interrupt();
			result.Cancel(true);
			return;
		}
		if (timeout > 0 && started) {
			if (!this.WaitForReadyState() && !result.IsDone) {
				result.Cancel(true);
			}
		}
	}

	public void StopBrokerApplication() 
    {
        this.logger.Info("Stopping Rabbit Application.");
		this.erlangTemplate.ExecuteAndConvertRpc("rabbit", "stop");
		if (timeout > 0) {
			this.WaitForUnreadyState();
		}
	}

        public RabbitBrokerAdmin(IConnectionFactory connectionFactory)
        {
            this.virtualHost = connectionFactory.VirtualHost;
            this.rabbitTemplate = new RabbitTemplate(connectionFactory);
            this.rabbitAdmin = new RabbitAdmin(rabbitTemplate);
            InitializeDefaultErlangTemplate(rabbitTemplate);
        }

        private void InitializeDefaultErlangTemplate(RabbitTemplate template)
        {
            string peerNodeName = "rabbit@" + template.ConnectionFactory.Host;
            logger.Debug("Creating Erlang.NET connection with peerNodeName = [" + peerNodeName + "]");
            SimpleConnectionFactory otpCf = new SimpleConnectionFactory("rabbit-spring-monitor-net", peerNodeName);
            otpCf.AfterPropertiesSet();
            CreateErlangTemplate(otpCf);
        }

        private void CreateErlangTemplate(SimpleConnectionFactory factory)
        {
            erlangTemplate = new ErlangTemplate(factory);
            erlangTemplate.ErlangConverter = new RabbitControlErlangConverter();
            erlangTemplate.AfterPropertiesSet();
        }

        #region Implementation of IRabbitBrokerOperations

        public void RemoveBinding(Binding binding)
        {
            rabbitTemplate.Execute<object>(delegate(IModel model)
                                               {
                                                   model.QueueUnbind(binding.Queue, binding.Exchange, binding.RoutingKey,
                                                                     binding.Arguments);
                                                   return null;
                                               });
        }

        public RabbitStatus Status
        {
            get
            {
                return (RabbitStatus)erlangTemplate.ExecuteAndConvertRpc("rabbit", "status");
            }
        }

        public IList<QueueInfo> Queues
        {
            get {
                return
                    (IList<QueueInfo>)
                    erlangTemplate.ExecuteAndConvertRpc("rabbit_amqqueue", "info_all", encoding.GetBytes(virtualHost)); }
        }

        public void AddUser(string username, string password)
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit_access_control", "add_user", encoding.GetBytes(username), encoding.GetBytes(password));
        }

        public void DeleteUser(string username)
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit_access_control", "delete_user", encoding.GetBytes(username));
        }

        public void ChangeUserPassword(string username, string newPassword)
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit_access_control", "change_password", encoding.GetBytes(username),
                                                encoding.GetBytes(newPassword));		

        }

        public IList<string> ListUsers()
        {
            return (IList<string>)erlangTemplate.ExecuteAndConvertRpc("rabbit_access_control", "list_users");
        }

        #endregion

        public void DeclareExchange(IExchange exchange)
        {
            rabbitAdmin.DeclareExchange(exchange);
        }

        public void DeleteExchange(string exchangeName)
        {
            rabbitAdmin.DeleteExchange(exchangeName);
        }

        public Queue DeclareQueue()
        {
            return rabbitAdmin.DeclareQueue();
        }

        public void DeclareQueue(Queue queue)
        {
            rabbitAdmin.DeclareQueue(queue);
        }

        public void DeleteQueue(string queueName)
        {
            rabbitAdmin.DeleteQueue(queueName);
        }

        public void DeleteQueue(string queueName, bool unused, bool empty)
        {
            rabbitAdmin.DeleteQueue(queueName, unused, empty);
        }

        public void PurgeQueue(string queueName, bool noWait)
        {
            rabbitAdmin.PurgeQueue(queueName, noWait);
        }

        public void DeclareBinding(Binding binding)
        {
            rabbitAdmin.DeclareBinding(binding);
        }

        public void StartBrokerApplication()
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit", "start");
        }

        public void StopBrokerApplication()
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit", "stop");
        }

        /// <summary>
        /// Starts the node. NOT YET IMPLEMENTED!
        /// </summary>
        /// Starts the Erlang node where RabbitMQ is running by shelling out to the directory specified by RABBIT_HOME and
        /// executing the standard named start script.  It spawns the shell command execution into its own thread.
        public void StartNode()
        {
            throw new NotImplementedException();
        }

        public void StopNode()
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit", "stop_and_halt");
        }

        public void ResetNode()
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit_mnesia", "reset");
        }

        public void ForceResetNode()
        {
            erlangTemplate.ExecuteAndConvertRpc("rabbit_mnesia", "force_reset");
        }

        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The byte representation of the string.</returns>
        /// Safely convert a string to its bytes using the encoding provided.
        /// @see #setEncoding(String)
        /// @param string the value to convert
        /// @return the bytes from the string using the encoding provided
        /// @throws IllegalStateException if the encoding is ont supported
        /// <remarks></remarks>
        private byte[] GetBytes(string value)
        {
            try
            {
                return SerializationUtils.SerializeString(value, this.encoding);
            }
            catch (Exception e)
            {
                throw new Exception("Unsupported encoding: " + this.encoding);
            }
        }

    }

    /// <summary>
    /// A status callback interface.
    /// </summary>
    /// <remarks></remarks>
    internal interface IStatusCallback
    {
        /// <summary>
        /// Gets the specified status.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns>Value indicating status.</returns>
        /// <remarks></remarks>
        bool Get(RabbitStatus status);
    }
}