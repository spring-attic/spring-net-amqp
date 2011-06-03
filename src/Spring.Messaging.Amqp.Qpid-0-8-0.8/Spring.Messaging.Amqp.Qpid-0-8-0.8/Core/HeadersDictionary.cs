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
using System.Collections.Generic;
using System.Reflection;
using Apache.Qpid.Framing;
using Apache.Qpid.Messaging;

namespace Spring.Messaging.Amqp.Qpid.Core
{
    /// <summary>
    ///  
    /// </summary>
    /// <author>Mark Pollack</author>
    public class HeadersDictionary : IDictionary<string, object>, IHeaders
    {

        private IHeaders headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public HeadersDictionary(IHeaders headers)
        {
            this.headers = headers;
        }

        #region Implementation of IEnumerable

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotSupportedException("Not supported use in QPID.");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion


        #region Implementation of ICollection<KeyValuePair<string,object>>

        public void Add(KeyValuePair<string, object> item)
        {
            headers[item.Key] = item.Value;
        }

        public void Clear()
        {
            throw new NotSupportedException("Not supported use in QPID.");
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return headers.Contains(item.Key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotSupportedException("Not supported use in QPID.");
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            throw new NotSupportedException("Not supported use in QPID.");
        }

        public int Count
        {
            get { throw new NotSupportedException("Not supported use in QPID."); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region Implementation of IDictionary<string,object>

        public bool ContainsKey(string key)
        {
            return headers.Contains(key);
        }

        public void Add(string key, object value)
        {
            headers[key] = value;
        }

        public bool Remove(string key)
        {
            //TODO check if this is valid
            headers[key] = null;
            return true;
        }

        public bool TryGetValue(string key, out object value)
        {
            throw new NotSupportedException("Not supported use in QPID.");
        }

        public bool Contains(string name)
        {
            return headers.Contains(name);
        }

        public bool GetBoolean(string name)
        {
            return headers.GetBoolean(name);
        }

        public void SetBoolean(string name, bool value)
        {
            headers.SetBoolean(name, value);
        }

        public byte GetByte(string name)
        {
            return headers.GetByte(name);
        }

        public void SetByte(string name, byte value)
        {
            headers.SetByte(name, value);
        }

        public short GetShort(string name)
        {
            return headers.GetShort(name);
        }

        public void SetShort(string name, short value)
        {
            headers.SetShort(name, value);
        }

        public int GetInt(string name)
        {
            return headers.GetInt(name);
        }

        public void SetInt(string name, int value)
        {
            headers.SetInt(name, value);
        }

        public long GetLong(string name)
        {
            return headers.GetLong(name);
        }

        public void SetLong(string name, long value)
        {
            headers.SetLong(name, value);
        }

        public float GetFloat(string name)
        {
            return headers.GetFloat(name);
        }

        public void SetFloat(string name, float value)
        {
            headers.SetFloat(name, value);
        }

        public double GetDouble(string name)
        {
            return headers.GetDouble(name);
        }

        public void SetDouble(string name, double value)
        {
            headers.SetDouble(name, value);
        }

        public string GetString(string name)
        {
            return headers.GetString(name);
        }

        public void SetString(string name, string value)
        {
            headers.SetString(name, value);
        }

        object IHeaders.this[string name]
        {
            get { return headers[name]; }
            set { headers[name] = value; }
        }

        public object this[string key]
        {
            get { return headers[key]; }
            set { headers[key] = value; }
        }

        public ICollection<string> Keys
        {
            get { throw new NotSupportedException("Not supported use in QPID."); }
        }

        public ICollection<object> Values
        {
            get { throw new NotSupportedException("Not supported use in QPID."); }
        }

        #endregion

        public IHeaders Headers
        {
            get { return headers; }
        }
    }
}