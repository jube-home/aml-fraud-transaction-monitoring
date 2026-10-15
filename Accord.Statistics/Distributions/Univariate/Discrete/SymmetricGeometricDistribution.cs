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

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Symmetric Geometric Distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Symmetric Geometric Distribution can be seen as a discrete case
    ///         of the <see cref="LaplaceDistribution" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="GeometricDistribution" />
    /// <seealso cref="HypergeometricDistribution" />
    [Serializable]
    public class SymmetricGeometricDistribution : UnivariateDiscreteDistribution
    {
        // Derived measures
        private double lnconstant;

        // Distribution parameters

        /// <summary>
        ///     Creates a new symmetric geometric distribution.
        /// </summary>
        /// <param name="probabilityOfSuccess">The success probability.</param>
        public SymmetricGeometricDistribution([Unit] double probabilityOfSuccess)
        {
            if (probabilityOfSuccess < 0 || probabilityOfSuccess > 1)
            {
                throw new ArgumentOutOfRangeException("probabilityOfSuccess",
                    "A probability must be between 0 and 1.");
            }

            ProbabilityOfSuccess = probabilityOfSuccess;
            lnconstant = System.Math.Log(ProbabilityOfSuccess) - System.Math.Log(2.0 * (1.0 - ProbabilityOfSuccess));
        }

        /// <summary>
        ///     Gets the success probability for the distribution.
        /// </summary>
        public double ProbabilityOfSuccess { get; }

        /// <summary>
        ///     Gets the support interval for this distribution, which
        ///     in the case of the Symmetric Geometric is [-inf, +inf].
        /// </summary>
        /// <value>
        ///     A <see cref="IntRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override IntRange Support => new(int.MinValue, int.MaxValue);

        /// <summary>
        ///     Gets the mean for this distribution, which in
        ///     the case of the Symmetric Geometric is zero.
        /// </summary>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Mean
        {
            get => 0;
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>The distribution's variance.</value>
        public override double Variance
        {
            get => (2 - ProbabilityOfSuccess) * (1 - ProbabilityOfSuccess) /
                   (ProbabilityOfSuccess * ProbabilityOfSuccess);
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        protected internal override double InnerDistributionFunction(int k)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Gets the probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="k">A single point in the distribution range.</param>
        /// <returns>
        ///     The probability of <c>k</c> occurring
        ///     in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerProbabilityMassFunction(int k)
        {
            if (k == 0)
            {
                return ProbabilityOfSuccess; // need special case otherwise will be double at center
            }

            return System.Math.Exp(lnconstant) * System.Math.Pow(1 - ProbabilityOfSuccess, System.Math.Abs(k) - 1);
        }

        /// <summary>
        ///     Gets the log-probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="k">A single point in the distribution range.</param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>k</c> will occur.
        /// </remarks>
        protected internal override double InnerLogProbabilityMassFunction(int k)
        {
            if (k == 0)
            {
                return System.Math.Log(ProbabilityOfSuccess); // need special case otherwise will be double at center
            }

            return lnconstant + (System.Math.Abs(k) - 1) * System.Math.Log(1 - ProbabilityOfSuccess);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>
        ///     A random observations drawn from this distribution.
        /// </returns>
        public override int Generate(Random source)
        {
            var u = source.NextDouble();
            return System.Math.Sign(u - 0.5) * (int)GeometricDistribution.Random(ProbabilityOfSuccess, source);
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
        /// <returns>
        ///     A random vector of observations drawn from this distribution.
        /// </returns>
        public override double[] Generate(int samples, double[] result, Random source)
        {
            GeometricDistribution.Random(ProbabilityOfSuccess, samples, result, source);
            for (var i = 0; i < samples; i++)
            {
                result[i] *= System.Math.Sign(source.NextDouble() - 0.5);
            }

            return result;
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
        /// <returns>
        ///     A random vector of observations drawn from this distribution.
        /// </returns>
        public override int[] Generate(int samples, int[] result, Random source)
        {
            GeometricDistribution.Random(ProbabilityOfSuccess, samples, result, source);
            for (var i = 0; i < samples; i++)
            {
                result[i] *= System.Math.Sign(source.NextDouble() - 0.5);
            }

            return result;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new SymmetricGeometricDistribution(ProbabilityOfSuccess);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "SymmetricGeometric(x; p = {0})",
                ProbabilityOfSuccess.ToString(format, formatProvider));
        }
    }
}