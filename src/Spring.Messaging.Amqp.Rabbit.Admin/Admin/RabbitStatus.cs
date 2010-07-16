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

using System.Collections.Generic;
using Spring.Erlang.Core;

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>
    ///  The status object returned from querying the broker
    /// </summary>
    /// <author>Mark Pollack</author>
    public class RabbitStatus
    {
        private IList<Application> runningApplications;

        private IList<Node> nodes;

        private IList<Node> runningNodes;

        public RabbitStatus(IList<Application> runningApplications, IList<Node> nodes, IList<Node> runningNodes)
        {
            this.runningApplications = runningApplications;
            this.nodes = nodes;
            this.runningNodes = runningNodes;
        }

        public IList<Application> RunningApplications
        {
            get { return runningApplications; }
        }

        public IList<Node> Nodes
        {
            get { return nodes; }
        }

        public IList<Node> RunningNodes
        {
            get { return runningNodes; }
        }

        public override string ToString()
        {
            return string.Format("RunningApplications: {0}, Nodes: {1}, RunningNodes: {2}", runningApplications, nodes, runningNodes);
        }
    }

}