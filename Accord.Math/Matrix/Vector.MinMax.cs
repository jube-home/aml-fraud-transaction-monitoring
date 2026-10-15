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

namespace Accord.Math
{
    public static partial class Matrix
    {
        /// <summary>
        ///     Gets the indices that sort a vector.
        /// </summary>
        public static int[] ArgSort<T>(this T[] values)
            where T : IComparable<T>
        {
            int[] idx;
            values.Copy().Sort(out idx);
            return idx;
        }

        #region Vector ArgMin/ArgMax

        /// <summary>
        ///     Gets the maximum element in a vector.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ArgMax<T>(this T[] values)
            where T : IComparable<T>
        {
            var imax = 0;
            var max = values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i].CompareTo(max) > 0)
                {
                    max = values[i];
                    imax = i;
                }
            }

            return imax;
        }

        /// <summary>
        ///     Gets the maximum element in a vector.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ArgMax<T>(this T[] values, out T max)
            where T : IComparable<T>
        {
            var imax = 0;
            max = values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i].CompareTo(max) > 0)
                {
                    max = values[i];
                    imax = i;
                }
            }

            return imax;
        }

        /// <summary>
        ///     Gets the minimum element in a vector.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ArgMin<T>(this T[] values)
            where T : IComparable<T>
        {
            var imin = 0;
            var min = values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i].CompareTo(min) < 0)
                {
                    min = values[i];
                    imin = i;
                }
            }

            return imin;
        }

        /// <summary>
        ///     Gets the minimum element in a vector.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ArgMin<T>(this T[] values, out T min)
            where T : IComparable<T>
        {
            var imin = 0;
            min = values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i].CompareTo(min) < 0)
                {
                    min = values[i];
                    imin = i;
                }
            }

            return imin;
        }

        #endregion

        #region Vector Min/Max

        /// <summary>
        ///     Gets the maximum non-null element in a vector.
        /// </summary>
        public static T? Max<T>(this T?[] values, out int imax)
            where T : struct, IComparable<T>
        {
            imax = -1;
            T? max = null;

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i].HasValue)
                {
                    if (max == null || values[i].Value.CompareTo(max.Value) > 0)
                    {
                        max = values[i];
                        imax = i;
                    }
                }
            }

            return max;
        }

        /// <summary>
        ///     Gets the minimum non-null element in a vector.
        /// </summary>
        public static T? Min<T>(this T?[] values, out int imin)
            where T : struct, IComparable<T>
        {
            imin = -1;
            T? min = null;

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i].HasValue)
                {
                    if (min == null || values[i].Value.CompareTo(min.Value) < 0)
                    {
                        min = values[i];
                        imin = i;
                    }
                }
            }

            return min;
        }

        /// <summary>
        ///     Gets the maximum element in a vector.
        /// </summary>
        public static T Max<T>(this T[] values, out int imax)
            where T : IComparable<T>
        {
            imax = 0;
            var max = values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i].CompareTo(max) > 0)
                {
                    max = values[i];
                    imax = i;
                }
            }

            return max;
        }

        /// <summary>
        ///     Gets the maximum element in a vector.
        /// </summary>
        public static T Max<T>(this T[] values, out int imax, bool alreadySorted)
            where T : IComparable<T>
        {
            if (alreadySorted)
            {
                imax = values.Length - 1;
                return values[values.Length - 1];
            }

            return Max(values, out imax);
        }

        /// <summary>
        ///     Gets the maximum element in a vector.
        /// </summary>
        public static T Max<T>(this T[] values)
            where T : IComparable<T>
        {
            return Max(values, out _);
        }

        /// <summary>
        ///     Gets the minimum element in a vector.
        /// </summary>
        public static T Min<T>(this T[] values, out int imin)
            where T : IComparable<T>
        {
            imin = 0;
            var min = values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i].CompareTo(min) < 0)
                {
                    min = values[i];
                    imin = i;
                }
            }

            return min;
        }

        /// <summary>
        ///     Gets the minimum element in a vector.
        /// </summary>
        public static T Min<T>(this T[] values)
            where T : IComparable<T>
        {
            return Min(values, out _);
        }

        #endregion

        #region limited length

        /// <summary>
        ///     Gets the maximum element in a vector up to a fixed length.
        /// </summary>
        public static T Max<T>(this T[] values, int length, out int imax)
            where T : IComparable<T>
        {
            imax = 0;
            var max = values[0];
            for (var i = 1; i < length; i++)
            {
                if (values[i].CompareTo(max) > 0)
                {
                    max = values[i];
                    imax = i;
                }
            }

            return max;
        }

        /// <summary>
        ///     Gets the maximum element in a vector up to a fixed length.
        /// </summary>
        public static T Max<T>(this T[] values, int length)
            where T : IComparable<T>
        {
            return Max(values, length, out _);
        }

        /// <summary>
        ///     Gets the minimum element in a vector up to a fixed length.
        /// </summary>
        public static T Min<T>(this T[] values, int length, out int imax)
            where T : IComparable<T>
        {
            imax = 0;
            var max = values[0];
            for (var i = 1; i < length; i++)
            {
                if (values[i].CompareTo(max) < 0)
                {
                    max = values[i];
                    imax = i;
                }
            }

            return max;
        }

        /// <summary>
        ///     Gets the minimum element in a vector up to a fixed length.
        /// </summary>
        public static T Min<T>(this T[] values, int length)
            where T : IComparable<T>
        {
            return Min(values, length, out _);
        }

        #endregion
    }
}