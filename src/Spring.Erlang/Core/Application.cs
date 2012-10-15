// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Application.cs" company="The original author or authors.">
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
    /// Describes an Erlang application.  Only three fields are supported as that is the level
    /// of information that rabbitmq returns when performing a status request.
    /// </summary>
    /// <remarks>
    /// See http://www.erlang.org/doc/man/app.html for full details
    /// </remarks>
    /// <author>Mark Pollack</author>
    public class Application
    {
        /// <summary>
        /// The description.
        /// </summary>
        private readonly string description;

        /// <summary>
        /// The id.
        /// </summary>
        private readonly string id;

        /// <summary>
        /// The version.
        /// </summary>
        private readonly string version;

        /// <summary>Initializes a new instance of the <see cref="Application"/> class.</summary>
        /// <param name="description">The description.</param>
        /// <param name="id">The id.</param>
        /// <param name="version">The version.</param>
        public Application(string description, string id, string version)
        {
            this.description = description;
            this.id = id;
            this.version = version;
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        public string Description { get { return this.description; } }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public string Id { get { return this.id; } }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public string Version { get { return this.version; } }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString() { return string.Format("Description: {0}, Id: {1}, Version: {2}", this.description, this.id, this.version); }
    }
}
