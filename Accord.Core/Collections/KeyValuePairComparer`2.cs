// Accord.NET Framework (originally)
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Relicensed to MIT per the author's own explicit grant made when the
// upstream project was archived (2020-11-19):
// https://github.com/accord-net/framework
// "I am also willing to change the license of any file where I am the
// single author... to MIT so people can reuse individual pieces of code
// more easily." -- Cesar De Souza
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;

namespace Accord.Collections
{
    /// <summary>
    ///     Vanilla key-based comparer for <see cref="KeyValuePair{TKey,TValue}" />.
    /// </summary>
    /// <typeparam name="TKey">The key type in the key-value pair.</typeparam>
    /// <typeparam name="TValue">The value type in the key-value pair.</typeparam>
    [Serializable]
    public class KeyValuePairComparer<TKey, TValue>
        : Comparer<KeyValuePair<TKey, TValue>>, IComparer<TKey>
    {
        private readonly IComparer<TKey> keyComparer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KeyValuePairComparer&lt;TKey, TValue&gt;" /> class.
        /// </summary>
        /// <param name="keyComparer">The comparer to be used to compare keys.</param>
        public KeyValuePairComparer(IComparer<TKey> keyComparer)
        {
            if (keyComparer == null)
            {
                throw new ArgumentNullException("keyComparer");
            }

            this.keyComparer = keyComparer;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KeyValuePairComparer&lt;TKey, TValue&gt;" /> class.
        /// </summary>
        public KeyValuePairComparer()
        {
            keyComparer = Comparer<TKey>.Default;
        }

        /// <summary>
        ///     Returns a default sort order comparer for the
        ///     key-value pair specified by the generic argument.
        /// </summary>
        public new static KeyValuePairComparer<TKey, TValue> Default => new();

        /// <summary>
        ///     Compares two objects and returns a value indicating whether
        ///     one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        public int Compare(TKey x, TKey y)
        {
            return keyComparer.Compare(x, y);
        }

        /// <summary>
        ///     Compares two objects and returns a value indicating whether
        ///     one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        public override int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
        {
            return keyComparer.Compare(x.Key, y.Key);
        }
    }
}