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
    ///     Euclidean distance metric.
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Math\DistanceTest.cs" region="doc_euclidean_4" />
    /// </example>
    /// <seealso cref="SquareEuclidean" />
    /// <seealso cref="WeightedSquareEuclidean" />
    /// <seealso cref="WeightedEuclidean" />
    [Serializable]
    public struct Euclidean :
        IMetric<double>, ISimilarity<double>,
        IMetric<double[]>, ISimilarity<double[]>,
        IMetric<Tuple<double, double>>, ISimilarity<Tuple<double, double>>,
        IDistance<Sparse<double>>, ISimilarity<Sparse<double>>,
        ICloneable
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
        public double Distance(double x, double y)
        {
            return System.Math.Abs(x - y);
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
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\DistanceTest.cs" region="doc_euclidean_4" />
        /// </example>
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

            return System.Math.Sqrt(sum);
        }

        /// <summary>
        ///     Gets the Euclidean distance between two points. Note: this function
        ///     is dangerous as it is too easy to invert its arguments by mistake.
        ///     Please consider using the Tuple&lt;double, double> overload instead.
        /// </summary>
        /// <param name="vector1x">The first coordinate of first point in space.</param>
        /// <param name="vector1y">The second coordinate of first point in space.</param>
        /// <param name="vector2x">The first coordinate of second point in space.</param>
        /// <param name="vector2y">The second coordinate of second point in space.</param>
        /// <returns>The Euclidean distance between x and y.</returns>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\DistanceTest.cs" region="doc_euclidean_4" />
        /// </example>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double vector1x, double vector1y, double vector2x, double vector2y)
        {
            var dx = vector1x - vector2x;
            var dy = vector1y - vector2y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        ///     Gets the Euclidean distance between two points.
        /// </summary>
        /// <param name="x">The first point in space.</param>
        /// <param name="y">The second point in space.</param>
        /// <returns>The Euclidean distance between x and y.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(Tuple<double, double> x, Tuple<double, double> y)
        {
            var dx = x.Item1 - y.Item1;
            var dy = x.Item2 - y.Item2;
            return System.Math.Sqrt(dx * dx + dy * dy);
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
            return System.Math.Sqrt(SquareEuclidean.Sparse(x, y));
        }

        /// <summary>
        ///     Gets a similarity measure between two points.
        /// </summary>
        /// <param name="x">The first point to be compared.</param>
        /// <param name="y">The second point to be compared.</param>
        /// <returns>A similarity measure between x and y.</returns>
        public double Similarity(double x, double y)
        {
            return 1.0 / (1.0 + System.Math.Abs(x - y));
        }

        /// <summary>
        ///     Gets a similarity measure between two points.
        /// </summary>
        /// <param name="x">The first point to be compared.</param>
        /// <param name="y">The second point to be compared.</param>
        /// <returns>A similarity measure between x and y.</returns>
        public double Similarity(double[] x, double[] y)
        {
            var sum = 0.0;

            for (var i = 0; i < x.Length; i++)
            {
                var u = x[i] - y[i];
                sum += u * u;
            }

            return 1.0 / (1.0 + System.Math.Sqrt(sum));
        }

        /// <summary>
        ///     Gets a similarity measure between two points.
        /// </summary>
        /// <param name="x">The first point to be compared.</param>
        /// <param name="y">The second point to be compared.</param>
        /// <returns>A similarity measure between x and y.</returns>
        public double Similarity(Tuple<double, double> x, Tuple<double, double> y)
        {
            var dx = x.Item1 - y.Item1;
            var dy = x.Item2 - y.Item2;

            return 1.0 / (1.0 + System.Math.Sqrt(dx * dx + dy * dy));
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
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new Euclidean();
        }
    }
}