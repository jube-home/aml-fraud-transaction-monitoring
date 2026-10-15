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
    ///     Directions for the General Comparer.
    /// </summary>
    public enum ComparerDirection
    {
        /// <summary>
        ///     Sorting will be performed in ascending order.
        /// </summary>
        Ascending = +1,

        /// <summary>
        ///     Sorting will be performed in descending order.
        /// </summary>
        Descending = -1
    }

    /// <summary>
    ///     General comparer which supports multiple
    ///     directions and comparison of absolute values.
    /// </summary>
    /// <example>
    ///     <code>
    ///   // Assume we have values to sort
    ///   double[] values = { 0, -5, 3, 1, 8 };
    ///   
    ///   // We can create an ad-hoc sorting rule considering only absolute values
    ///   Array.Sort(values, new GeneralComparer(ComparerDirection.Ascending, Math.Abs));
    ///   
    ///   // Result will be { 0, 1, 3, 5, 8 }.
    /// </code>
    /// </example>
    /// <seealso cref="ElementComparer{T}" />
    /// <seealso cref="ArrayComparer{T}" />
    /// <seealso cref="GeneralComparer" />
    /// <seealso cref="CustomComparer{T}" />
    public class GeneralComparer : IComparer<double>, IComparer<int>
    {
        private readonly Func<double, double> map;
        private int direction = 1;

        /// <summary>
        ///     Constructs a new General Comparer.
        /// </summary>
        /// <param name="direction">The direction to compare.</param>
        public GeneralComparer(ComparerDirection direction)
            : this(direction, false)
        {
        }

        /// <summary>
        ///     Constructs a new General Comparer.
        /// </summary>
        /// <param name="direction">The direction to compare.</param>
        /// <param name="useAbsoluteValues">True to compare absolute values, false otherwise. Default is false.</param>
        public GeneralComparer(ComparerDirection direction, bool useAbsoluteValues)
        {
            if (useAbsoluteValues)
            {
                map = System.Math.Abs;
            }
            else
            {
                map = a => a;
            }

            this.direction = (int)direction;
        }

        /// <summary>
        ///     Constructs a new General Comparer.
        /// </summary>
        /// <param name="direction">The direction to compare.</param>
        /// <param name="map">
        ///     The mapping function which will be applied to
        ///     each vector element prior to any comparisons.
        /// </param>
        public GeneralComparer(ComparerDirection direction, Func<double, double> map)
        {
            this.map = map;
            this.direction = (int)direction;
        }

        /// <summary>
        ///     Gets or sets the sorting direction
        ///     used by this comparer.
        /// </summary>
        public ComparerDirection Direction
        {
            get => (ComparerDirection)direction;
            set => direction = (int)value;
        }

        /// <summary>
        ///     Compares two objects and returns a value indicating whether one is less than,
        ///     equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        public int Compare(double x, double y)
        {
            return direction * map(x).CompareTo(map(y));
        }

        /// <summary>
        ///     Compares two objects and returns a value indicating whether one is less than,
        ///     equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        public int Compare(int x, int y)
        {
            return direction * map(x).CompareTo(map(y));
        }
    }

    /// <summary>
    ///     General comparer which supports multiple sorting directions.
    /// </summary>
    /// <example>
    ///     <code>
    ///   // Assume we have values to sort
    ///   double[] values = { 0, -5, 3, 1, 8 };
    ///   
    ///   // We can create an ad-hoc sorting rule
    ///   Array.Sort(values, new GeneralComparer&lt;double>(ComparerDirection.Descending));
    ///   
    ///   // Result will be { 8, 5, 3, 1, 0 }.
    /// </code>
    /// </example>
    /// <seealso cref="ElementComparer{T}" />
    /// <seealso cref="ArrayComparer{T}" />
    /// <seealso cref="GeneralComparer" />
    /// <seealso cref="CustomComparer{T}" />
    public class GeneralComparer<T> : IComparer<T> where T : IComparable<T>
    {
        private int direction = 1;

        /// <summary>
        ///     Constructs a new General Comparer.
        /// </summary>
        /// <param name="direction">The direction to compare.</param>
        public GeneralComparer(ComparerDirection direction)
        {
            this.direction = (int)direction;
        }

        /// <summary>
        ///     Gets or sets the sorting direction
        ///     used by this comparer.
        /// </summary>
        public ComparerDirection Direction
        {
            get => (ComparerDirection)direction;
            set => direction = (int)value;
        }

        /// <summary>
        ///     Compares two objects and returns a value indicating whether one is less than,
        ///     equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        public int Compare(T x, T y)
        {
            return direction * Comparer<T>.Default.Compare(x, y);
        }
    }
}