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

namespace Accord.Math.Comparers
{
    /// <summary>
    ///     Stable comparer for stable sorting algorithm.
    /// </summary>
    /// <typeparam name="T">The type of objects to compare.</typeparam>
    /// <remarks>
    ///     This class helps sort the elements of an array without swapping
    ///     elements which are already in order. This comprises a <c>stable</c>
    ///     sorting algorithm. This class is used by the <see cref="Tools.StableSort{T}(T[], out int[])" /> method to produce a
    ///     stable sort
    ///     of its given arguments.
    /// </remarks>
    /// <example>
    ///     In order to use this class, please use <see cref="Tools.StableSort{T}(T[], out int[])" />.
    /// </example>
    /// <seealso cref="ElementComparer{T}" />
    /// <seealso cref="ArrayComparer{T}" />
    /// <seealso cref="GeneralComparer" />
    /// <seealso cref="CustomComparer{T}" />
    public class StableComparer<T> : IComparer<KeyValuePair<int, T>>
    {
        private readonly Comparison<T> comparison;

        /// <summary>
        ///     Constructs a new instance of the <see cref="StableComparer&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="comparison">The comparison function.</param>
        public StableComparer(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }

        /// <summary>
        ///     Compares two objects and returns a value indicating
        ///     whether one is less than, equal to, or greater than
        ///     the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of x and y.</returns>
        public int Compare(KeyValuePair<int, T> x, KeyValuePair<int, T> y)
        {
            var result = comparison(x.Value, y.Value);
            return result != 0 ? result : x.Key.CompareTo(y.Key);
        }
    }
}