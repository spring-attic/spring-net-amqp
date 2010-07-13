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
        private string description;

        private string id;

        private string version;

        public Application(string description, string id, string version)
        {
            this.description = description;
            this.id = id;
            this.version = version;
        }

        public string Description
        {
            get { return description; }
        }

        public string Id
        {
            get { return id; }
        }

        public string Version
        {
            get { return version; }
        }

        public override string ToString()
        {
            return string.Format("Description: {0}, Id: {1}, Version: {2}", description, id, version);
        }
    }

}