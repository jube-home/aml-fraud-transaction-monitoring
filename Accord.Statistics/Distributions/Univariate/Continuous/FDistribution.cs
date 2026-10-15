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
using Accord.Math.Random;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     F (Fisher-Snedecor) distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability theory and statistics, the F-distribution is a continuous
    ///         probability distribution. It is also known as Snedecor's F distribution
    ///         or the Fisher-Snedecor distribution (after R.A. Fisher and George W. Snedecor).
    ///         The F-distribution arises frequently as the null distribution of a test statistic,
    ///         most notably in the analysis of variance; see <see cref="Testing.FTest" />.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/F-distribution">
    ///                         Wikipedia, The Free Encyclopedia. F-distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/F-distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to construct a Fisher-Snedecor's F-distribution
    ///         with 8 and 5 degrees of freedom, respectively.
    ///     </para>
    ///     <code>
    ///   // Create a Fisher-Snedecor's F distribution with 8 and 5 d.f.
    ///   FDistribution F = new FDistribution(degrees1: 8, degrees2: 5);
    /// 
    ///   // Common measures
    ///   double mean = F.Mean;     // 1.6666666666666667
    ///   double median = F.Median; // 1.0545096252132447
    ///   double var = F.Variance;  // 7.6388888888888893
    ///   
    ///   // Cumulative distribution functions
    ///   double cdf = F.DistributionFunction(x: 0.27); // 0.049463408057268315
    ///   double ccdf = F.ComplementaryDistributionFunction(x: 0.27); // 0.95053659194273166
    ///   double icdf = F.InverseDistributionFunction(p: cdf); // 0.27
    ///   
    ///   // Probability density functions
    ///   double pdf = F.ProbabilityDensityFunction(x: 0.27); // 0.45120469723580559
    ///   double lpdf = F.LogProbabilityDensityFunction(x: 0.27); // -0.79583416831212883
    ///   
    ///   // Hazard (failure rate) functions
    ///   double hf = F.HazardFunction(x: 0.27); // 0.47468419528555084
    ///   double chf = F.CumulativeHazardFunction(x: 0.27); // 0.050728620222091653
    ///   
    ///   // String representation
    ///   string str = F.ToString(CultureInfo.InvariantCulture); // F(x; df1 = 8, df2 = 5)
    /// </code>
    /// </example>
    [Serializable]
    public class FDistribution : UnivariateContinuousDistribution,
        ISampleableDistribution<double>
    {
        // Derived values
        private double b;

        // Distribution parameters

        private double? mean;
        private double? variance;

        /// <summary>
        ///     Constructs a F-distribution with
        ///     the given degrees of freedom.
        /// </summary>
        public FDistribution()
            : this(1, 1)
        {
        }

        /// <summary>
        ///     Constructs a F-distribution with
        ///     the given degrees of freedom.
        /// </summary>
        /// <param name="degrees1">The first degree of freedom. Default is 1.</param>
        /// <param name="degrees2">The second degree of freedom. Default is 1.</param>
        public FDistribution([PositiveInteger] int degrees1, [PositiveInteger] int degrees2)
        {
            if (degrees1 <= 0)
            {
                throw new ArgumentOutOfRangeException("degrees1", "Degrees of freedom must be positive.");
            }

            if (degrees2 <= 0)
            {
                throw new ArgumentOutOfRangeException("degrees1", "Degrees of freedom must be positive.");
            }

            DegreesOfFreedom1 = degrees1;
            DegreesOfFreedom2 = degrees2;

            b = Beta.Function(degrees1 * 0.5, degrees2 * 0.5);
        }

        /// <summary>
        ///     Gets the first degree of freedom.
        /// </summary>
        public int DegreesOfFreedom1 { get; }

        /// <summary>
        ///     Gets the second degree of freedom.
        /// </summary>
        public int DegreesOfFreedom2 { get; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        public override double Mean
        {
            get
            {
                if (!mean.HasValue)
                {
                    if (DegreesOfFreedom2 <= 2)
                    {
                        mean = double.NaN;
                    }
                    else
                    {
                        mean = DegreesOfFreedom2 / (DegreesOfFreedom2 - 2.0);
                    }
                }

                return mean.Value;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        public override double Variance
        {
            get
            {
                if (!variance.HasValue)
                {
                    if (DegreesOfFreedom2 <= 4)
                    {
                        variance = double.NaN;
                    }
                    else
                    {
                        variance = 2.0 * DegreesOfFreedom2 * DegreesOfFreedom2 *
                                   (DegreesOfFreedom1 + DegreesOfFreedom2 - 2) /
                                   (DegreesOfFreedom1 * (DegreesOfFreedom2 - 2) * (DegreesOfFreedom2 - 2) *
                                    (DegreesOfFreedom2 - 4));
                    }
                }

                return variance.Value;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's mode value.
        /// </value>
        public override double Mode
        {
            get
            {
                if (DegreesOfFreedom1 > 2)
                {
                    var a = (DegreesOfFreedom1 - 2.0) / DegreesOfFreedom1;
                    var b = DegreesOfFreedom2 / (DegreesOfFreedom2 + 2.0);
                    return a * b;
                }

                return double.NaN;
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="DoubleRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override DoubleRange Support
        {
            get => new(0, double.PositiveInfinity);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new FDistribution(DegreesOfFreedom1, DegreesOfFreedom2);
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     the F-distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     <para>
        ///         The Cumulative Distribution Function (CDF) describes the cumulative
        ///         probability that a given value or any value smaller than it will occur.
        ///     </para>
        ///     <para>
        ///         The F-distribution CDF is computed in terms of the
        ///         <see cref="Beta.Incomplete">
        ///             Incomplete Beta function Ix(a,b)
        ///         </see>
        ///         as CDF(x) = Iu(d1/2, d2/2) in which
        ///         u is given as u = (d1 * x) / (d1 * x + d2)
        ///     </para>
        ///     .
        /// </remarks>
        protected internal override double InnerDistributionFunction(double x)
        {
            var u = DegreesOfFreedom1 * x / (DegreesOfFreedom1 * x + DegreesOfFreedom2);
            return Beta.Incomplete(DegreesOfFreedom1 * 0.5, DegreesOfFreedom2 * 0.5, u);
        }

        /// <summary>
        ///     Gets the complementary cumulative distribution
        ///     function evaluated at point <c>x</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The F-distribution complementary CDF is computed in terms of the
        ///         <see cref="Beta.Incomplete">
        ///             Incomplete Beta function Ix(a,b)
        ///         </see>
        ///         as CDFc(x) = Iu(d2/2, d1/2) in which
        ///         u is given as u = (d2 * x) / (d2 * x + d1)
        ///     </para>
        ///     .
        /// </remarks>
        protected internal override double InnerComplementaryDistributionFunction(double x)
        {
            var u = DegreesOfFreedom2 / (DegreesOfFreedom2 + DegreesOfFreedom1 * x);
            return Beta.Incomplete(DegreesOfFreedom2 * 0.5, DegreesOfFreedom1 * 0.5, u);
        }

        /// <summary>
        ///     Gets the inverse of the cumulative distribution function (icdf) for
        ///     this distribution evaluated at probability <c>p</c>. This function
        ///     is also known as the Quantile function.
        /// </summary>
        /// <remarks>
        ///     The Inverse Cumulative Distribution Function (ICDF) specifies, for
        ///     a given probability, the value which the random variable will be at,
        ///     or below, with that probability.
        /// </remarks>
        protected internal override double InnerInverseDistributionFunction(double p)
        {
            // Cephes Math Library Release 2.8:  June, 2000
            // Copyright 1984, 1987, 1995, 2000 by Stephen L. Moshier
            // Adapted under the LGPL with permission of original author.

            double d1 = this.DegreesOfFreedom1;
            double d2 = this.DegreesOfFreedom2;

            double x;

            var w = Beta.Incomplete(0.5 * d2, 0.5 * d1, 0.5);

            if (w > p || p < 0.001)
            {
                w = Beta.IncompleteInverse(0.5 * d1, 0.5 * d2, p);
                x = d2 * w / (d1 * (1.0 - w));
            }
            else
            {
                w = Beta.IncompleteInverse(0.5 * d2, 0.5 * d1, 1.0 - p);
                x = (d2 - d2 * w) / (d1 * w);
            }

            return x;
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     the F-distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var u = System.Math.Pow(DegreesOfFreedom1 * x, DegreesOfFreedom1) *
                    System.Math.Pow(DegreesOfFreedom2, DegreesOfFreedom2) /
                    System.Math.Pow(DegreesOfFreedom1 * x + DegreesOfFreedom2, DegreesOfFreedom1 + DegreesOfFreedom2);
            return System.Math.Sqrt(u) / (x * b);
        }

        /// <summary>
        ///     Gets the log-probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            var lnu = DegreesOfFreedom1 * System.Math.Log(DegreesOfFreedom1 * x) +
                      DegreesOfFreedom2 * System.Math.Log(DegreesOfFreedom2) -
                      (DegreesOfFreedom1 + DegreesOfFreedom2) *
                      System.Math.Log(DegreesOfFreedom1 * x + DegreesOfFreedom2);
            return 0.5 * lnu - System.Math.Log(x * b);
        }

        /// <summary>
        ///     Not available.
        /// </summary>
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("F(x; df1 = {0}, df2 = {1})",
                DegreesOfFreedom1.ToString(format, formatProvider),
                DegreesOfFreedom2.ToString(format, formatProvider));
        }

        #region ISamplableDistribution<double> Members

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
        public override double[] Generate(int samples, double[] result, Random source)
        {
            return Random(DegreesOfFreedom1, DegreesOfFreedom2, samples, result, source);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public override double Generate(Random source)
        {
            return Random(DegreesOfFreedom1, DegreesOfFreedom2, source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     F-distribution with the given parameters.
        /// </summary>
        /// <param name="d1">The first degree of freedom.</param>
        /// <param name="d2">The second degree of freedom.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <returns>An array of double values sampled from the specified F-distribution.</returns>
        public static double[] Random(int d1, int d2, int samples)
        {
            return Random(d1, d2, samples, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     F-distribution with the given parameters.
        /// </summary>
        /// <param name="d1">The first degree of freedom.</param>
        /// <param name="d2">The second degree of freedom.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>An array of double values sampled from the specified F-distribution.</returns>
        public static double[] Random(int d1, int d2, int samples, double[] result)
        {
            return Random(d1, d2, samples, result, Generator.Random);
        }

        /// <summary>
        ///     Generates a random observation from the
        ///     F-distribution with the given parameters.
        /// </summary>
        /// <param name="d1">The first degree of freedom.</param>
        /// <param name="d2">The second degree of freedom.</param>
        /// <returns>A random double value sampled from the specified F-distribution.</returns>
        public static double Random(int d1, int d2)
        {
            return Random(d1, d2, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     F-distribution with the given parameters.
        /// </summary>
        /// <param name="d1">The first degree of freedom.</param>
        /// <param name="d2">The second degree of freedom.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>An array of double values sampled from the specified F-distribution.</returns>
        public static double[] Random(int d1, int d2, int samples, Random source)
        {
            return Random(d1, d2, samples, new double[samples], source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     F-distribution with the given parameters.
        /// </summary>
        /// <param name="d1">The first degree of freedom.</param>
        /// <param name="d2">The second degree of freedom.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>An array of double values sampled from the specified F-distribution.</returns>
        public static double[] Random(int d1, int d2, int samples, double[] result, Random source)
        {
            var x = GammaDistribution.Random(d1 / 2.0, 2, samples, result,
                source);
            var y = GammaDistribution.Random(d2 / 2.0, 2, samples, source);

            for (var i = 0; i < x.Length; i++)
            {
                x[i] /= y[i];
            }

            return x;
        }

        /// <summary>
        ///     Generates a random observation from the
        ///     F-distribution with the given parameters.
        /// </summary>
        /// <param name="d1">The first degree of freedom.</param>
        /// <param name="d2">The second degree of freedom.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random double value sampled from the specified F-distribution.</returns>
        public static double Random(int d1, int d2, Random source)
        {
            var x = GammaDistribution.Random(d1 / 2.0, 2, source);
            var y = GammaDistribution.Random(d2 / 2.0, 2, source);
            return x / y;
        }

        #endregion
    }
}