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
    ///     Laplace's Distribution (as known as the double exponential distribution).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability theory and statistics, the Laplace distribution is a continuous
    ///         probability distribution named after Pierre-Simon Laplace. It is also sometimes called
    ///         the double exponential distribution.
    ///     </para>
    ///     <para>
    ///         The difference between two independent identically distributed exponential random
    ///         variables is governed by a Laplace distribution, as is a Brownian motion evaluated at an
    ///         exponentially distributed random time. Increments of Laplace motion or a variance gamma
    ///         process evaluated over the time scale also have a Laplace distribution.
    ///     </para>
    ///     <para>
    ///         The probability density function of the Laplace distribution is also reminiscent of the
    ///         normal distribution; however, whereas the normal distribution is expressed in terms of
    ///         the squared difference from the mean μ, the Laplace density is expressed in terms of the
    ///         absolute difference from the mean. Consequently the Laplace distribution has fatter tails
    ///         than the normal distribution.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Laplace_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Laplace distribution.
    ///                         Available from: http://en.wikipedia.org/wiki/Laplace_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///    // Create a new Laplace distribution with μ = 4 and b = 2
    ///    var laplace = new LaplaceDistribution(location: 4, scale: 2);
    ///    
    ///    // Common measures
    ///    double mean = laplace.Mean;     // 4.0
    ///    double median = laplace.Median; // 4.0
    ///    double var = laplace.Variance;  // 8.0
    ///    
    ///    // Cumulative distribution functions
    ///    double cdf = laplace.DistributionFunction(x: 0.27);               // 0.077448104942453522
    ///    double ccdf = laplace.ComplementaryDistributionFunction(x: 0.27); // 0.92255189505754642
    ///    double icdf = laplace.InverseDistributionFunction(p: cdf);        // 0.27
    ///    
    ///    // Probability density functions
    ///    double pdf = laplace.ProbabilityDensityFunction(x: 0.27);     //  0.038724052471226761
    ///    double lpdf = laplace.LogProbabilityDensityFunction(x: 0.27); // -3.2512943611198906
    ///    
    ///    // Hazard (failure rate) functions
    ///    double hf = laplace.HazardFunction(x: 0.27); // 0.041974931360160776
    ///    double chf = laplace.CumulativeHazardFunction(x: 0.27); // 0.080611649844768624
    ///     
    ///    // String representation
    ///    string str = laplace.ToString(CultureInfo.InvariantCulture); // Laplace(x; μ = 4, b = 2)
    /// </code>
    /// </example>
    public class LaplaceDistribution : UnivariateContinuousDistribution, IFormattable
    {
        private readonly double b; // scale parameter b

        private readonly double constant; // 1 / (2 * b)
        private readonly double u; // location parameter μ (mu)

        /// <summary>
        ///     Creates a new Laplace distribution.
        /// </summary>
        /// <param name="location">The location parameter μ (mu).</param>
        /// <param name="scale">The scale parameter b.</param>
        public LaplaceDistribution([Real] double location, [Positive] double scale)
        {
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale", "Scale must be non-negative.");
            }

            u = location;
            b = scale;

            constant = 1.0 / (2.0 * b);
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>The distribution's mean value.</value>
        /// <remarks>
        ///     The Laplace's distribution mean has the
        ///     same value as the location parameter μ.
        /// </remarks>
        public override double Mean
        {
            get => u;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the mode for this distribution (μ).
        /// </summary>
        /// <remarks>
        ///     The Laplace's distribution mode has the
        ///     same value as the location parameter μ.
        /// </remarks>
        public override double Mode
        {
            get => u;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the median for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's median value.
        /// </value>
        /// <remarks>
        ///     The Laplace's distribution median has the
        ///     same value as the location parameter μ.
        /// </remarks>
        public override double Median => u;

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>The distribution's variance.</value>
        /// <remarks>
        ///     The Laplace's variance is computed as 2*b².
        /// </remarks>
        public override double Variance
        {
            get => 2 * b * b;
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
        ///     Gets the entropy for this distribution.
        /// </summary>
        /// <value>The distribution's entropy.</value>
        /// <remarks>
        ///     The Laplace's entropy is defined as ln(2*e*b), in which
        ///     <c>e</c> is the <see cref="Math.E">Euler constant</see>.
        /// </remarks>
        public override double Entropy
        {
            get => System.Math.Log(2 * System.Math.E * b);
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
            return string.Format(formatProvider, "Laplace(x; μ = {0}, b = {1})",
                u.ToString(format, formatProvider),
                b.ToString(format, formatProvider));
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
        ///     See <see cref="LaplaceDistribution" />.
        /// </example>
        protected internal override double InnerDistributionFunction(double x)
        {
            return 0.5 * (1 + System.Math.Sign(x - u) * (1 - System.Math.Exp(-System.Math.Abs(x - u) / b)));
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
        ///     See <see cref="LaplaceDistribution" />.
        /// </example>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            return constant * System.Math.Exp(-System.Math.Abs(x - u) / b);
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
        ///     See <see cref="LaplaceDistribution" />.
        /// </example>
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            return System.Math.Log(constant) - System.Math.Abs(x - u) / b;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new LaplaceDistribution(u, b);
        }
    }
}