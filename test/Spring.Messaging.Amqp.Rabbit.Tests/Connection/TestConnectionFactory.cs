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
using System.Collections;
using RabbitMQ.Client;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class TestConnectionFactory : ConnectionFactory
    {
        private int createConnectionCount;

        protected override IConnection CreateConnection(int maxRedirects, IDictionary connectionAttempts, IDictionary connectionErrors, params AmqpTcpEndpoint[] endpoints)
        {
            createConnectionCount++;
            return new TestConnection();
        }

        public override IConnection CreateConnection(int maxRedirects, params AmqpTcpEndpoint[] endpoints)
        {
            createConnectionCount++;
            return new TestConnection();
        }

        public override IConnection CreateConnection(params AmqpTcpEndpoint[] endpoints)
        {
            createConnectionCount++;
            return new TestConnection();
        }

        public int CreateConnectionCount
        {
            get { return createConnectionCount; }
        }
    }

}