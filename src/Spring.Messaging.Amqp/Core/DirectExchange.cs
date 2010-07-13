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
    /// Simple container collecting information to describe a direct exchange.
    /// </summary>
    /// <remarks>
    /// Used in conjunction with administrative operations.
    /// </remarks>
    /// <author>Mark Pollack</author>
    /// <see cref="IAmqpAdmin"/>
    public class DirectExchange : AbstractExchange
    {
        public DirectExchange(string name) : base(name)
        {
        }

        public DirectExchange(string name, bool durable, bool autoDelete) : base(name, durable, autoDelete)
        {
        }

        public override ExchangeType ExchangeType
        {
            get { return Core.ExchangeType.Direct; }
        }
    }

}