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
    ///     The Minkowski distance is a metric in a normed vector space which can be
    ///     considered as a generalization of both the
    ///     <see cref="Euclidean">
    ///         Euclidean
    ///         distance
    ///     </see>
    ///     and the <see cref="Manhattan">Manhattan distance</see>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The framework distinguishes between metrics and distances by using different
    ///         types for them. This makes it possible to let the compiler figure out logic
    ///         problems such as the specification of a non-metric for a method that requires
    ///         a proper metric (i.e. that respects the triangle inequality).
    ///     </para>
    ///     <para>
    ///         The objective of this technique is to make it harder to make some mistakes.
    ///         However, it is possible to bypass this mechanism by using the named constructors
    ///         such as <see cref="Nonmetric" /> to create distances implementing the <see cref="IMetric{T}" />
    ///         interface that are not really metrics. Use at your own risk.
    ///     </para>
    /// </remarks>
    [Serializable]
    public struct Minkowski : IMetric<double[]>, IMetric<int[]>, ICloneable
    {
        /// <summary>
        ///     Gets the order <c>p</c> of this Minkowski distance.
        /// </summary>
        public double Order { get; private set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Minkowski" /> class.
        /// </summary>
        /// <param name="p">The Minkowski order <c>p</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">The Minkowski distance is not a metric for p &lt; 1.</exception>
        public Minkowski(double p)
        {
            if (p < 1)
            {
                throw new ArgumentOutOfRangeException("p", "The Minkowski distance is not a metric for p < 1.");
            }

            this.Order = p;
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
        public double Distance(int[] x, int[] y)
        {
            double sum = 0;
            for (var i = 0; i < x.Length; i++)
            {
                sum += System.Math.Pow(System.Math.Abs(x[i] - y[i]), Order);
            }

            return System.Math.Pow(sum, 1 / Order);
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
        public double Distance(double[] x, double[] y)
        {
            double sum = 0;
            for (var i = 0; i < x.Length; i++)
            {
                sum += System.Math.Pow(System.Math.Abs(x[i] - y[i]), Order);
            }

            return System.Math.Pow(sum, 1 / Order);
        }

        /// <summary>
        ///     Creates a non-metric Minkowski distance, bypassing
        ///     argument checking. Use at your own risk.
        /// </summary>
        /// <param name="p">The Minkowski order <c>p</c>.</param>
        /// <returns>
        ///     A Minkowski object implementing a Minkowski distance
        ///     that is not necessarily a metric. Use at your own risk.
        /// </returns>
        public static Minkowski Nonmetric(double p)
        {
            var minkowski = new Minkowski(1);
            minkowski.Order = p;
            return minkowski;
        }

        /// <summary>
        ///     Gets the <see cref="Manhattan" /> distance as a special
        ///     case of the <see cref="Minkowski" /> distance.
        /// </summary>
        public static readonly Minkowski Manhattan = new(1);

        /// <summary>
        ///     Gets the <see cref="Euclidean" /> distance as a special
        ///     case of the <see cref="Minkowski" /> distance.
        /// </summary>
        public static readonly Minkowski Euclidean = new(2);

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new Minkowski(Order);
        }
    }
}