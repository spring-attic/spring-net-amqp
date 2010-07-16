using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using Common.Logging;
using RabbitMQ.Client;
using Spring.Expressions;
using Spring.Messaging.Amqp.Core;
using Spring.Messaging.Amqp.Rabbit.Core;
using Spring.Messaging.Amqp.Rabbit.Support;
using Spring.Messaging.Amqp.Support.Converter;
using Spring.Util;


namespace Spring.Messaging.Amqp.Rabbit.Listener.Adapter
{
    /// <summary>
    /// Message listener adapter that delegates the handling of messages to target
    /// listener methods via reflection, with flexible message type conversion.
    /// Allows listener methods to operate on message content types, completely
    /// independent from the Rabbit API.
    /// </summary>
    /// <remarks>
    /// <para>By default, the content of incoming messages gets extracted before
    /// being passed into the target listener method, to let the target method
    /// operate on message content types such as String or byte array instead of
    /// the raw Message. Message type conversion is delegated to a Spring
    /// <see cref="IMessageConverter"/>. By default, a <see cref="SimpleMessageConverter"/>
    /// will be used. (If you do not want such automatic message conversion taking
    /// place, then be sure to set the <see cref="MessageConverter"/> property
    /// to <code>null</code>.)
    /// </para>
    /// <para>If a target listener method returns a non-null object (typically of a
    /// message content type such as <code>String</code> or byte array), it will get
    /// wrapped in a Rabbit <code>Message</code> and sent to the exchange of the incoming message
    /// with the routing key that comes from the Rabbit ReplyTo property if available or via the 
    /// <see cref="DefaultResponseRoutingKey"/> property.
    /// </para>
    /// <para>
    /// The sending of response messages is only available when
    /// using the <see cref="IChannelAwareMessageListener"/> entry point (typically through a
    /// Spring message listener container). Usage as a MessageListener
    /// does <i>not</i> support the generation of response messages.
    /// </para>
    /// <para>Consult the reference documentation for examples of method signatures compliant with this
    /// adapter class.
    /// </para>
    /// </remarks>
    /// <author>Mark Pollack</author>
    /// <author>Mark Pollack (.NET)</author>
    public class MessageListenerAdapter : IMessageListener, IChannelAwareMessageListener
    {
        #region Logging

        private readonly ILog logger = LogManager.GetLogger(typeof (MessageListenerAdapter));

        #endregion

        /// <summary>
        /// The default handler method name.
        /// </summary>
        public static string ORIGINAL_DEFAULT_HANDLER_METHOD = "HandleMessage";

        private static string DEFAULT_RESPONSE_EXCHANGE = "";

        #region Fields

        private object handlerObject;

        private string defaultHandlerMethod = ORIGINAL_DEFAULT_HANDLER_METHOD;

        private IExpression processingExpression;

        private string defaultResponseRoutingKey;

        private volatile bool mandatoryPublish;

        private volatile bool immediatePublish;

        private IMessageConverter messageConverter;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageListenerAdapter"/> class with default settings.
        /// </summary>
        public MessageListenerAdapter()
        {
            InitDefaultStrategies();
            handlerObject = this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageListenerAdapter"/> class for the given handler object
        /// </summary>
        /// <param name="handlerObject">The delegate object.</param>
        public MessageListenerAdapter(object handlerObject)
        {
            InitDefaultStrategies();
            this.handlerObject = handlerObject;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the handler object to delegate message listening to.
        /// </summary>
        /// <remarks>
	    /// Specified listener methods have to be present on this target object.
	    /// If no explicit handler object has been specified, listener
	    /// methods are expected to present on this adapter instance, that is,
	    /// on a custom subclass of this adapter, defining listener methods.
        /// </remarks>
        /// <value>The handler object.</value>
        public object HandlerObject
        {
            get { return handlerObject; }
            set { handlerObject = value; }
        }

        /// <summary>
        /// Gets or sets the default handler method to delegate to,
	    /// for the case where no specific listener method has been determined.
	    /// Out-of-the-box value is <see cref="ORIGINAL_DEFAULT_HANDLER_METHOD"/> ("HandleMessage"}.
        /// </summary>
        /// <value>The default handler method.</value>
        public string DefaultHandlerMethod
        {
            get { return defaultHandlerMethod; }
            set
            {
                defaultHandlerMethod = value;
                processingExpression = Expression.Parse(defaultHandlerMethod + "(#convertedObject)");
            }
        }


        /// <summary>
        /// Sets the default routing key to use when sending response messages. This will be applied
	    /// in case of a request message that does not carry a "ReplyTo" property.
	    /// Response destinations are only relevant for listener methods that return
	    /// result objects, which will be wrapped in a response message and sent to a
	    /// response destination.
        /// </summary>
        /// <value>The default ReplyTo value.</value>
        public string DefaultResponseRoutingKey
        {
            set { this.defaultResponseRoutingKey = value; }
        }

        /// <summary>
        /// Gets or sets the message converter that will convert incoming Rabbit messages to
	    /// listener method arguments, and objects returned from listener
	    /// methods back to Rabbit messages.
	    /// </summary>
	    /// <remarks>
	    /// <para>The default converter is a {@link SimpleMessageConverter}, which is able
	    /// to handle Byte arrays and strings.
	    /// </para>
        /// </remarks>
        /// <value>The message converter.</value>
        public IMessageConverter MessageConverter
        {
            get { return messageConverter; }
            set { messageConverter = value; }
        }

        public bool MandatoryPublish
        {           
            set { mandatoryPublish = value; }
        }

        public bool ImmediatePublish
        {
            set { immediatePublish = value; }
        }

        #endregion


        /// <summary>
	    /// Rabbit <see cref="IMessageListener"/> entry point.
	    /// <para>Delegates the message to the target listener method, with appropriate
	    /// conversion of the message arguments
	    /// </para>
	    /// </summary>
	    /// <remarks>
	    /// In case of an exception, the <see cref="HandleListenerException"/> method will be invoked.
	    /// <b>Note</b> 
	    /// Does not support sending response messages based on
	    /// result objects returned from listener methods. Use the
	    /// <see cref="IChannelAwareMessageListener"/> entry point (typically through a Spring
	    /// message listener container) for handling result objects as well.
        /// </remarks>
        /// <param name="message">The incoming message.</param>
        public void OnMessage(Message message)
        {
            try
            {
                OnMessage(message, null);
            }
            catch (Exception e)
            {
                HandleListenerException(e);
            }
        }

        /// <summary>
	    /// Spring <see cref="IChannelAwareMessageListener"/> entry point.
	    /// <para>
	    /// Delegates the message to the target listener method, with appropriate
	    /// conversion of the message argument. If the target method returns a
	    /// non-null object, wrap in a Rabbit message and send it back.
	    /// </para>
        /// </summary>
        /// <param name="message">The incoming message.</param>
        /// <param name="channel">The channel to operate on.</param>
        public void OnMessage(Message message, IModel channel)
        {
            if (handlerObject != this)
            {
                if (typeof(IChannelAwareMessageListener).IsInstanceOfType(handlerObject))
                {
                    if (channel != null)
                    {
                        ((IChannelAwareMessageListener)handlerObject).OnMessage(message, channel);
                        return;
                    }
                    else if (!typeof(IMessageListener).IsInstanceOfType(handlerObject))
                    {
                        throw new InvalidOperationException("MessageListenerAdapter cannot handle a " +
                            "IChannelAwareMessageListener delegate if it hasn't been invoked with a Channel itself");
                    }
                }
                if (typeof(IMessageListener).IsInstanceOfType(handlerObject))
                {
                    ((IMessageListener)handlerObject).OnMessage(message);
                    return;
                }
            }

            // Regular case: find a handler method reflectively.
            object convertedMessage = ExtractMessage(message);


            IDictionary vars = new Hashtable();
            vars["convertedObject"] = convertedMessage;

            //Invoke message handler method and get result.
            object result;
            try
            {
                result = processingExpression.GetValue(handlerObject, vars);
            }            
            // Will only happen if dynamic method invocation falls back to standard reflection.
            catch (TargetInvocationException ex)
            {
                Exception targetEx = ex.InnerException;
                //TODO assuming EndOfStreamException came from Rabbit itself.
                if (ObjectUtils.IsAssignable(typeof(EndOfStreamException), targetEx))
                {
                    throw ReflectionUtils.UnwrapTargetInvocationException(ex);
                }
                else
                {
                    throw new ListenerExecutionFailedException("Listener method '" + defaultHandlerMethod + "' threw exception", targetEx);
                }
            }
            catch (Exception ex)
            {
                throw new ListenerExecutionFailedException("Failed to invoke target method '" + defaultHandlerMethod +
                                                           "' with argument " + convertedMessage, ex);
            }

            if (result != null)
            {
                HandleResult(result, message, channel);
            }
            else
            {
                logger.Debug("No result object given - no result to handle");
            }
        }

        /// <summary>
        /// Initialize the default implementations for the adapter's strategies.
        /// </summary>
        protected virtual void InitDefaultStrategies()
        {
            MessageConverter = new SimpleMessageConverter();
            processingExpression = Expression.Parse(defaultHandlerMethod + "(#convertedObject)");
        }

        /// <summary>
        /// Handle the given exception that arose during listener execution.
	    /// The default implementation logs the exception at error level.
	    /// <para>This method only applies when used with <see cref="IMessageListener"/>.
	    /// In case of the Spring <see cref="IChannelAwareMessageListener"/> mechanism,
	    /// exceptions get handled by the caller instead.
	    /// </para>
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        protected virtual void HandleListenerException(Exception ex)
        {
            logger.Error("Listener execution failed", ex);
        }

        /// <summary>
        /// Extract the message body from the given message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>the content of the message, to be passed into the
        /// listener method as argument</returns>
        private object ExtractMessage(Message message)
        {
            IMessageConverter converter = MessageConverter;
            if (converter != null)
            {
                return converter.FromMessage(message);
            }
            return message;
        }

        /// <summary>
        /// Gets the name of the listener method that is supposed to
	    /// handle the given message.
	    /// The default implementation simply returns the configured
	    /// default listener method, if any.
        /// </summary>
        /// <param name="originalMessage">The EMS request message.</param>
        /// <param name="extractedMessage">The converted Rabbit request message,
        /// to be passed into the listener method as argument.</param>
        /// <returns>the name of the listener method (never <code>null</code>)</returns>
        protected virtual string GetHandlerMethodName(Message originalMessage, object extractedMessage)
        {
            return DefaultHandlerMethod;
        }

        /// <summary>
        /// Handles the given result object returned from the listener method, sending a response message back. 
        /// </summary>
        /// <param name="result">The result object to handle (never <code>null</code>).</param>
        /// <param name="request">The original request message.</param>
        /// <param name="channel">The channel to operate on (may be <code>null</code>).</param>
        protected virtual void HandleResult(object result, Message request, IModel channel)
        {
            if (channel != null)
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Listener method returned result [" + result +
                                 "] - generating response message for it");
                }
                Message response = BuildMessage(channel, result);
                PostProcessResponse(request, response);
                String replyTo = GetResponseReplyTo(request, response, channel);
                SendResponse(channel, replyTo, response);
            }
            else
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Listener method returned result [" + result +
                                 "]: not generating response message for it because of no Rabbit Channel given");
                }
            }
        }

        /// <summary>
        /// Builds a Rabbit message to be sent as response based on the given result object.
        /// </summary>
        /// <param name="channel">The Rabbit Channel to operate on.</param>
        /// <param name="result">The content of the message, as returned from the listener method.</param>
        /// <returns>the Rabbit <code>Message</code> (never <code>null</code>)</returns>
        /// <exception cref="MessageConversionException">If there was an error in message conversion</exception>
        protected virtual Message BuildMessage(IModel channel, Object result)
        {
            IMessageConverter converter = MessageConverter;
            if (converter != null)
            {
                return converter.ToMessage(result, new RabbitMessagePropertiesFactory(channel));
            }
            else
            {
                Message msg = result as Message;
                if (msg == null)
                {
                    throw new MessageConversionException(
                        "No IMessageConverter specified - cannot handle message [" + result + "]");
                }
                return msg;
            }
        }

        /// <summary>
        /// Post-process the given response message before it will be sent. The default implementation
        /// sets the response's correlation id to the request message's correlation id.
        /// </summary>
        /// <param name="request">The original incoming message.</param>
        /// <param name="response">The outgoing Rabbit message about to be sent.</param>
        protected virtual void PostProcessResponse(Message request, Message response)
        {
            string correlation = Encoding.UTF8.GetString(request.MessageProperties.CorrelationId);            
            if (correlation.Length == 0)
            {
                correlation = request.MessageProperties.MessageId;
            }
            response.MessageProperties.CorrelationId = Encoding.UTF8.GetBytes(correlation);
        }

        /// <summary>
	    /// Determine a response destination for the given message.
	    /// </summary>
	    /// <remarks>
	    /// <para>The default implementation first checks the Rabbit ReplyTo
	    /// property of the supplied request; if that is not <code>null</code>
	    /// it is returned; if it is <code>null</code>, then the configured
	    /// <see cref="DefaultResponseRoutingKey"/> default response routing key}
	    /// is returned; if this too is <code>null</code>, then an
        /// <see cref="InvalidOperationException"/>is thrown.
	    /// </para>
        /// </remarks>
        /// <param name="request">The original incoming message.</param>
        /// <param name="response">Tthe outgoing message about to be sent.</param>
        /// <param name="channel">The channel to operate on.</param>
        /// <returns>the response destination (never <code>null</code>)</returns>
        /// <exception cref="InvalidOperationException">if no destination can be determined.</exception>
        protected virtual string GetResponseReplyTo(Message request, Message response, IModel channel)
        {
            string replyTo = request.MessageProperties.ReplyTo;
            if (replyTo == null )
            {
                replyTo = this.defaultResponseRoutingKey;
                if (replyTo == null)
                {
                    throw new InvalidOperationException("Cannot determine ReplyTo message property value: " +
						"Request message does not contain reply-to property, and no default ReplyTo value set.");
                }
            }
            return replyTo;
        }

        /// <summary>
        /// Sends the given response message to the given destination.
        /// </summary>
        /// <param name="channel">The channel to operate on.</param>
        /// <param name="replyTo">The replyto property to determine where to send a response.</param>
        /// <param name="message">The outgoing message about to be sent.</param>
        protected virtual void SendResponse(IModel channel, String replyTo, Message message)
        {
            //TODO need a strategy for extracdting exchange/routing key
            string exchange = MessageListenerAdapter.DEFAULT_RESPONSE_EXCHANGE;           
            string receivedExchange = message.MessageProperties.ReceivedExchange;
            if (receivedExchange != null)
            {
                logger.Debug("Overriding defaultResponseExchange and using exchange of received message.");
                exchange = receivedExchange;
            }
            PostProcessChannel(channel, message);

            //TODO parameterize out defalut encoding
            channel.BasicPublish(exchange, replyTo, 
                                this.mandatoryPublish, this.immediatePublish,
                                RabbitUtils.ExtractBasicProperties(channel, message),
                                message.Body);            
        }

        /// <summary>
        /// Post-process the given message producer before using it to send the response.
        /// The default implementation is empty.
        /// </summary>
        /// <param name="channel">The channel that will be used to send the message.</param>
        /// <param name="response">The outgoing message about to be sent.</param>
        protected virtual void PostProcessChannel(IModel channel, Message response)
        {
            
        }
    }

    /// <summary>
    /// Internal class combining a destination name and its target destination type (queue or topic).
    /// </summary>
    internal class DestinationNameHolder
    {
        private readonly string name;

        private readonly bool isTopic;

        public DestinationNameHolder(string name, bool isTopic)
        {
            this.name = name;
            this.isTopic = isTopic;
        }


        public string Name
        {
            get { return name; }
        }

        public bool IsTopic
        {
            get { return isTopic; }
        }
    }
}
