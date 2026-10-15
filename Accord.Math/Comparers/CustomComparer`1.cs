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
    ///     Custom comparer which accepts any delegate or
    ///     anonymous function to perform value comparisons.
    /// </summary>
    /// <typeparam name="T">The type of objects to compare.</typeparam>
    /// <example>
    ///     <code>
    ///   // Assume we have values to sort
    ///   double[] values = { 0, 5, 3, 1, 8 };
    ///   
    ///   // We can create an ad-hoc sorting rule using
    ///   Array.Sort(values, new CustomComparer&lt;double>((a, b) => -a.CompareTo(b)));
    ///   
    ///   // Result will be { 8, 5, 3, 1, 0 }.
    /// </code>
    /// </example>
    public class CustomComparer<T> : IComparer<T>, IEqualityComparer<T>
    {
        private readonly Func<T, T, int> comparer;

        /// <summary>
        ///     Constructs a new <see cref="CustomComparer&lt;T&gt;" />.
        /// </summary>
        /// <param name="comparer">The comparer function.</param>
        public CustomComparer(Func<T, T, int> comparer)
        {
            this.comparer = comparer;
        }

        /// <summary>
        ///     Compares two objects and returns a value indicating
        ///     whether one is less than, equal to, or greater than
        ///     the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of x and y.</returns>
        public int Compare(T x, T y)
        {
            return comparer(x, y);
        }

        /// <summary>
        ///     Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type T to compare.</param>
        /// <param name="y">The second object of type T to compare.</param>
        /// <returns>true if the specified objects are equal; otherwise, false.</returns>
        public bool Equals(T x, T y)
        {
            return comparer(x, y) == 0;
        }

        /// <summary>
        ///     Returns a hash code for the given object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        ///     A hash code for the given object, suitable for use in
        ///     hashing algorithms and data structures like a hash table.
        /// </returns>
        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }
}