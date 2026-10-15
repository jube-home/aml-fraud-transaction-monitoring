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
    ///     Chi-Square (χ²) probability distribution
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability theory and statistics, the chi-square distribution (also chi-squared
    ///         or χ²-distribution) with k degrees of freedom is the distribution of a sum of the
    ///         squares of k independent standard normal random variables. It is one of the most
    ///         widely used probability distributions in inferential statistics, e.g. in hypothesis
    ///         testing, or in construction of confidence intervals.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Chi-square_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Chi-square distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Chi-square_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example demonstrates how to create a new χ²
    ///         distribution with the given degrees of freedom.
    ///     </para>
    ///     <code>
    ///   // Create a new χ² distribution with 7 d.f.
    ///   var chisq = new ChiSquareDistribution(degreesOfFreedom: 7);
    /// 
    ///   // Common measures
    ///   double mean = chisq.Mean;     //  7
    ///   double median = chisq.Median; //  6.345811195595612
    ///   double var = chisq.Variance;  // 14
    /// 
    ///   // Cumulative distribution functions
    ///   double cdf = chisq.DistributionFunction(x: 6.27);           // 0.49139966433823956
    ///   double ccdf = chisq.ComplementaryDistributionFunction(x: 6.27); // 0.50860033566176044
    ///   double icdf = chisq.InverseDistributionFunction(p: cdf);        // 6.2700000000852318
    ///   
    ///   // Probability density functions
    ///   double pdf = chisq.ProbabilityDensityFunction(x: 6.27);     // 0.11388708001184455
    ///   double lpdf = chisq.LogProbabilityDensityFunction(x: 6.27); // -2.1725478476948092
    /// 
    ///   // Hazard (failure rate) functions
    ///   double hf = chisq.HazardFunction(x: 6.27);            // 0.22392254197721179
    ///   double chf = chisq.CumulativeHazardFunction(x: 6.27); // 0.67609276602233315
    ///   
    ///   // String representation
    ///   string str = chisq.ToString(); // "χ²(x; df = 7)
    /// </code>
    /// </example>
    [Serializable]
    public class ChiSquareDistribution : UnivariateContinuousDistribution,
        ISampleableDistribution<double>
    {
        // Distribution parameters

        // Distribution measures
        private double? entropy;

        /// <summary>
        ///     Constructs a new Chi-Square distribution
        ///     with given degrees of freedom.
        /// </summary>
        public ChiSquareDistribution()
            : this(1)
        {
        }

        /// <summary>
        ///     Constructs a new Chi-Square distribution
        ///     with given degrees of freedom.
        /// </summary>
        /// <param name="degreesOfFreedom">The degrees of freedom for the distribution. Default is 1.</param>
        public ChiSquareDistribution([PositiveInteger] int degreesOfFreedom)
        {
            if (degreesOfFreedom <= 0)
            {
                throw new ArgumentOutOfRangeException("degreesOfFreedom",
                    "The number of degrees of freedom must be higher than zero.");
            }

            this.DegreesOfFreedom = degreesOfFreedom;
        }

        /// <summary>
        ///     Gets the Degrees of Freedom for this distribution.
        /// </summary>
        public int DegreesOfFreedom { get; private set; }

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
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <remarks>
        ///     The χ² distribution mean is the number of degrees of freedom.
        /// </remarks>
        public override double Mean
        {
            get => DegreesOfFreedom;
            set => DegreesOfFreedom = (int)value;
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <remarks>
        ///     The χ² distribution variance is twice its degrees of freedom.
        /// </remarks>
        public override double Variance
        {
            get => 2.0 * DegreesOfFreedom;
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <remarks>
        ///     The χ² distribution mode is <c>max(degrees of freedom - 2, 0)</c>.
        /// </remarks>
        /// <value>
        ///     The distribution's mode value.
        /// </value>
        public override double Mode
        {
            get => System.Math.Max(DegreesOfFreedom - 2, 0);
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        public override double Entropy
        {
            get
            {
                if (!entropy.HasValue)
                {
                    var kd2 = DegreesOfFreedom / 2.0;
                    var m1 = System.Math.Log(2.0 * Gamma.Function(kd2));
                    var m2 = (1.0 - kd2) * Gamma.Digamma(kd2);
                    entropy = kd2 + m1 + m2;
                }

                return entropy.Value;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new ChiSquareDistribution(DegreesOfFreedom);
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     the χ² distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The Probability Density Function (PDF) describes the
        ///         probability that a given value <c>x</c> will occur.
        ///     </para>
        ///     <para>
        ///         References:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     <a href="http://en.wikipedia.org/wiki/Chi-square_distribution#Probability_density_function">
        ///                         Wikipedia, the free encyclopedia. Chi-square distribution.
        ///                     </a>
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            double v = DegreesOfFreedom;
            var m1 = System.Math.Pow(x, (v - 2.0) / 2.0);
            var m2 = System.Math.Exp(-x / 2.0);
            var m3 = System.Math.Pow(2, v / 2.0) * Gamma.Function(v / 2.0);
            return m1 * m2 / m3;
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
            double v = DegreesOfFreedom;
            var m1 = (v - 2.0) / 2.0 * System.Math.Log(x);
            var m2 = -x / 2.0;
            var m3 = v / 2.0 * System.Math.Log(2) + Gamma.Log(v / 2.0);
            return m1 + m2 - m3;
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     the χ² distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The Cumulative Distribution Function (CDF) describes the cumulative
        ///         probability that a given value or any value smaller than it will occur.
        ///     </para>
        ///     <para>
        ///         The χ² distribution function is defined in terms of the
        ///         <see cref="Gamma.LowerIncomplete">
        ///             Incomplete Gamma Function Γ(a, x)
        ///         </see>
        ///         as CDF(x; df) = Γ(df/2, x/d).
        ///     </para>
        /// </remarks>
        protected internal override double InnerDistributionFunction(double x)
        {
            return Gamma.LowerIncomplete(DegreesOfFreedom / 2.0, x / 2.0);
        }

        /// <summary>
        ///     Gets the complementary cumulative distribution function
        ///     (ccdf) for the χ² distribution evaluated at point <c>x</c>.
        ///     This function is also known as the Survival function.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The Complementary Cumulative Distribution Function (CCDF) is
        ///         the complement of the Cumulative Distribution Function, or 1
        ///         minus the CDF.
        ///     </para>
        ///     <para>
        ///         The χ² complementary distribution function is defined in terms of the
        ///         <see cref="Gamma.UpperIncomplete">
        ///             Complemented Incomplete Gamma
        ///             Function Γc(a, x)
        ///         </see>
        ///         as CDF(x; df) = Γc(df/2, x/d).
        ///     </para>
        /// </remarks>
        protected internal override double InnerComplementaryDistributionFunction(double x)
        {
            return Gamma.UpperIncomplete(DegreesOfFreedom / 2.0, x / 2.0);
        }

        /// <summary>
        ///     Gets the inverse of the cumulative distribution function (icdf) for
        ///     this distribution evaluated at probability <c>p</c>. This function
        ///     is also known as the Quantile function.
        /// </summary>
        /// <param name="p">A probability value between 0 and 1.</param>
        /// <returns>
        ///     A sample which could original the given probability
        ///     value when applied in the <see cref="UnivariateContinuousDistribution.DistributionFunction(double)" />.
        /// </returns>
        protected internal override double InnerInverseDistributionFunction(double p)
        {
            return Gamma.InverseLowerIncomplete(DegreesOfFreedom / 2.0, p) * 2.0;
        }

        /// <summary>
        ///     This method is not supported.
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
            return string.Format(formatProvider, "χ²(x; df = {0})",
                DegreesOfFreedom.ToString(format, formatProvider));
        }

        /// <summary>
        ///     Gets the inverse of the cumulative distribution function (icdf) for
        ///     this distribution evaluated at probability <c>p</c>. This function
        ///     is also known as the Quantile function.
        /// </summary>
        /// <param name="p">A probability value between 0 and 1.</param>
        /// <param name="degreesOfFreedom">
        ///     The degrees of freedom of the Chi-Square distribution.
        /// </param>
        /// <returns>
        ///     A sample which could original the given probability
        ///     value when applied in the <see cref="UnivariateContinuousDistribution.DistributionFunction(double)" />.
        /// </returns>
        public static double Inverse(double p, int degreesOfFreedom)
        {
            return Gamma.InverseLowerIncomplete(degreesOfFreedom / 2.0, p) * 2.0;
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
            return Random(DegreesOfFreedom, samples, result, source);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random observations drawn from this distribution.</returns>
        public override double Generate(Random source)
        {
            return Random(DegreesOfFreedom, source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Chi-Square distribution with the given parameters.
        /// </summary>
        /// <returns>An array of double values sampled from the specified Chi-Square distribution.</returns>
        public static double[] Random(int degreesOfFreedom, int samples)
        {
            return Random(degreesOfFreedom, samples, Generator.Random);
        }

        /// <summary>
        ///     Generates a random observation from the
        ///     Chi-Square distribution with the given parameters.
        /// </summary>
        /// <param name="degreesOfFreedom">The degrees of freedom for the distribution.</param>
        /// <returns>A random double value sampled from the specified Chi-Square distribution.</returns>
        public static double Random(int degreesOfFreedom)
        {
            return Random(degreesOfFreedom, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Chi-Square distribution with the given parameters.
        /// </summary>
        /// <returns>An array of double values sampled from the specified Chi-Square distribution.</returns>
        public static double[] Random(int degreesOfFreedom, int samples, Random source)
        {
            return GammaDistribution.Random(degreesOfFreedom / 2.0, 2, samples, source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Chi-Square distribution with the given parameters.
        /// </summary>
        /// <returns>An array of double values sampled from the specified Chi-Square distribution.</returns>
        public static double[] Random(int degreesOfFreedom, int samples, double[] result, Random source)
        {
            return GammaDistribution.Random(degreesOfFreedom / 2.0, 2,
                samples, result, source);
        }

        /// <summary>
        ///     Generates a random observation from the
        ///     Chi-Square distribution with the given parameters.
        /// </summary>
        /// <param name="degreesOfFreedom">The degrees of freedom for the distribution.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random double value sampled from the specified Chi-Square distribution.</returns>
        public static double Random(int degreesOfFreedom, Random source)
        {
            return GammaDistribution.Random(degreesOfFreedom / 2.0, 2, source);
        }

        #endregion
    }
}