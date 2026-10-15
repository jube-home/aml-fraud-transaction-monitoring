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

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Inverse Gamma Distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The inverse gamma distribution is a two-parameter family of continuous probability
    ///         distributions on the positive real line, which is the distribution of the reciprocal
    ///         of a variable distributed according to the gamma distribution. Perhaps the chief use
    ///         of the inverse gamma distribution is in Bayesian statistics, where it serves as the
    ///         conjugate prior of the variance of a normal distribution. However, it is common among
    ///         Bayesians to consider an alternative parameterization of the normal distribution in
    ///         terms of the precision, defined as the reciprocal of the variance, which allows the
    ///         gamma distribution to be used directly as a conjugate prior.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Inverse-gamma_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Inverse Gamma Distribution.
    ///                         Available from: http://en.wikipedia.org/wiki/Inverse-gamma_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.johndcook.com/inverse gamma.pdf">
    ///                         John D. Cook. (2008). The Inverse Gamma Distribution.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // Create a new inverse Gamma distribution with α = 0.42 and β = 0.5
    ///   var invGamma = new InverseGammaDistribution(shape: 0.42, scale: 0.5);
    /// 
    ///   // Common measures
    ///   double mean   = invGamma.Mean;     // -0.86206896551724133
    ///   double median = invGamma.Median;   // 3.1072323347401709
    ///   double var    = invGamma.Variance; // -0.47035626665061164
    ///    
    ///   // Cumulative distribution functions
    ///   double cdf = invGamma.DistributionFunction(x: 0.27);           // 0.042243552114989695
    ///   double ccdf = invGamma.ComplementaryDistributionFunction(x: 0.27); // 0.95775644788501035
    ///   double icdf = invGamma.InverseDistributionFunction(p: cdf);        // 0.26999994629410995
    ///   
    ///   // Probability density functions
    ///   double pdf = invGamma.ProbabilityDensityFunction(x: 0.27);     // 0.35679850067181362
    ///   double lpdf = invGamma.LogProbabilityDensityFunction(x: 0.27); // -1.0305840804381006
    ///   
    ///   // Hazard (failure rate) functions
    ///   double hf = invGamma.HazardFunction(x: 0.27); // 0.3725357333377633
    ///   double chf = invGamma.CumulativeHazardFunction(x: 0.27); // 0.043161763098266373
    ///   
    ///   // String representation
    ///   string str = invGamma.ToString(); // Γ^(-1)(x; α = 0.42, β = 0.5)
    /// </code>
    /// </example>
    /// <seealso cref="GammaDistribution" />
    [Serializable]
    public class InverseGammaDistribution : UnivariateContinuousDistribution
    {
        private double a;
        private double b;

        private double constant;

        /// <summary>
        ///     Creates a new Inverse Gamma Distribution.
        /// </summary>
        /// <param name="shape">The shape parameter α (alpha).</param>
        /// <param name="scale">The scale parameter β (beta).</param>
        public InverseGammaDistribution([Positive] double shape, [Positive] double scale)
        {
            if (shape <= 0)
            {
                throw new ArgumentOutOfRangeException("shape");
            }

            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale");
            }

            a = shape;
            b = scale;

            constant = System.Math.Pow(b, a) / Gamma.Function(a);
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>The distribution's mean value.</value>
        /// <remarks>
        ///     In the Inverse Gamma distribution, the Mean is given as b / (a - 1).
        /// </remarks>
        public override double Mean
        {
            get => b / (a - 1);
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
            get => b / (a + 1);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>The distribution's variance.</value>
        /// <remarks>
        ///     In the Inverse Gamma distribution, the Variance is given as b² / ((a - 1)² * (a - 2)).
        /// </remarks>
        public override double Variance
        {
            get => b * b / ((a - 1) * (a - 1) * (a - 2));
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
            get => new(double.Epsilon, double.PositiveInfinity);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        /// <value>The distribution's entropy.</value>
        public override double Entropy
        {
            get => a + System.Math.Log(b * Gamma.Function(a)) - (1 + a) * Gamma.Digamma(a);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     <para>
        ///         The Cumulative Distribution Function (CDF) describes the cumulative
        ///         probability that a given value or any value smaller than it will occur.
        ///     </para>
        ///     <para>
        ///         In the Inverse Gamma CDF is computed in terms of the
        ///         <see cref="Gamma.UpperIncomplete">
        ///             Upper Incomplete Regularized Gamma Function Q
        ///         </see>
        ///         as CDF(x) = Q(a, b / x).
        ///     </para>
        /// </remarks>
        /// <example>
        ///     See <see cref="InverseGammaDistribution" />.
        /// </example>
        protected internal override double InnerDistributionFunction(double x)
        {
            return Gamma.UpperIncomplete(a, b / x);
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
        ///     See <see cref="InverseGammaDistribution" />.
        /// </example>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            return constant * System.Math.Pow(x, -a - 1) * System.Math.Exp(-b / x);
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
        ///     See <see cref="InverseGammaDistribution" />.
        /// </example>
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            var lnx = System.Math.Log(x);
            return System.Math.Log(constant) + (-a - 1) * lnx + -b / x;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new InverseGammaDistribution(a, b);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Γ^(-1)(x; α = {0}, β = {1})",
                a.ToString(format, formatProvider),
                b.ToString(format, formatProvider));
        }
    }
}