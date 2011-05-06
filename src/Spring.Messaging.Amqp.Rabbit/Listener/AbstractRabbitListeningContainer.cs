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
using Common.Logging;
using Spring.Context;
using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Objects.Factory;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    /// 
    /// </summary>
    /// <author>Mark Pollack</author>
    public abstract class AbstractRabbitListeningContainer : RabbitAccessor, IObjectNameAware, IDisposable, ILifecycle
    {
        #region Logging

        private readonly ILog logger = LogManager.GetLogger(typeof(AbstractRabbitListeningContainer));

        #endregion

        private volatile string objectName;

        private volatile IConnection sharedConnection;

        private volatile bool active = false;

        private volatile bool running = false;

        protected object lifecycleMonitor = new object();

        #region Implementation of IObjectNameAware

        /// <summary>
        /// Set the name of the object in the object factory that created this object.
        /// </summary>
        /// <value>The name of the object in the factory.</value>
        /// <remarks>
        /// 	<p>
        /// Invoked after population of normal object properties but before an init
        /// callback like <see cref="T:Spring.Objects.Factory.IInitializingObject"/>'s
        /// <see cref="M:Spring.Objects.Factory.IInitializingObject.AfterPropertiesSet"/>
        /// method or a custom init-method.
        /// </p>
        /// </remarks>
        public string ObjectName
        {
            set { objectName = value; }
            get { return objectName; }
        }

        #endregion

        public override void AfterPropertiesSet()
        {
            base.AfterPropertiesSet();
            ValidateConfiguration();
            Initialize();
        }

        /// <summary>
        /// Validates the configuration of this container
        /// </summary>
        /// <notes>
        /// The default implementation is empty.  To be overridden in subclasses.
        /// </notes>
        /// <see cref="Shutdown"/>
        protected virtual void ValidateConfiguration()
        {
            
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            Shutdown();
        }

        #endregion

        #region Lifecycle methods for starting and stopping the container

        /// <summary>
        /// Initializes this container.  Creates and calls a Rabbit Connection and 
        /// calls <see cref="DoInitialize"/>.
        /// </summary>
        public virtual void Initialize()
        {
            try
            {
                lock (lifecycleMonitor)
                {
                    this.active = true;
                    System.Threading.Monitor.PulseAll(this.lifecycleMonitor);
                }
                DoStart();
                DoInitialize();
            } catch (Exception)
            {
                ConnectionFactoryUtils.ReleaseConnection(this.sharedConnection, ConnectionFactory);
                throw;
            }
        }

        /// <summary>
        /// Stop the shared Connection, call <see cref="DoShutdown"/> and close
        /// this container
        /// </summary>
        public virtual void Shutdown()
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug("Shutting down messae listener container.");
            }
            bool wasRunning = false;
            lock (lifecycleMonitor)
            {
                wasRunning = this.running;
                this.running = false;
                this.active = false;
                System.Threading.Monitor.PulseAll(this.lifecycleMonitor);
            }

            // Stop shared Connection early, if necessary
            if (wasRunning && SharedConnectionEnabled)
            {
                try
                {
                    StopSharedConnection();
                } catch (Exception ex)
                {
                    logger.Debug("Could not stop Rabbit Connection on shutdown.");
                }
            }

            // Shut down the invokers
            try
            {
                DoShutdown();
            } catch (Exception ex)
            {
                // TODO look into what exceptino are thrown and if want new exception hierarchy
            } finally
            {
                if (SharedConnectionEnabled)
                {
                    ConnectionFactoryUtils.ReleaseConnection(this.sharedConnection, ConnectionFactory);
                    this.sharedConnection = null;
                }
            }
        }


        /// <summary>
        /// Gets a value indicating whether this container is currently active, that is
        /// whether it has been set up but not shut down yet.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        public bool IsActive
        {
            get
            {
                lock(lifecycleMonitor)
                {
                    return this.active;
                }
            }
        }

        /// <summary>
        /// Starts this container.
        /// </summary>
        public void Start()
        {
            DoStart();             
        }

        /// <summary>
        /// Start the shared Connection, if any, and notify all invoker tasks.
        /// </summary>
        /// <see cref="EstablishSharedConnection"/>
        protected virtual void DoStart()
        {
            // Lazily establish a shared Connection, if necessary
            if (SharedConnectionEnabled)
            {
                EstablishSharedConnection();
            }

            // Reschedule any paused tasks, if any.
            lock (lifecycleMonitor)
            {
                this.running = true;
                System.Threading.Monitor.PulseAll(this.lifecycleMonitor);
            }
        }


        /// <summary>
        /// Stop this container.
        /// </summary>
        public void Stop()
        {
            DoStop();
        }

        public bool IsRunning
        {
            get
            {
                lock (lifecycleMonitor)
                {
                    return (this.running && RunningAllowed());
                }
            }
        }

        /// <summary>
        /// Notify all invoker tasks and stop the shared Connection, if any.
        /// </summary>
        /// <see cref="StopSharedConnection"/>
        protected virtual void DoStop()
        {
            lock (this.lifecycleMonitor)
            {
                this.running = false;
                System.Threading.Monitor.PulseAll(this.lifecycleMonitor);
            }

            if (SharedConnectionEnabled)
            {
                StopSharedConnection();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this container is currently running, that is,
        /// whether it has been started and not stopped yet.
        /// </summary>
        /// <value><c>true</c> if running; otherwise, <c>false</c>.</value>
        public bool Running
        {
            get
            {
                lock(lifecycleMonitor)
                {
                    return (this.running && RunningAllowed());
                }
            }
        }


        /// <summary>
        /// Check whether this container's listeners are generally allowed to run.
        /// This implementation always returns true; the default 'running' state is 
        /// purely determined by <see cref="Start"/> / <see cref="Stop"/>.
        /// </summary>
        /// <remarks>Subclasses may override this method to check against temporary 
        /// conditions that prevent listeners from actually running. In other words, 
        /// they may apply further restrictions to the 'running' state, returning 
        /// false if such a restriction prevents listeners from running.
        /// </remarks>
        /// <returns>true if running is allowed, false otherise</returns>
        protected virtual bool RunningAllowed()
        {
            return true;
        }

        #endregion


        #region Management of a shared Rabbit Connection

        /// <summary>
        /// Establishes the shared connection for this container.
        /// </summary>
        /// <remarks>The default implementation delegates to <see cref="CreateSharedConnection"/>
        /// which does one immediate connection attempt and throws an exception if it fails.
        /// Can be overridden to have a recover process in place, retrying until a Connection can
        /// be successfully established.
        /// </remarks>
        protected virtual void EstablishSharedConnection()
        {
            if (this.sharedConnection == null)
            {
                this.sharedConnection = CreateSharedConnection();
                logger.Debug("Established shared Rabbit Connection");
            }
        }

        /// <summary>
        /// Creates the shared connection for this container.
        /// </summary>
        /// <remarks>The default implementation creates a standard Connection and prepares it
        /// through <see cref="PrepareSharedConnection"/>.
        /// </remarks>
        /// <returns>The prepared Connection</returns>
        protected virtual IConnection CreateSharedConnection()
        {
            IConnection con = CreateConnection();
            try
            {
                PrepareSharedConnection(con);
                return con;
            } catch (Exception ex)
            {
                RabbitUtils.CloseConnection(con);
                throw;
            }
        }

        /// <summary>
        /// Prepares the given connection, which is about to be registered as the
        /// shared connection for this container.
        /// </summary>
        /// <remarks>Subclasses can override this to apply further settings.
        /// </remarks>
        /// <param name="connection">The connection to prepare.</param>
        protected virtual void PrepareSharedConnection(IConnection connection)
        {

        }

        /// <summary>
        /// Stops the shared connection.
        /// </summary>
        protected virtual void StopSharedConnection()
        {
            if (this.sharedConnection != null)
            {
                try {
                    this.sharedConnection.Close();
                } catch (Exception ex)
                {
                    logger.Debug("Ignoring connection close exception - assuming already closed:  ", ex);
                }
            }
        }

        protected virtual IConnection SharedConnection
        {
            get
            {
                if (!SharedConnectionEnabled)
                {
                    throw new InvalidOperationException("This listener container does not maintain a shared Connection");
                }
                if (this.sharedConnection == null)
                {
                    throw new SharedConnectionNotInitializedException(
					"This listener container's shared Connection has not been initialized yet");		
                }
                return this.sharedConnection;
            }
        }

        #endregion



        #region Template methods to be implemented by subclasses

        /// <summary>
        /// Gets a value indicating whether a shared Rabbited connection should be maintained 
        /// by this container base class
        /// </summary>
        /// <value>
        /// 	<c>true</c> if shared connection enabled; otherwise, <c>false</c>.
        /// </value>
        protected abstract bool SharedConnectionEnabled { get;  }

        /// <summary>
        /// Registers any invokers within this container.  Subclasses
        /// need to implement this method for their specific invoker management
        /// process.
        /// </summary>
        protected abstract void DoInitialize();

        /// <summary>
        /// Close the registered invokers.  Subclasses need to implement this method for
        /// their specific invoker management process.
        /// </summary>
        /// <remarks>A shared Rabbit connection, if any, will automatically be closed afterwards.
        /// </remarks>
        /// <see cref="Shutdown"/>
        protected abstract void DoShutdown();
        

        #endregion

    }

    /// <summary>
    /// Exception that indicates that the initial setup of this container's
    /// shared Connection failed. This is indicating to invokers that they need
    /// to establish the shared Connection themselves on first access.
    /// </summary>
    public class SharedConnectionNotInitializedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedConnectionNotInitializedException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public SharedConnectionNotInitializedException(string message)
            : base(message)
        {
        }
    }
}