// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IResourceHolder.cs" company="The original author or authors.">
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

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    /// Generic interface to be implemented by resource holders. Allows Spring's transaction infrastructure to introspect and reset the holder when necessary.
    /// </summary>
    /// <author>Juergen Hoeller</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public interface IResourceHolder
    {
        /// <summary>Reset the transactional state of this holder.</summary>
        void Reset();

        /// <summary>Notify this holder that it has been unbound from transaction synchronization.</summary>
        void Unbound();

        /// <summary>Determine whether this holder is considered as 'void', i.e. as a leftover from a previous thread.</summary>
        /// <returns>True if void, else False.</returns>
        bool IsVoid();
    }
}
