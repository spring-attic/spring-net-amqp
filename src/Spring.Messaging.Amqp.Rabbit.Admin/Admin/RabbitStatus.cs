// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitStatus.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System.Collections.Generic;
using Spring.Erlang.Core;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>
    ///  The status object returned from querying the broker
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class RabbitStatus
    {
        /// <summary>
        /// The running applications.
        /// </summary>
        private readonly IList<Application> runningApplications;

        /// <summary>
        /// The nodes.
        /// </summary>
        private IList<Node> nodes;

        /// <summary>
        /// The running nodes.
        /// </summary>
        private IList<Node> runningNodes;

        /// <summary>Initializes a new instance of the <see cref="RabbitStatus"/> class.</summary>
        /// <param name="runningApplications">The running applications.</param>
        /// <param name="nodes">The nodes.</param>
        /// <param name="runningNodes">The running nodes.</param>
        public RabbitStatus(IList<Application> runningApplications, IList<Node> nodes, IList<Node> runningNodes)
        {
            this.runningApplications = runningApplications;
            this.nodes = nodes;
            this.runningNodes = runningNodes;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is alive.
        /// </summary>
        public bool IsAlive { get { return this.nodes != null && !(this.nodes.Count <= 0); } }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        public bool IsRunning { get { return this.runningNodes != null && !(this.runningNodes.Count <= 0); } }

        /// <summary>
        /// Gets a value indicating whether this instance is ready.
        /// </summary>
        public bool IsReady
        {
            get
            {
                var erlangNodeIsRunning = this.IsRunning && this.runningApplications != null && !(this.runningApplications.Count <= 0);
                if (!erlangNodeIsRunning)
                {
                    return false;
                }

                var rabbitIsRunning = false;
                foreach (var application in this.runningApplications)
                {
                    if (application.Id == "\"RabbitMQ\"")
                    {
                        rabbitIsRunning = true;
                    }
                }

                return rabbitIsRunning;
            }
        }

        /// <summary>
        /// Gets the running applications.
        /// </summary>
        public IList<Application> RunningApplications { get { return this.runningApplications; } }

        /// <summary>
        /// Gets the nodes.
        /// </summary>
        public IList<Node> Nodes { get { return this.nodes; } set { this.nodes = value; } }

        /// <summary>
        /// Gets the running nodes.
        /// </summary>
        public IList<Node> RunningNodes { get { return this.runningNodes; } set { this.runningNodes = value; } }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString() { return string.Format("IsAlive: {0}, IsRunning: {1}, IsReady: {2}, RunningApplications: {3}, Nodes: {4}, RunningNodes: {5}", this.IsAlive, this.IsRunning, this.IsReady, this.runningApplications, this.nodes, this.runningNodes); }
    }
}
