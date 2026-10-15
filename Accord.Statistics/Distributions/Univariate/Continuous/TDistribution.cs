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
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Student's t-distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability and statistics, Student's t-distribution (or simply the
    ///         t-distribution) is a family of continuous probability distributions that
    ///         arises when estimating the mean of a normally distributed population in
    ///         situations where the sample size is small and population standard deviation
    ///         is unknown. It plays a role in a number of widely used statistical analyses,
    ///         including the Student's t-test for assessing the statistical significance of
    ///         the difference between two sample means, the construction of confidence intervals
    ///         for the difference between two population means, and in linear regression
    ///         analysis. The Student's t-distribution also arises in the Bayesian analysis of
    ///         data from a normal family.
    ///     </para>
    ///     <para>
    ///         If we take <c>k</c> samples from a normal distribution with fixed unknown mean and
    ///         variance, and if we compute the sample mean and sample variance for these k
    ///         samples, then the t-distribution (for k) can be defined as the distribution
    ///         of the location of the true mean, relative to the sample mean and divided by
    ///         the sample standard deviation, after multiplying by the normalizing term
    ///         <c>sqrt(n)</c>, where <c>n</c> is the sample size. In this way the t-distribution
    ///         can be used to estimate how likely it is that the true mean lies in any given
    ///         range.
    ///     </para>
    ///     <para>
    ///         The t-distribution is symmetric and bell-shaped, like the normal distribution,
    ///         but has heavier tails, meaning that it is more prone to producing values that
    ///         fall far from its mean. This makes it useful for understanding the statistical
    ///         behavior of certain types of ratios of random quantities, in which variation in
    ///         the denominator is amplified and may produce outlying values when the denominator
    ///         of the ratio falls close to zero. The Student's t-distribution is a special case
    ///         of the generalized hyperbolic distribution.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Student's_t-distribution">
    ///                         Wikipedia, The Free Encyclopedia. Student's t-distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Student's_t-distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // Create a new Student's T distribution with d.f = 4.2
    ///   TDistribution t = new TDistribution(degreesOfFreedom: 4.2);
    ///   
    ///   // Common measures
    ///   double mean = t.Mean;     // 0.0
    ///   double median = t.Median; // 0.0
    ///   double var = t.Variance;  // 1.9090909090909089
    ///   
    ///   // Cumulative distribution functions
    ///   double cdf = t.DistributionFunction(x: 1.4);           // 0.88456136730659074
    ///   double pdf = t.ProbabilityDensityFunction(x: 1.4);     // 0.13894002185341031
    ///   double lpdf = t.LogProbabilityDensityFunction(x: 1.4); // -1.9737129364307417
    ///   
    ///   // Probability density functions
    ///   double ccdf = t.ComplementaryDistributionFunction(x: 1.4); // 0.11543863269340926
    ///   double icdf = t.InverseDistributionFunction(p: cdf);       // 1.4000000000000012
    ///   
    ///   // Hazard (failure rate) functions
    ///   double hf = t.HazardFunction(x: 1.4);            // 1.2035833984833988
    ///   double chf = t.CumulativeHazardFunction(x: 1.4); // 2.1590162088918525
    ///   
    ///   // String representation
    ///   string str = t.ToString(CultureInfo.InvariantCulture); // T(x; df = 4.2)
    /// </code>
    /// </example>
    /// <seealso cref="Testing.TTest" />
    /// <seealso cref="NoncentralTDistribution" />
    [Serializable]
    public class TDistribution : UnivariateContinuousDistribution, IFormattable
    {
        private double lnconstant;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TDistribution" /> class.
        /// </summary>
        /// <param name="degreesOfFreedom">The degrees of freedom.</param>
        public TDistribution([Positive(1)] double degreesOfFreedom)
        {
            if (degreesOfFreedom < 1)
            {
                throw new ArgumentOutOfRangeException("degreesOfFreedom");
            }

            DegreesOfFreedom = degreesOfFreedom;

            var v = degreesOfFreedom;

            var num = Gamma.Log((v + 1) / 2.0);
            var den = 0.5 * System.Math.Log(v * System.Math.PI) + Gamma.Log(v / 2.0);
            lnconstant = num - den;
        }

        /// <summary>
        ///     Gets the degrees of freedom for the distribution.
        /// </summary>
        public double DegreesOfFreedom { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>The distribution's mean value.</value>
        /// <remarks>
        ///     In the T Distribution, the mean is zero if the number of degrees
        ///     of freedom is higher than 1. Otherwise, it is undefined.
        /// </remarks>
        public override double Mean
        {
            get => DegreesOfFreedom > 1 ? 0 : double.NaN;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the mode for this distribution (always zero).
        /// </summary>
        /// <value>
        ///     The distribution's mode value (zero).
        /// </value>
        public override double Mode
        {
            get => 0;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        public override double Variance
        {
            get
            {
                if (DegreesOfFreedom > 2)
                {
                    return DegreesOfFreedom / (DegreesOfFreedom - 2);
                }

                if (DegreesOfFreedom > 1)
                {
                    return double.PositiveInfinity;
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
            get => new(double.NegativeInfinity, double.PositiveInfinity);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("T(x; df = {0})",
                DegreesOfFreedom.ToString(format, formatProvider));
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        /// <example>
        ///     See <see cref="TDistribution" />.
        /// </example>
        protected internal override double InnerDistributionFunction(double x)
        {
            var v = DegreesOfFreedom;
            var sqrt = System.Math.Sqrt(x * x + v);
            var u = (x + sqrt) / (2 * sqrt);
            return Beta.Incomplete(v / 2.0, v / 2.0, u);
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
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
        /// <example>
        ///     See <see cref="TDistribution" />.
        /// </example>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            return System.Math.Exp(LogProbabilityDensityFunction(x));
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
        /// <example>
        ///     See <see cref="TDistribution" />.
        /// </example>
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            return lnconstant - (DegreesOfFreedom + 1) / 2.0 * Special.Log1p(x * x / DegreesOfFreedom);
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
        /// <example>
        ///     See <see cref="TDistribution" />.
        /// </example>
        protected internal override double InnerInverseDistributionFunction(double p)
        {
            return inverseDistributionLeftTail(DegreesOfFreedom, p);
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new TDistribution(DegreesOfFreedom);
        }

        /// <summary>
        ///     Gets the inverse of the cumulative distribution function (icdf) for
        ///     the left tail T-distribution evaluated at probability <c>p</c>.
        /// </summary>
        /// <remarks>
        ///     Based on the stdtril function from the Cephes Math Library
        ///     Release 2.8, adapted with permission of Stephen L. Moshier.
        /// </remarks>
        private static double inverseDistributionLeftTail(double df, double p)
        {
            if (p == 0)
            {
                return double.NegativeInfinity;
            }

            if (p == 1)
            {
                return double.PositiveInfinity;
            }

            if (p > 0.25 && p < 0.75)
            {
                if (p == 0.5)
                {
                    return 0;
                }

                var u = 1.0 - 2.0 * p;
                var z = Beta.IncompleteInverse(0.5, 0.5 * df, System.Math.Abs(u));
                var t = System.Math.Sqrt(df * z / (1.0 - z));

                if (p < 0.5)
                {
                    t = -t;
                }

                return t;
            }
            else
            {
                var rflg = -1;

                if (p >= 0.5)
                {
                    p = 1.0 - p;
                    rflg = 1;
                }

                var z = Beta.IncompleteInverse(0.5 * df, 0.5, 2.0 * p);

                if (double.MaxValue * z < df)
                {
                    return rflg * double.MaxValue;
                }

                var t = System.Math.Sqrt(df / z - df);
                return rflg * t;
            }
        }
    }
}