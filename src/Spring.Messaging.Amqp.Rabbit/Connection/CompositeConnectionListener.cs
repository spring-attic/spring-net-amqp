
using System.Collections.Generic;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A composite connection listener.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald</author>
    public class CompositeConnectionListener : IConnectionListener 
    {
        /// <summary>
        /// The delegates.
        /// </summary>
        private IList<IConnectionListener> delegates = new List<IConnectionListener>();

        /// <summary>
        /// Gets or sets Delegates.
        /// </summary>
        public IList<IConnectionListener> Delegates
        {
            get { return this.delegates; }
            set { this.delegates = value; }
        }

        /// <summary>
        /// Action to perform on create.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        public void OnCreate(IConnection connection)
        {
            foreach (var theDelegate in this.delegates)
	        {
                theDelegate.OnCreate(connection);
            }
	    }

        /// <summary>
        /// Action to perform on close.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        public void OnClose(IConnection connection)
        {
            foreach (var theDelegate in this.delegates)
            {
                theDelegate.OnClose(connection);
            }
	    }
    }
}
