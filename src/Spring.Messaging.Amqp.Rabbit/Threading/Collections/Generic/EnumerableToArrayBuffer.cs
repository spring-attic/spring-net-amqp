// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnumerableToArrayBuffer.cs" company="The original author or authors.">
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
using System;
using System.Collections.Generic;
using Spring.Collections.Generic;
#endregion

namespace Spring.Threading.Collections.Generic
{
    internal struct EnumerableToArrayBuffer<T>
    {
        private readonly T[] _items;
        private readonly int _count;
        private readonly ICollection<T> _collection;

        internal int Count { get { return this._collection == null ? this._count : this._collection.Count; } }

        internal EnumerableToArrayBuffer(IEnumerable<T> source)
        {
            T[] array = null;
            int length = 0;
            this._collection = source as ICollection<T>;
            if (this._collection != null)
            {
                this._items = null;
                this._count = 0;
                return;
            }

            foreach (var local in source)
            {
                if (array == null)
                {
                    array = new T[4];
                }
                else if (array.Length == length)
                {
                    var destinationArray = new T[length * 2];
                    Array.Copy(array, 0, destinationArray, 0, length);
                    array = destinationArray;
                }

                array[length] = local;
                length++;
            }

            this._items = array;
            this._count = length;
        }

        internal T[] ToArray()
        {
            var count = this.Count;
            if (count == 0)
            {
                return new T[0];
            }

            T[] destinationArray;
            if (this._collection == null)
            {
                if (this._items.Length == this._count)
                {
                    return this._items;
                }

                destinationArray = new T[this._count];
                Array.Copy(this._items, 0, destinationArray, 0, this._count);
                return destinationArray;
            }

            var list = this._collection as List<T>;
            if (list != null)
            {
                return list.ToArray();
            }

            var ac = this._collection as AbstractCollection<T>;
            if (ac != null)
            {
                return ac.ToArray();
            }

            destinationArray = new T[count];
            this._collection.CopyTo(destinationArray, 0);
            return destinationArray;
        }

        /// <summary>Caller to guarantee items.Length &gt; index &gt;= 0</summary>
        /// <param name="items">The items.</param>
        /// <param name="index">The index.</param>
        internal void CopyTo(T[] items, int index)
        {
            if (this._collection != null && this._collection.Count > 0)
            {
                this._collection.CopyTo(items, index);
            }
            else if (this._count > 0)
            {
                Array.Copy(this._items, 0, items, index, this._count);
            }
        }
    }
}
