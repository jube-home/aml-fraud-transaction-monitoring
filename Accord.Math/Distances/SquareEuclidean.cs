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

namespace Accord.Math.Distances
{
    /// <summary>
    ///     Square-Euclidean distance and similarity. Please note that this
    ///     distance is not a metric as it doesn't obey the triangle inequality.
    /// </summary>
    /// <seealso cref="Euclidean" />
    [Serializable]
    public struct SquareEuclidean :
        IDistance<double[]>, ISimilarity<double[]>,
        IDistance<double>, ISimilarity<double>,
        IDistance<Sparse<double>>, ISimilarity<Sparse<double>>, ICloneable
    {
        /// <summary>
        ///     Computes the distance <c>d(x,y)</c> between points
        ///     <paramref name="x" /> and <paramref name="y" />.
        /// </summary>
        /// <param name="x">The first point <c>x</c>.</param>
        /// <param name="y">The second point <c>y</c>.</param>
        /// <returns>
        ///     A double-precision value representing the distance <c>d(x,y)</c>
        ///     between <paramref name="x" /> and <paramref name="y" /> according
        ///     to the distance function implemented by this class.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double[] x, double[] y)
        {
            var sum = 0.0;

            for (var i = 0; i < x.Length; i++)
            {
                var u = x[i] - y[i];
                sum += u * u;
            }

            return sum;
        }

        /// <summary>
        ///     Computes the distance <c>d(x,y)</c> between points
        ///     <paramref name="x" /> and <paramref name="y" />.
        /// </summary>
        /// <param name="x">The first point <c>x</c>.</param>
        /// <param name="y">The second point <c>y</c>.</param>
        /// <returns>
        ///     A double-precision value representing the distance <c>d(x,y)</c>
        ///     between <paramref name="x" /> and <paramref name="y" /> according
        ///     to the distance function implemented by this class.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double x, double y)
        {
            var u = x - y;
            return u * u;
        }

        /// <summary>
        ///     Computes the distance <c>d(x,y)</c> between points
        ///     <paramref name="x" /> and <paramref name="y" />.
        /// </summary>
        /// <param name="x">The first point <c>x</c>.</param>
        /// <param name="y">The second point <c>y</c>.</param>
        /// <returns>
        ///     A double-precision value representing the distance <c>d(x,y)</c>
        ///     between <paramref name="x" /> and <paramref name="y" /> according
        ///     to the distance function implemented by this class.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(Sparse<double> x, Sparse<double> y)
        {
            return Sparse(x, y);
        }

        /// <summary>
        ///     Gets the Square Euclidean distance between two points.
        /// </summary>
        /// <param name="x1">The first coordinate of first point in space.</param>
        /// <param name="y1">The second coordinate of first point in space.</param>
        /// <param name="x2">The first coordinate of second point in space.</param>
        /// <param name="y2">The second coordinate of second point in space.</param>
        /// <returns>The Square Euclidean distance between x and y.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double x1, double y1, double x2, double y2)
        {
            var dx = x1 - x2;
            var dy = y1 - y2;
            return dx * dx + dy * dy;
        }

        /// <summary>
        ///     Gets a similarity measure between two points.
        /// </summary>
        /// <param name="x">The first point to be compared.</param>
        /// <param name="y">The second point to be compared.</param>
        /// <returns>A similarity measure between x and y.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Similarity(double[] x, double[] y)
        {
            return 1.0 / (1.0 + Distance(x, y));
        }

        /// <summary>
        ///     Gets a similarity measure between two points.
        /// </summary>
        /// <param name="x">The first point to be compared.</param>
        /// <param name="y">The second point to be compared.</param>
        /// <returns>A similarity measure between x and y.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Similarity(Sparse<double> x, Sparse<double> y)
        {
            return 1.0 / (1.0 + Distance(x, y));
        }

        /// <summary>
        ///     Gets a similarity measure between two points.
        /// </summary>
        /// <param name="x">The first point to be compared.</param>
        /// <param name="y">The second point to be compared.</param>
        /// <returns>A similarity measure between x and y.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Similarity(double x, double y)
        {
            return 1.0 / (1.0 + Distance(x, y));
        }

        /// <summary>
        ///     Computes the distance <c>d(x,y)</c> between points
        ///     <paramref name="x" /> and <paramref name="y" />.
        /// </summary>
        /// <param name="x">The first point <c>x</c>.</param>
        /// <param name="y">The second point <c>y</c>.</param>
        /// <returns>
        ///     A double-precision value representing the distance <c>d(x,y)</c>
        ///     between <paramref name="x" /> and <paramref name="y" /> according
        ///     to the distance function implemented by this class.
        /// </returns>
        public static double Sparse(Sparse<double> x, Sparse<double> y)
        {
            double sum = 0;

            int i = 0, j = 0;

            while (i < x.Indices.Length && j < y.Indices.Length)
            {
                var posx = x.Indices[i];
                var posy = y.Indices[j];

                if (posx == posy)
                {
                    var d = x.Values[i] - y.Values[j];
                    sum += d * d;
                    i++;
                    j++;
                }
                else if (posx < posy)
                {
                    var d = x.Values[i];
                    sum += d * d;
                    i++;
                }
                else if (posx > posy)
                {
                    var d = y.Values[j];
                    sum += d * d;
                    j++;
                }
            }

            for (; i < x.Values.Length; i++)
            {
                sum += x.Values[i] * x.Values[i];
            }

            for (; j < y.Values.Length; j++)
            {
                sum += y.Values[j] * y.Values[j];
            }

            return sum;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new SquareEuclidean();
        }
    }
}