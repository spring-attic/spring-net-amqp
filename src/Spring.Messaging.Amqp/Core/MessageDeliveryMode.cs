
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

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    /// Enumeration for the Message Delivery Mode.  The None value is set in case the properties of the message 
    /// do not contain any delivery mode value.
    /// </summary>
    /// <author>Mark Pollack</author>
    public enum MessageDeliveryMode
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// Non-Persistent
        /// </summary>
        NON_PERSISTENT = 1,

        /// <summary>
        /// Persistent
        /// </summary>
        PERSISTENT = 2
    }

}