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
using Accord.Math.Comparers;
using Accord.Math.Random;

namespace Accord.Math
{
    /// <summary>
    ///     Static class Vector. Defines a set of extension methods
    ///     that operates mainly on single-dimensional arrays.
    /// </summary>
    /// <seealso cref="Jagged" />
    /// <seealso cref="Vector" />
    public static partial class Vector
    {
        /// <summary>
        ///     Shuffles an array.
        /// </summary>
        public static void Shuffle<T>(this T[] array)
        {
            var random = Generator.Random;

            // i is the number of items remaining to be shuffled.
            for (var i = array.Length; i > 1; i--)
            {
                // Pick a random element to swap with the i-th element.
                var j = random.Next(i);

                // Swap array elements.
                var aux = array[j];
                array[j] = array[i - 1];
                array[i - 1] = aux;
            }
        }

        /// <summary>
        ///     Shuffles a collection.
        /// </summary>
        public static void Shuffle<T>(this IList<T> array)
        {
            var random = Generator.Random;

            // i is the number of items remaining to be shuffled.
            for (var i = array.Count; i > 1; i--)
            {
                // Pick a random element to swap with the i-th element.
                var j = random.Next(i);

                // Swap array elements.
                var aux = array[j];
                array[j] = array[i - 1];
                array[i - 1] = aux;
            }
        }

        /// <summary>
        ///     Sorts the elements of an entire one-dimensional array using the given comparison.
        /// </summary>
        public static void Sort<T>(this T[] values, Comparison<T> comparison, bool stable = false, bool asc = true)
        {
            if (!stable)
            {
                Array.Sort(values, comparison);
            }
            else
            {
                var keys = new KeyValuePair<int, T>[values.Length];
                for (var i = 0; i < values.Length; i++)
                {
                    keys[i] = new KeyValuePair<int, T>(i, values[i]);
                }

                Array.Sort(keys, values, new StableComparer<T>(comparison));
            }

            if (!asc)
            {
                Array.Reverse(values);
            }
        }

        /// <summary>
        ///     Sorts the elements of an entire one-dimensional array using the given comparison.
        /// </summary>
        public static void Sort<T>(this T[] values, bool stable = false, bool asc = true)
            where T : IComparable<T>
        {
            if (!stable)
            {
                Array.Sort(values);
            }
            else
            {
                var keys = new KeyValuePair<int, T>[values.Length];
                for (var i = 0; i < values.Length; i++)
                {
                    keys[i] = new KeyValuePair<int, T>(i, values[i]);
                }

                Array.Sort(keys, values, new StableComparer<T>((a, b) => a.CompareTo(b)));
            }

            if (!asc)
            {
                Array.Reverse(values);
            }
        }

        /// <summary>
        ///     Sorts the elements of an entire one-dimensional array using the given comparison.
        /// </summary>
        public static void Sort<T>(this T[] values, out int[] order, bool stable = false,
            ComparerDirection direction = ComparerDirection.Ascending)
            where T : IComparable<T>
        {
            if (!stable)
            {
                order = Range(values.Length);
                Array.Sort(values, order);

                if (direction == ComparerDirection.Descending)
                {
                    Array.Reverse(values);
                    Array.Reverse(order);
                }
            }
            else
            {
                var keys = new KeyValuePair<int, T>[values.Length];
                for (var i = 0; i < values.Length; i++)
                {
                    keys[i] = new KeyValuePair<int, T>(i, values[i]);
                }

                if (direction == ComparerDirection.Ascending)
                {
                    Array.Sort(keys, values, new StableComparer<T>((a, b) => a.CompareTo(b)));
                }
                else
                {
                    Array.Sort(keys, values, new StableComparer<T>((a, b) => -a.CompareTo(b)));
                }

                order = new int[values.Length];
                for (var i = 0; i < keys.Length; i++)
                {
                    order[i] = keys[i].Key;
                }
            }
        }

        /// <summary>
        ///     Shuffles an array.
        /// </summary>
        public static T[] Shuffled<T>(this T[] array)
        {
            var clone = (T[])array.Clone();
            Shuffle(clone);
            return clone;
        }

        /// <summary>
        ///     Shuffles a collection.
        /// </summary>
        public static TList Shuffled<TList, T>(this TList array)
            where TList :
#if !NETSTANDARD1_4
            ICloneable,
#endif
            IList<T>
        {
            var clone = (TList)array.Clone();
            Shuffle(clone);
            return clone;
        }

        /// <summary>
        ///     Sorts the elements of an entire one-dimensional array using the given comparison.
        /// </summary>
        public static T[] Sorted<T>(this T[] values, Comparison<T> comparison, bool stable = false)
        {
            var clone = (T[])values.Clone();
            Sort(clone, comparison, stable);
            return clone;
        }

        /// <summary>
        ///     Sorts the elements of an entire one-dimensional array using the given comparison.
        /// </summary>
        public static T[] Sorted<T>(this T[] values, bool stable = false, bool asc = true)
            where T : IComparable<T>
        {
            var clone = (T[])values.Clone();
            Sort(clone, stable, asc);
            return clone;
        }

        /// <summary>
        ///     Sorts the elements of an entire one-dimensional array using the given comparison.
        /// </summary>
        public static T[] Sorted<T>(this ICollection<T> values, bool stable = false)
            where T : IComparable<T>
        {
            var clone = new T[values.Count];
            values.CopyTo(clone, 0);
            Sort(clone, stable);
            return clone;
        }

        /// <summary>
        ///     Sorts the elements of an entire one-dimensional array using the given comparison.
        /// </summary>
        public static T[] Sorted<T>(this T[] values, out int[] order, bool stable = false,
            ComparerDirection direction = ComparerDirection.Ascending)
            where T : IComparable<T>
        {
            var clone = (T[])values.Clone();
            Sort(clone, out order, stable, direction);
            return clone;
        }
    }
}