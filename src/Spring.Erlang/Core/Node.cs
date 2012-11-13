// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Node.cs" company="The original author or authors.">
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

namespace Spring.Erlang.Core
{
    /// <summary>
    /// Simple description class for an Erlang node. 
    /// </summary>
    /// <author>Mark Pollack</author>
    public class Node
    {
        /// <summary>
        /// The name.
        /// </summary>
        private readonly string name;

        /// <summary>Initializes a new instance of the <see cref="Node"/> class.</summary>
        /// <param name="name">The name.</param>
        public Node(string name) { this.name = name; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get { return this.name; } }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString() { return string.Format("Name: {0}", this.name); }
    }
}
