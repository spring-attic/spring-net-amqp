// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMessagePropertiesConverter.cs" company="The original author or authors.">
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
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// Strategy interface for converting between Spring AMQP {@link MessageProperties}
    /// and RabbitMQ BasicProperties.
    /// </summary>
    /// <author>Mark Fisher</author>
    /// <author>Joe Fitzgerald</author>
    public interface IMessagePropertiesConverter
    {
        /// <summary>Toes the message properties.</summary>
        /// <param name="source">The source.</param>
        /// <param name="envelope">The envelope.</param>
        /// <param name="charset">The charset.</param>
        /// <returns>The message properties.</returns>
        MessageProperties ToMessageProperties(IBasicProperties source, BasicGetResult envelope, string charset);

        /// <summary>Froms the message properties.</summary>
        /// <param name="channel">The channel.</param>
        /// <param name="source">The source.</param>
        /// <param name="charset">The charset.</param>
        /// <returns>The basic properties.</returns>
        IBasicProperties FromMessageProperties(IModel channel, MessageProperties source, string charset);
    }
}
