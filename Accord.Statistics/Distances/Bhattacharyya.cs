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
using Accord.Math.Decompositions;
using Accord.Statistics;
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Math.Distances
{
    /// <summary>
    ///     Bhattacharyya distance.
    /// </summary>
    [Serializable]
    public sealed class Bhattacharyya :
        IDistance<double[]>,
        IDistance<double[,]>, IDistance<double[][]>,
        IDistance<GeneralDiscreteDistribution>,
        IDistance<UnivariateDiscreteDistribution>,
        IDistance<MultivariateNormalDistribution>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Bhattacharyya" /> class.
        /// </summary>
        public Bhattacharyya()
        {
        }

        /// <summary>
        ///     Bhattacharyya distance between two histograms.
        /// </summary>
        /// <param name="x">The first histogram.</param>
        /// <param name="y">The second histogram.</param>
        /// <returns>
        ///     The Bhattacharyya between the two histograms.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(GeneralDiscreteDistribution x, GeneralDiscreteDistribution y)
        {
            double b = 0;
            for (var i = 0; i < x.Length; i++)
            {
                b += System.Math.Sqrt(x.Frequencies[i] * y.Frequencies[i]);
            }

            return System.Math.Sqrt(1.0 - b);
        }

        /// <summary>
        ///     Bhattacharyya distance between two Gaussian distributions.
        /// </summary>
        /// <param name="x">The first Normal distribution.</param>
        /// <param name="y">The second Normal distribution.</param>
        /// <returns>The Bhattacharyya distance between the two distributions.</returns>
        public double Distance(MultivariateNormalDistribution x, MultivariateNormalDistribution y)
        {
            return Distance(x.Mean, x.Covariance, y.Mean, y.Covariance);
        }

        /// <summary>
        ///     Bhattacharyya distance between two histograms.
        /// </summary>
        /// <param name="x">The first histogram.</param>
        /// <param name="y">The second histogram.</param>
        /// <returns>
        ///     The Bhattacharyya between the two histograms.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(UnivariateDiscreteDistribution x, UnivariateDiscreteDistribution y)
        {
            double b = 0;
            foreach (var i in x.Support.Intersection(y.Support))
            {
                b += System.Math.Sqrt(x.ProbabilityMassFunction(i) * y.ProbabilityMassFunction(i));
            }

            return System.Math.Sqrt(1.0 - b);
        }

        /// <summary>
        ///     Bhattacharyya distance between two datasets, assuming
        ///     their contents can be modelled by multivariate Gaussians.
        /// </summary>
        /// <param name="x">The first dataset.</param>
        /// <param name="y">The second dataset.</param>
        /// <returns>
        ///     The Bhattacharyya between the two datasets.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double[,] x, double[,] y)
        {
            var meanX = x.Mean(0);
            var meanY = y.Mean(0);
            var covX = x.Covariance(meanX);
            var covY = y.Covariance(meanY);

            return Distance(meanX, covX, meanY, covY);
        }

        /// <summary>
        ///     Bhattacharyya distance between two histograms.
        /// </summary>
        /// <param name="x">The first histogram.</param>
        /// <param name="y">The second histogram.</param>
        /// <returns>
        ///     The Bhattacharyya between the two histograms.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double[] x, double[] y)
        {
            double b = 0;
            for (var i = 0; i < x.Length; i++)
            {
                b += System.Math.Sqrt(x[i] * y[i]);
            }

            return System.Math.Sqrt(1.0 - b);
        }

        /// <summary>
        ///     Bhattacharyya distance between two datasets, assuming
        ///     their contents can be modelled by multivariate Gaussians.
        /// </summary>
        /// <param name="x">The first dataset.</param>
        /// <param name="y">The second dataset.</param>
        /// <returns>
        ///     The Bhattacharyya between the two datasets.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double[][] x, double[][] y)
        {
            var meanX = x.Mean(0);
            var meanY = y.Mean(0);
            var covX = x.Covariance(meanX);
            var covY = y.Covariance(meanY);

            return Distance(meanX, covX, meanY, covY);
        }

        /// <summary>
        ///     Bhattacharyya distance between two Gaussian distributions.
        /// </summary>
        /// <param name="meanX">Mean for the first distribution.</param>
        /// <param name="covX">Covariance matrix for the first distribution.</param>
        /// <param name="meanY">Mean for the second distribution.</param>
        /// <param name="covY">Covariance matrix for the second distribution.</param>
        /// <returns>The Bhattacharyya distance between the two distributions.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double[] meanX, double[,] covX, double[] meanY, double[,] covY)
        {
            var lnDetX = covX.LogPseudoDeterminant();
            var lnDetY = covY.LogPseudoDeterminant();

            return Distance(meanX, covX, lnDetX, meanY, covY, lnDetY);
        }

        /// <summary>
        ///     Bhattacharyya distance between two Gaussian distributions.
        /// </summary>
        /// <param name="meanX">Mean for the first distribution.</param>
        /// <param name="covX">Covariance matrix for the first distribution.</param>
        /// <param name="meanY">Mean for the second distribution.</param>
        /// <param name="covY">Covariance matrix for the second distribution.</param>
        /// <returns>The Bhattacharyya distance between the two distributions.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double[] meanX, double[][] covX, double[] meanY, double[][] covY)
        {
            var lnDetX = covX.LogPseudoDeterminant();
            var lnDetY = covY.LogPseudoDeterminant();

            return Distance(meanX, covX, lnDetX, meanY, covY, lnDetY);
        }

        /// <summary>
        ///     Bhattacharyya distance between two Gaussian distributions.
        /// </summary>
        /// <param name="meanX">Mean for the first distribution.</param>
        /// <param name="covX">Covariance matrix for the first distribution.</param>
        /// <param name="meanY">Mean for the second distribution.</param>
        /// <param name="covY">Covariance matrix for the second distribution.</param>
        /// <param name="lnDetCovX">
        ///     The logarithm of the determinant for
        ///     the covariance matrix of the first distribution.
        /// </param>
        /// <param name="lnDetCovY">
        ///     The logarithm of the determinant for
        ///     the covariance matrix of the second distribution.
        /// </param>
        /// <returns>The Bhattacharyya distance between the two distributions.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(
            double[] meanX, double[,] covX, double lnDetCovX,
            double[] meanY, double[,] covY, double lnDetCovY)
        {
            var n = meanX.Length;

            // P = (covX + covY) / 2
            var P = new double[n, n];
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    P[i, j] = (covX[i, j] + covY[i, j]) / 2.0;
                }
            }

            var svd = new SingularValueDecomposition(P);
            var detP = svd.LogPseudoDeterminant;

            var d = new double[meanX.Length];
            for (var i = 0; i < meanX.Length; i++)
            {
                d[i] = meanX[i] - meanY[i];
            }

            var z = svd.Solve(d);

            var r = 0.0;
            for (var i = 0; i < d.Length; i++)
            {
                r += d[i] * z[i];
            }

            var mahalanobis = System.Math.Abs(r);

            var a = 1.0 / 8.0 * mahalanobis;
            var b = 0.5 * (detP - 0.5 * (lnDetCovX + lnDetCovY));

            return a + b;
        }

        /// <summary>
        ///     Bhattacharyya distance between two Gaussian distributions.
        /// </summary>
        /// <param name="meanX">Mean for the first distribution.</param>
        /// <param name="covX">Covariance matrix for the first distribution.</param>
        /// <param name="meanY">Mean for the second distribution.</param>
        /// <param name="covY">Covariance matrix for the second distribution.</param>
        /// <param name="lnDetCovX">
        ///     The logarithm of the determinant for
        ///     the covariance matrix of the first distribution.
        /// </param>
        /// <param name="lnDetCovY">
        ///     The logarithm of the determinant for
        ///     the covariance matrix of the second distribution.
        /// </param>
        /// <returns>The Bhattacharyya distance between the two distributions.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(
            double[] meanX, double[][] covX, double lnDetCovX,
            double[] meanY, double[][] covY, double lnDetCovY)
        {
            var n = meanX.Length;

            // P = (covX + covY) / 2
            var P = Jagged.Zeros(n, n);
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    P[i][j] = (covX[i][j] + covY[i][j]) / 2.0;
                }
            }

            var svd = new JaggedSingularValueDecomposition(P);
            var detP = svd.LogPseudoDeterminant;

            var d = new double[meanX.Length];
            for (var i = 0; i < meanX.Length; i++)
            {
                d[i] = meanX[i] - meanY[i];
            }

            var z = svd.Solve(d);

            var r = 0.0;
            for (var i = 0; i < d.Length; i++)
            {
                r += d[i] * z[i];
            }

            var mahalanobis = System.Math.Abs(r);

            var a = 1.0 / 8.0 * mahalanobis;
            var b = 0.5 * (detP - 0.5 * (lnDetCovX + lnDetCovY));

            return a + b;
        }
    }
}