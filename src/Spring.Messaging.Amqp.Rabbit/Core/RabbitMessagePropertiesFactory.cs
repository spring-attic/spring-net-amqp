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

using System;
using RabbitMQ.Client;
using Spring.Messaging.Amqp.Core;

namespace Spring.Messaging.Amqp.Rabbit.Core
{
    /// <summary>
    /// Simple implementation of <see cref="IMessagePropertiesFactory"/> interface.
    /// </summary>
    /// <author>Mark Pollack</author>
    public class RabbitMessagePropertiesFactory : IMessagePropertiesFactory
    {
        private IModel model;

        public RabbitMessagePropertiesFactory(IModel model)
        {
            if (model == null)
            {
                throw new InvalidOperationException("Must set Model property value calling create");
            }
            this.model = model;
        }

        public IMessageProperties Create()
        {            
            return new MessageProperties(model.CreateBasicProperties());
        }
    }

}