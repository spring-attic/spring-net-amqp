// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AcknowledgeMode.cs" company="The original author or authors.">
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

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Acknowledge Mode Utilities
    /// </summary>
    /// <author>Dave Syer</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public static class AcknowledgeModeUtils
    {
        /// <summary>
        /// Enumeration of Acknowledge Modes
        /// </summary>
        /// <author>Joe Fitzgerald (.NET)</author>
        public enum AcknowledgeMode
        {
            ///<summary>
            /// None AcknowledgeMode
            ///</summary>
            None, 

            /// <summary>
            /// None Acnowledge Mode
            /// </summary>
            Manual, 

            /// <summary>
            /// Manual Acnowledge Mode
            /// </summary>, 
            Auto
        }

        /// <summary>Determine if the acknowledge mode is transaction allowed.</summary>
        /// <param name="mode">The mode.</param>
        /// <returns>True if transaction allowed, else false.</returns>
        public static bool TransactionAllowed(this AcknowledgeMode mode) { return mode == AcknowledgeMode.Auto || mode == AcknowledgeMode.Manual; }

        /// <summary>Determine if the acknowledge mode is auto ack.</summary>
        /// <param name="mode">The mode.</param>
        /// <returns>True if auto ack, else false.</returns>
        public static bool IsAutoAck(this AcknowledgeMode mode) { return mode == AcknowledgeMode.None; }

        /// <summary>Determine if the acknowledge mode is manual.</summary>
        /// <param name="mode">The mode.</param>
        /// <returns>True if manual, else false.</returns>
        public static bool IsManual(this AcknowledgeMode mode) { return mode == AcknowledgeMode.Manual; }
    }
}
