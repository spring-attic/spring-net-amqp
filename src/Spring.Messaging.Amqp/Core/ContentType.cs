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
    /// A simple class to hold content type values.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald</author>
    public class ContentType
    {
        public static readonly string CONTENT_TYPE_BYTES = "application/octet-stream";
        public static readonly string CONTENT_TYPE_TEXT_PLAIN = "text/plain";
        public static readonly string CONTENT_TYPE_SERIALIZED_OBJECT = "application/x-dotnet-serialized-object";
        public static readonly string CONTENT_TYPE_JSON = "application/json";
    }

}