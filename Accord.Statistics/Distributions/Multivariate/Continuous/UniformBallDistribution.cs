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
using Accord.Math;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Distributions.Multivariate
{
    /// <summary>
    ///     Uniform distribution inside a n-dimensional ball.
    /// </summary>
    /// <seealso cref="MultivariateContinuousDistribution" />
    [Serializable]
    public class UniformBallDistribution : MultivariateContinuousDistribution,
        ISampleableDistribution<double[]>
    {
        // Distribution parameters
        private double[] center;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UniformBallDistribution" /> class.
        /// </summary>
        /// <param name="dimension">The number of dimensions in the n-dimensional sphere.</param>
        public UniformBallDistribution(int dimension)
            : this(new double[dimension])
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UniformBallDistribution" /> class.
        /// </summary>
        /// <param name="mean">The sphere's mean.</param>
        /// <param name="radius">The sphere's radius.</param>
        public UniformBallDistribution(double[] mean, double radius = 1)
            : base(mean.Length)
        {
            center = mean;
            this.Radius = radius;

            double n = mean.Length;
            var num = System.Math.Pow(System.Math.PI, n / 2.0);
            var den = Gamma.Function(n / 2.0 + 1);
            Volume = num / den * System.Math.Pow(radius, n);
        }

        /// <summary>
        ///     Gets the sphere radius.
        /// </summary>
        public double Radius { get; }

        /// <summary>
        ///     Gets the sphere volume.
        /// </summary>
        public double Volume { get; }

        /// <summary>
        ///     Gets the sphere center (mean) vector.
        /// </summary>
        /// <value>A vector containing the mean values for the distribution.</value>
        public override double[] Mean => center;

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>A vector containing the variance values for the distribution.</value>
        public override double[] Variance => Vector.Create(Dimension, Radius);

        /// <summary>
        ///     Gets the variance-covariance matrix for this distribution.
        /// </summary>
        /// <value>A matrix containing the covariance values for the distribution.</value>
        public override double[,] Covariance => Matrix.Diagonal(Variance);

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new UniformBallDistribution(center.Copy(), Radius);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public override double[][] Generate(int samples, double[][] result, Random source)
        {
            var normal = new double[Dimension + 2];

            for (var i = 0; i < result.Length; i++)
            {
                // Generate n + 2 independent normal distributed values
                NormalDistribution.Random(normal.Length, normal, source);
                normal.Normalize(true); // make unit norm
                for (var j = 0; j < Dimension; j++) // take n dimensions
                {
                    result[i][j] = normal[j] * Radius + center[j];
                }
            }

            return result;
        }

        /// <summary>
        ///     Not implemented.
        /// </summary>
        protected internal override double InnerDistributionFunction(double[] x)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range. For a
        ///     univariate distribution, this should be a single
        ///     double value. For a multivariate distribution,
        ///     this should be a double array.
        /// </param>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerProbabilityDensityFunction(double[] x)
        {
            if (Distance.Euclidean(x, center) < Radius)
            {
                return 1 / Volume;
            }

            return 0;
        }

        /// <summary>
        ///     Gets the log-probability density function (pdf)
        ///     for this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range. For a
        ///     univariate distribution, this should be a single
        ///     double value. For a multivariate distribution,
        ///     this should be a double array.
        /// </param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        protected internal override double InnerLogProbabilityDensityFunction(params double[] x)
        {
            if (Distance.Euclidean(x, center) < Radius)
            {
                return -System.Math.Log(Volume);
            }

            return double.NegativeInfinity;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "UniformBall(X; μ, r)");
        }
    }
}