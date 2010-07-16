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

#region

using System.Collections;
using System.Collections.Generic;

#endregion

namespace Spring.Messaging.Amqp.Core
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public interface IMessageProperties
    {
        string AppId { get; set; }

        string ClusterId { get; set; }

        string ContentEncoding { get; set; }

        long ContentLength { get; set; }

        string ContentType { get; set; }

        //TODO use byte[] as property type instead?
        byte[] CorrelationId { get; set; }

        //byte? DeliveryMode { get; set; }
        MessageDeliveryMode DeliveryMode { get; set; }

        ulong DeliveryTag { get; }


        string Expiration { get; set; }
        
        IDictionary<string, object> Headers { get; set; }

        void SetHeader(string key, object val);

        object GetHeader(string key);

        uint MessageCount { get; }

        string MessageId { get; set; }

        int Priority { get; set; }

        string ReceivedExchange { get; }

        string ReceivedRoutingKey { get; }

        bool Redelivered { get; }

        //TODO consider ReplyToAddress property type (qpid uses it... hiding deeper in rabbit)

        string ReplyTo { get; set; }



        long Timestamp { get; set; }

        string Type { get; set; }

        string UserId { get; set; }
    }
}