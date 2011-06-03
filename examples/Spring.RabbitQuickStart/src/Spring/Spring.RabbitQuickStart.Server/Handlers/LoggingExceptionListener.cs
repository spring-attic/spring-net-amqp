

using System;
using Common.Logging;

namespace Spring.RabbitQuickStart.Server.Handlers
{
    public class LoggingExceptionListener // TODO what is the equivalent in Rabbit? : IExceptionListener
    {
        #region Logging

        private readonly ILog logger = LogManager.GetLogger(typeof(LoggingExceptionListener));

        #endregion

        /// <summary>
        /// Called when there is an exception in message processing.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void OnException(Exception exception)
        {
            logger.Info("********* Caught exception *************", exception);
        }
    }
}