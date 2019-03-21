// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeConnectionListener.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System.Collections.Generic;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// A composite connection listener.
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class CompositeConnectionListener : IConnectionListener
    {
        /// <summary>
        /// The delegates.
        /// </summary>
        private IList<IConnectionListener> delegates = new List<IConnectionListener>();

        /// <summary>
        /// Sets the delegates.
        /// </summary>
        /// <value>
        /// The delegates.
        /// </value>
        public IList<IConnectionListener> Delegates { set { this.delegates = value; } }

        /// <summary>Adds the delegate.</summary>
        /// <param name="connectionListener">The connection listener.</param>
        public void AddDelegate(IConnectionListener connectionListener) { this.delegates.Add(connectionListener); }

        /// <summary>Action to perform on create.</summary>
        /// <param name="connection">The connection.</param>
        public void OnCreate(IConnection connection)
        {
            foreach (var theDelegate in this.delegates)
            {
                theDelegate.OnCreate(connection);
            }
        }

        /// <summary>Action to perform on close.</summary>
        /// <param name="connection">The connection.</param>
        public void OnClose(IConnection connection)
        {
            foreach (var theDelegate in this.delegates)
            {
                theDelegate.OnClose(connection);
            }
        }
    }
}
