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
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Listener
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public abstract class AbstractMessageListenerContainer : AbstractRabbitListeningContainer
    {

        #region Logging

        private readonly ILog logger = LogManager.GetLogger(typeof(AbstractMessageListenerContainer));

        #endregion

        private volatile string queue;

        private IErrorHandler errorHandler;

        private bool exposeListenerChannel = true;

        private volatile object messageListener;

        protected bool autoAck = false;

        /// <summary>
        /// Gets or sets the name of the queue to receive messages from
        /// </summary>
        /// <value>The name of the queue. Can not be null.</value>
        public string Queue
        {
            get { return this.queue; }
            set
            {
                AssertUtils.ArgumentNotNull(value, "Queue");
                this.queue = value;
            }
        }

        public string RequiredQueueName()
        {
            AssertUtils.ArgumentNotNull(queue, "Queue");
            return this.queue;
        }

        public bool AutoAck
        {
            get { return this.autoAck; }
            set { this.autoAck = value; }
        }


        /// <summary>
        /// Exposes the listener channel to a registered 
        /// <see cref="Spring.Messaging.Amqp.Rabbit.Core.IChannelAwareMessageListener"/> as well as to 
        /// <see cref="Spring.Messaging.Amqp.Rabbit.Core.RabbitTemplate"/> calls.
        /// Default is true, reusing the listener's <see cref="IModel"/>
        /// </summary>
        /// <remarks>Turn this off to expose a fresh Rabbit Channel fetched from the
        /// same underlying Rabbit <see cref="IConnection"/> instead.  Note that 
        /// Channels managed by an external transaction manager will always get
        /// exposed to <see cref="Spring.Messaging.Amqp.Rabbit.Core.RabbitTemplate"/>
        /// calls.  So interms of RabbitTemplate exposure, this setting only affects locally
        /// transacted Channels.
        /// </remarks>
        /// <value>
        /// 	<c>true</c> if expose listener channel; otherwise, <c>false</c>.
        /// </value>
        /// <see cref="Spring.Messaging.Amqp.Rabbit.Core.IChannelAwareMessageListener"/>.
        public bool ExposeListenerChannel
        {
            get { return this.exposeListenerChannel; }
            set { this.exposeListenerChannel = value; }
        }


        /// <summary>
        /// Sets the message listener to register with the container.  This
        /// can be either a Spring <see cref="IMessageListener"/> object or
        /// a Spring <see cref="IChannelAwareMessageListener"/> object.
        /// </summary>
        /// <value>The message listener.</value>
        /// <exception cref="ArgumentException">If the supplied listener</exception>
        /// is not a 
        /// <see cref="IMessageListener"/>
        ///  or 
        /// <see cref="IChannelAwareMessageListener"/>
        /// <see cref="IMessageListener"/>
        public object MessageListener
        {
            set
            {
                CheckMessageListener(value);
                this.messageListener = value;
            }
            get
            {
                return this.messageListener;
            }
        }

        /// <summary>
        /// Checks the message listener, throwing an exception
        /// if it does not correspond to a supported listener type.
        /// By default, only a <see cref="IMessageListener"/> object or a
        /// Spring <see cref="IChannelAwareMessageListener"/> object will be accepted.
        /// </summary>
        /// <param name="messageListener">The message listener.</param>
        protected virtual void CheckMessageListener(object listenerToCheck)
        {
            AssertUtils.ArgumentNotNull(listenerToCheck, "IMessage Listener can not be null");
            if (!(listenerToCheck is IMessageListener || listenerToCheck is IChannelAwareMessageListener))
            {
                throw new ArgumentException("messageListener needs to be of type [" + typeof(IMessageListener).FullName + "] or [" + typeof(IChannelAwareMessageListener).FullName + "]");
            }
        }


        /// <summary>
        /// Sets an ErrorHandler to be invoked in case of any uncaught exceptions thrown
        /// while processing a Message. By default there will be no ErrorHandler
        /// so that error-level logging is the only result.
        /// </summary>
        /// <value>The error handler.</value>
        public IErrorHandler ErrorHandler
        {
            set
            {
                this.errorHandler = value;
            }
        }

        /// <summary>
        /// Invokes the error handler.
        /// </summary>
        /// <param name="exception">The exception.</param>
        protected virtual void InvokeErrorHandler(Exception exception)
        {
            if (errorHandler != null)
            {
                errorHandler.HandleError(exception);
            }
            else if (logger.IsWarnEnabled)
            {
                logger.Warn("Execution of Rabbit message listener failed, and no ErrorHandler has been set.", exception);
            }
        }

        #region Template methods for listener execution

        /// <summary>
        /// Executes the specified listener,
        /// committing or rolling back the transaction afterwards (if necessary).
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="message">The received message.</param>
        /// <see cref="InvokeListener"/>
        /// <see cref="CommitIfNecessary"/>
        /// <see cref="RollbackOnExceptionIfNecessary"/>
        /// <see cref="HandleListenerException"/>
        protected virtual void ExecuteListener(IModel channel, Message message)
        {
            try
            {
                DoExecuteListener(channel, message);
            }
            catch (Exception ex)
            {
                HandleListenerException(ex);
            }
        }

        /// <summary>
        /// Executes the specified listener, 
        /// committing or rolling back the transaction afterwards (if necessary).
        /// </summary>
        /// <param name="session">The session to operate on.</param>
        /// <param name="message">The received message.</param>
        /// <exception cref="NMSException">If thrown by NMS API methods.</exception>
        /// <see cref="InvokeListener"/>
        /// <see cref="CommitIfNecessary"/>
        /// <see cref="RollbackOnExceptionIfNecessary"/>
        protected virtual void DoExecuteListener(IModel channel, Message message)
        {
            if (!Running)
            {
                #region Logging
                if (logger.IsWarnEnabled)
                {
                    logger.Warn("Rejecting received message because of the listener container " +
                        "having been stopped in the meantime: " + message);
                }
                #endregion
                RollbackIfNecessary(channel);
                throw new MessageRejectedWhileStoppingException();
            }

            try
            {
                InvokeListener(channel, message);
            }
            catch (Exception ex)
            {
                RollbackOnExceptionIfNecessary(channel, ex);
                throw;
            }
            CommitIfNecessary(channel, message);
        }

        /// <summary>
        /// Invokes the specified listener: either as standard NMS MessageListener
        /// or (preferably) as Spring SessionAwareMessageListener.
        /// </summary>
        /// <param name="session">The session to operate on.</param>
        /// <param name="message">The received message.</param>
        /// <see cref="MessageListener"/>
        protected virtual void InvokeListener(IModel channel, Message message)
        {
            object listener = MessageListener;
            if (listener is IChannelAwareMessageListener)
            {
                DoInvokeListener((IChannelAwareMessageListener)listener, channel, message);
            }
            else if (listener is IMessageListener)
            {
                DoInvokeListener((IMessageListener)listener, message);
            }
            else if (listener != null)
            {
                throw new ArgumentException("Only IMessageListener and IChannelAwareMessageListener supported");
            }
            else
            {
                throw new InvalidOperationException("No message listener specified - see property MessageListener");
            }
        }

        /// <summary>
        /// Invoke the specified listener as Spring SessionAwareMessageListener,
        /// exposing a new Rabbit Channel (potentially with its own transaction)
        /// to the listener if demanded.
        /// </summary>
        /// <param name="listener">The Spring ISessionAwareMessageListener to invoke.</param>
        /// <param name="channel">The channel to operate on.</param>
        /// <param name="message">The received message.</param>
        /// <see cref="IChannelAwareMessageListener"/>
        /// <see cref="ExposeListenerChannel"/>
        protected virtual void DoInvokeListener(IChannelAwareMessageListener listener, IModel channel, Message message)
        {
            IConnection conToClose = null;
            IModel channelToClose = null;
            try
            {
                IModel channelToUse = channel;
                if (!ExposeListenerChannel)
                {
                    //We need to expose a separate Session.
                    conToClose = CreateConnection();
                    channelToClose = CreateChannel(conToClose);
                    channelToUse = channelToClose;
                }
                // Actually invoke the message listener
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Invoking listener with message of type [" + message.GetType() +
                                 "] and session [" + channelToUse + "]");
                }
                listener.OnMessage(message, channelToUse);

                /* TODO need to figure out tx usage more
                // Clean up specially exposed Channel, if any
                if (channelToUse != channel)
                {
                    if (channelToUse.Transacted && ChannelTransacted)
                    {
                        // Transacted session created by this container -> commit.
                        Rabbit.CommitIfNecessary(channelToUse);
                    }
                }*/

            }
            finally
            {
                RabbitUtils.CloseChannel(channelToClose);
                RabbitUtils.CloseConnection(conToClose);
            }
        }

        /// <summary>
        /// Invoke the specified listener a Spring Rabbit MessageListener.
        /// </summary>
        /// <remarks>Default implementation performs a plain invocation of the
        /// <code>OnMessage</code> methods</remarks>
        /// <param name="listener">The listener to invoke.</param>
        /// <param name="message">The received message.</param>
        protected virtual void DoInvokeListener(IMessageListener listener, Message message)
        {
            listener.OnMessage(message);
        }

        /// <summary>
        /// Perform a commit or message acknowledgement, as appropriate
        /// </summary>
        /// <param name="channel">The channel to commit.</param>
        /// <param name="message">The message to acknowledge.</param>
        protected virtual void CommitIfNecessary(IModel channel, Message message)
        {
            if (!autoAck)
            {
                if (channel.IsOpen)
                {
                    try
                    {
                        channel.BasicAck(message.MessageProperties.DeliveryTag, false);
                    } catch (Exception ex)
                    {
                        //TODO investigate what specific excxeption is thrown
                        logger.Warn("Could not ack message with delivery tag [" + message.MessageProperties.DeliveryTag + "]");
                    }
                }
            }
            if (this.IsChannelLocallyTransacted(channel))
            {
                RabbitUtils.CommitIfNecessary(channel);
            }
        }

        /// <summary>
        /// Perform a rollback, if appropriate.
        /// </summary>
        /// <param name="session">The session to rollback.</param>
        protected virtual void RollbackIfNecessary(IModel channel)
        {
            if (IsChannelLocallyTransacted(channel))
            {
                // Transacted session created by this container -> rollback
                RabbitUtils.RollbackIfNecessary(channel);
            }
        }

        /// <summary>
        /// Perform a rollback, handling rollback excepitons properly.
        /// </summary>
        /// <param name="channel">The channel to rollback.</param>
        /// <param name="ex">The thrown application exception.</param>
        protected virtual void RollbackOnExceptionIfNecessary(IModel channel, Exception ex)
        {
            try
            {
                if (IsChannelLocallyTransacted(channel))
                {
                    // Transacted session created by this container -> rollback
                    if (logger.IsDebugEnabled)
                    {
                        logger.Debug("Initiating transaction rollback on application exception");
                    }
                    RabbitUtils.RollbackIfNecessary(channel);
                }
            }
            catch (Exception)
            {
                logger.Error("Application exception overriden by rollback exception", ex);
                throw;
            }
        }

        /// <summary>
        /// Determines whether the given Channel is locally transacted, that is, whether
        /// its transaction is managed by this listener container's Channel handling
        /// and not by an external transaction coordinator.
        /// </summary>
        /// <remarks>
        /// This method is about finding out whether the Channel's transaction
        /// is local or externally coordinated.
        /// </remarks>
        /// <param name="channel">The channel to check.</param>
        /// <returns>
        /// 	<c>true</c> if the is channel locally transacted; otherwise, <c>false</c>.
        /// </returns>
        /// <see cref="RabbitAccessor.ChannelTransacted"/>
        protected virtual bool IsChannelLocallyTransacted(IModel channel)
        {
            return ChannelTransacted;
        }

        /// <summary>
        /// Handle the given exception that arose during listener execution.
        /// </summary>
        /// <remarks>
        /// The default implementation logs the exception at error level,
        /// not propagating it to the Rabbit provider - assuming that all handling of
        /// acknowledgement and/or transactions is done by this listener container.
        /// This can be overridden in subclasses.
        /// </remarks>
        /// <param name="ex">The exception to handle</param>
        protected virtual void HandleListenerException(Exception ex)
        {
            if (ex is MessageRejectedWhileStoppingException)
            {
                // Internal exception - has been handled before.
                return;
            }
            //TODO test if this use of exception handling is valid.
            /*
            if (ex.GetType().FullName.Contains("RabbitMQ.Client.Exceptions"))
            {
                InvokeExceptionListener(ex);
            }*/
            if (IsActive)
            {
                // Regular case: failed while active.
                // Invoke ErrorHandler if available.
                InvokeErrorHandler(ex);
            }
            else
            {
                // Rare case: listener thread failed after container shutdown.
                // Log at debug level, to avoid spamming the shutdown log.
                logger.Debug("Listener exception after container shutdown", ex);
            }
        }

        #endregion
    }

    /// <summary>
    /// Internal exception class that indicates a rejected message on shutdown.
    /// Used to trigger a rollback for an external transaction manager in that case.
    /// </summary>
    internal class MessageRejectedWhileStoppingException : ApplicationException
    {
    }
}