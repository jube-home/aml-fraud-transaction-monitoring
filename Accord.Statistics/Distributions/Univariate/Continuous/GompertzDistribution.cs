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
    ///     Gompertz distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Gompertz distribution is a continuous probability distribution. The
    ///         Gompertz distribution is often applied to describe the distribution of
    ///         adult lifespans by demographers and actuaries. Related fields of science
    ///         such as biology and gerontology also considered the Gompertz distribution
    ///         for the analysis of survival. More recently, computer scientists have also
    ///         started to model the failure rates of computer codes by the Gompertz
    ///         distribution. In marketing science, it has been used as an individual-level
    ///         model of customer lifetime.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Gamma_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Gompertz distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Gompertz_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to construct a Gompertz
    ///         distribution with <c>η = 4.2</c> and <c>b = 1.1</c>.
    ///     </para>
    ///     <code>
    ///   // Create a new Gompertz distribution with η = 4.2 and b = 1.1
    ///   GompertzDistribution dist = new GompertzDistribution(eta: 4.2, b: 1.1);
    ///   
    ///   // Common measures
    ///   double median = dist.Median; // 0.13886469671401389
    ///   
    ///   // Cumulative distribution functions
    ///   double cdf = dist.DistributionFunction(x: 0.27); // 0.76599768199799145
    ///   double ccdf = dist.ComplementaryDistributionFunction(x: 0.27); // 0.23400231800200855
    ///   double icdf = dist.InverseDistributionFunction(p: cdf); // 0.26999999999766749
    ///   
    ///   // Probability density functions
    ///   double pdf = dist.ProbabilityDensityFunction(x: 0.27); // 1.4549484164912097
    ///   double lpdf = dist.LogProbabilityDensityFunction(x: 0.27); // 0.37497044741163688
    ///   
    ///   // Hazard (failure rate) functions
    ///   double hf = dist.HazardFunction(x: 0.27); // 6.2176666834502088
    ///   double chf = dist.CumulativeHazardFunction(x: 0.27); // 1.4524242576820101
    ///   
    ///   // String representation
    ///   string str = dist.ToString(CultureInfo.InvariantCulture); // "Gompertz(x; η = 4.2, b = 1.1)"
    /// </code>
    /// </example>
    public class GompertzDistribution : UnivariateContinuousDistribution
    {
        private readonly double b;
        private readonly double eta;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GompertzDistribution" /> class.
        /// </summary>
        /// <param name="eta">The shape parameter <c>η</c>.</param>
        /// <param name="b">The scale parameter <c>b</c>.</param>
        public GompertzDistribution([Positive] double eta, [Positive] double b)
        {
            if (eta <= 0)
            {
                throw new ArgumentOutOfRangeException("eta", "The shape eta must be positive.");
            }

            if (b <= 0)
            {
                throw new ArgumentOutOfRangeException("b", "The scale b must be positive.");
            }

            this.eta = eta;
            this.b = b;
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Mean
        {
            get => throw new NotSupportedException();
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Variance
        {
            get => throw new NotSupportedException();
            set { } //Not Implemented.
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
                if (eta >= 1)
                {
                    return 0;
                }

                return 1 / b * System.Math.Log(1 / eta);
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the median for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's median value.
        /// </value>
        public override double Median => 1.0 / b * System.Math.Log(-1 / eta * System.Math.Log(0.5) + 1);

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
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
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        protected internal override double InnerDistributionFunction(double x)
        {
            var ebx = System.Math.Exp(b * x);

            var cdf = 1.0 - System.Math.Exp(-eta * (ebx - 1.0));

            return cdf;
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
            var a1 = b * eta * System.Math.Exp(eta);
            var a2 = System.Math.Exp(b * x);
            var a3 = System.Math.Exp(-eta * System.Math.Exp(b * x));
            return a1 * a2 * a3;
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
            var a1 = System.Math.Log(b * eta) + eta;
            var a2 = b * x;
            var a3 = -eta * System.Math.Exp(b * x);
            return a1 + a2 + a3;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new GompertzDistribution(eta, b);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Gompertz(x; η = {0}, b = {1})",
                eta.ToString(format, formatProvider),
                b.ToString(format, formatProvider));
        }
    }
}