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
    ///     Element-at-position comparer.
    /// </summary>
    /// <remarks>
    ///     This class compares arrays by checking the value
    ///     of a particular element at a given array index.
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // We sort the arrays according to the 
    ///   // elements at their second column.
    ///   
    ///   double[][] values =
    ///   {   //                 v
    ///       new double[] {  0, 3, 0 },
    ///       new double[] {  0, 4, 1 },
    ///       new double[] { -1, 1, 1 },
    ///       new double[] { -1, 5, 4 },
    ///       new double[] { -2, 2, 6 },
    ///   };
    ///   
    ///   // Sort the array considering only the second column
    ///   Array.Sort(values, new ElementComparer() { Index = 1 });
    ///   
    ///   // The result will be
    ///   double[][] result =
    ///   {
    ///       new double[] { -1, 1, 1 },
    ///       new double[] { -2, 2, 6 },
    ///       new double[] {  0, 3, 0 },
    ///       new double[] {  0, 4, 1 },
    ///       new double[] { -1, 5, 4 },
    ///   };
    /// </code>
    /// </example>
    /// <seealso cref="ElementComparer{T}" />
    /// <seealso cref="ArrayComparer{T}" />
    /// <seealso cref="GeneralComparer" />
    /// <seealso cref="CustomComparer{T}" />
    public class ElementComparer : ElementComparer<double>
    {
    }

    /// <summary>
    ///     Element-at-position comparer.
    /// </summary>
    /// <remarks>
    ///     This class compares arrays by checking the value
    ///     of a particular element at a given array index.
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // We sort the arrays according to the 
    ///   // elements at their second column.
    ///   
    ///   double[][] values =
    ///   {   //                 v
    ///       new double[] {  0, 3, 0 },
    ///       new double[] {  0, 4, 1 },
    ///       new double[] { -1, 1, 1 },
    ///       new double[] { -1, 5, 4 },
    ///       new double[] { -2, 2, 6 },
    ///   };
    ///   
    ///   // Sort the array considering only the second column
    ///   Array.Sort(values, new ElementComparer() { Index = 1 });
    ///   
    ///   // The result will be
    ///   double[][] result =
    ///   {
    ///       new double[] { -1, 1, 1 },
    ///       new double[] { -2, 2, 6 },
    ///       new double[] {  0, 3, 0 },
    ///       new double[] {  0, 4, 1 },
    ///       new double[] { -1, 5, 4 },
    ///   };
    /// </code>
    /// </example>
    /// <seealso cref="ElementComparer" />
    /// <seealso cref="ArrayComparer{T}" />
    /// <seealso cref="GeneralComparer" />
    /// <seealso cref="CustomComparer{T}" />
    public class ElementComparer<T> : IComparer<T[]>, IEqualityComparer<T[]>
        where T : IComparable, IEquatable<T>
    {
        /// <summary>
        ///     Gets or sets the element index to compare.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        ///     Compares two objects and returns a value indicating
        ///     whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        public int Compare(T[] x, T[] y)
        {
            return Comparer<T>.Default.Compare(x[Index], y[Index]);
        }

        /// <summary>
        ///     Determines whether two instances are equal.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>
        ///     <c>true</c> if the specified object is equal to the other; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(T[] x, T[] y)
        {
            return EqualityComparer<T>.Default.Equals(x[Index], y[Index]);
        }

        /// <summary>
        ///     Returns a hash code for a given instance.
        /// </summary>
        /// <param name="obj">The instance.</param>
        /// <returns>
        ///     A hash code for the instance, suitable for use
        ///     in hashing algorithms and data structures like a hash table.
        /// </returns>
        public int GetHashCode(T[] obj)
        {
            return obj[Index].GetHashCode();
        }
    }
}