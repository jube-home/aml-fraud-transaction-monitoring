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
using Accord.Math.Integration;
using Accord.Math.Optimization;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Tukey-Lambda distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Formalized by John Tukey, the Tukey lambda distribution is a continuous
    ///         probability distribution defined in terms of its quantile function. It is
    ///         typically used to identify an appropriate distribution and not used in
    ///         statistical models directly.
    ///     </para>
    ///     <para>
    ///         The Tukey lambda distribution has a single shape parameter λ. As with other
    ///         probability distributions, the Tukey lambda distribution can be transformed
    ///         with a location parameter, μ, and a scale parameter, σ. Since the general form
    ///         of probability distribution can be expressed in terms of the standard distribution,
    ///         the subsequent formulas are given for the standard form of the function.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Tukey_lambda_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Tukey-Lambda distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Tukey_lambda_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This examples shows how to create a Tukey distribution and
    ///         compute some of its properties .
    ///     </para>
    ///     <code>
    /// var tukey = new TukeyLambdaDistribution(lambda: 0.14);
    /// 
    /// double mean = tukey.Mean;     // 0.0
    /// double median = tukey.Median; // 0.0
    /// double mode = tukey.Mode;     // 0.0
    /// double var = tukey.Variance;  // 2.1102970222144855
    /// double stdDev = tukey.StandardDeviation;  // 1.4526861402982014
    /// 
    /// double cdf = tukey.DistributionFunction(x: 1.4); // 0.83252947230217966
    /// double pdf = tukey.ProbabilityDensityFunction(x: 1.4); // 0.17181242109370659
    /// double lpdf = tukey.LogProbabilityDensityFunction(x: 1.4); // -1.7613519723149427
    /// 
    /// double ccdf = tukey.ComplementaryDistributionFunction(x: 1.4); // 0.16747052769782034
    /// double icdf = tukey.InverseDistributionFunction(p: cdf); // 1.4000000000000004
    /// 
    /// double hf = tukey.HazardFunction(x: 1.4); // 1.0219566231014163
    /// double chf = tukey.CumulativeHazardFunction(x: 1.4); // 1.7842102556452939
    /// 
    /// string str = tukey.ToString(CultureInfo.InvariantCulture); // Tukey(x; λ = 0.14)
    /// </code>
    /// </example>
    /// <seealso cref="LogisticDistribution" />
    /// <seealso cref="UniformContinuousDistribution" />
    /// <seealso cref="NormalDistribution" />
    [Serializable]
    public class TukeyLambdaDistribution : UnivariateContinuousDistribution
    {
        // Distribution parameters

        /// <summary>
        ///     Constructs a Tukey-Lambda distribution
        ///     with the given lambda (shape) parameter.
        /// </summary>
        public TukeyLambdaDistribution([Real] double lambda)
        {
            initialize(lambda);
        }

        /// <summary>
        ///     Gets the distribution shape parameter lambda (λ).
        /// </summary>
        public double Lambda { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution (always zero).
        /// </summary>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Mean
        {
            get => 0;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the median for this distribution (always zero).
        /// </summary>
        /// <value>
        ///     The distribution's median value.
        /// </value>
        public override double Median => 0;

        /// <summary>
        ///     Gets the mode for this distribution (always zero).
        /// </summary>
        /// <value>
        ///     The distribution's median value.
        /// </value>
        public override double Mode
        {
            get => 0;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's entropy.
        /// </value>
        public override double Entropy
        {
            get
            {
                var lower = 1e-15;
                var upper = 1.0 - 1e-15;

                return NonAdaptiveGaussKronrod
                    .Integrate(LogQuantileDensityFunction, lower, upper);
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's variance.
        /// </value>
        public override double Variance
        {
            get
            {
                if (Lambda == 0)
                {
                    return System.Math.PI * System.Math.PI / 3.0;
                }

                var a = 2.0 / (Lambda * Lambda);
                var b = 1 / (1 + 2 * Lambda);
                var c = Gamma.Function(Lambda + 1);
                var d = Gamma.Function(2 * Lambda + 2);
                return a * (b - c * c / d);
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
            get
            {
                if (Lambda > 0)
                {
                    return new DoubleRange(-1 / Lambda, 1 / Lambda);
                }

                return new DoubleRange(double.NegativeInfinity, double.PositiveInfinity);
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        protected internal override double InnerDistributionFunction(double x)
        {
            return BrentSearch.Find(InverseDistributionFunction, x, 0, 1, 1e-10);
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
            if (Lambda == 0)
            {
                return System.Math.Log(p / (1 - p));
            }

            var a = System.Math.Pow(p, Lambda);
            var b = System.Math.Pow(1 - p, Lambda);

            return (a - b) / Lambda;
        }

        /// <summary>
        ///     Gets the first derivative of the
        ///     <see cref="UnivariateContinuousDistribution.InverseDistributionFunction(double)">
        ///         inverse distribution function
        ///     </see>
        ///     (icdf) for this distribution evaluated
        ///     at probability <c>p</c>.
        /// </summary>
        /// <param name="p">A probability value between 0 and 1.</param>
        public override double QuantileDensityFunction(double p)
        {
            return System.Math.Pow(p, Lambda - 1) + System.Math.Pow(1 - p, Lambda - 1);
        }

        /// <summary>
        ///     Gets the log of the
        ///     <see cref="QuantileDensityFunction">
        ///         quantile
        ///         density function
        ///     </see>
        ///     , which in turn is the first derivative of
        ///     the
        ///     <see cref="UnivariateContinuousDistribution.InverseDistributionFunction(double)">
        ///         inverse distribution
        ///         function
        ///     </see>
        ///     (icdf), evaluated at probability <c>p</c>.
        /// </summary>
        /// <param name="p">A probability value between 0 and 1.</param>
        public double LogQuantileDensityFunction(double p)
        {
            var a = System.Math.Pow(p, Lambda - 1);
            var b = System.Math.Pow(1 - p, Lambda - 1);
            return System.Math.Log(a + b);
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
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            // http://www.ism.ac.jp/editsec/aism/pdf/044_4_0721.pdf
            return 1.0 / QuantileDensityFunction(DistributionFunction(x));
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
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            return -LogQuantileDensityFunction(DistributionFunction(x));
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new TukeyLambdaDistribution(Lambda);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Tukey(x; λ = {0})",
                Lambda.ToString(format, formatProvider));
        }

        private void initialize(double lambda)
        {
            this.Lambda = lambda;
        }
    }
}