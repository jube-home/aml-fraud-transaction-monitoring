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
    ///     Hyperbolic Secant distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability theory and statistics, the hyperbolic secant distribution is
    ///         a continuous probability distribution whose probability density function and
    ///         characteristic function are proportional to the hyperbolic secant function.
    ///         The hyperbolic secant function is equivalent to the inverse hyperbolic cosine,
    ///         and thus this distribution is also called the inverse-cosh distribution.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Sech_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Hyperbolic secant distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Sech_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This examples shows how to create a Sech distribution,
    ///         compute some of its properties and generate a number of
    ///         random samples from it.
    ///     </para>
    ///     <code>
    /// // Create a new hyperbolic secant distribution
    /// var sech = new HyperbolicSecantDistribution();
    /// 
    /// double mean = sech.Mean;     // 0.0
    /// double median = sech.Median; // 0.0
    /// double mode = sech.Mode;     // 0.0
    /// double var = sech.Variance;  // 1.0
    /// 
    /// double cdf = sech.DistributionFunction(x: 1.4); // 0.92968538268895873
    /// double pdf = sech.ProbabilityDensityFunction(x: 1.4); // 0.10955386512899701
    /// double lpdf = sech.LogProbabilityDensityFunction(x: 1.4); // -2.2113389316917877
    /// 
    /// double ccdf = sech.ComplementaryDistributionFunction(x: 1.4); // 0.070314617311041272
    /// double icdf = sech.InverseDistributionFunction(p: cdf); // 1.40
    /// 
    /// double hf = sech.HazardFunction(x: 1.4); // 1.5580524977385339
    /// 
    /// string str = sech.ToString(); // Sech(x)
    /// </code>
    /// </example>
    [Serializable]
    public class HyperbolicSecantDistribution : UnivariateContinuousDistribution
    {
        /// <summary>
        ///     Constructs a Hyperbolic Secant (Sech) distribution.
        /// </summary>
        public HyperbolicSecantDistribution()
        {
        }

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
        ///     Gets the variance for this distribution (always one).
        /// </summary>
        /// <value>
        ///     The distribution's variance.
        /// </value>
        public override double Variance
        {
            get => 1;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the Standard Deviation (the square root of
        ///     the variance) for the current distribution.
        /// </summary>
        /// <value>
        ///     The distribution's standard deviation.
        /// </value>
        public override double StandardDeviation => 1;

        /// <summary>
        ///     Gets the mode for this distribution (always zero).
        /// </summary>
        /// <value>
        ///     The distribution's mode value.
        /// </value>
        public override double Mode
        {
            get => 0;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the support interval for this distribution (-inf, +inf).
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
        /// <value>
        ///     The distribution's entropy.
        /// </value>
        public override double Entropy
        {
            get => 4.0 / System.Math.PI * Constants.Catalan;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        protected internal override double InnerDistributionFunction(double x)
        {
            var angle = System.Math.Atan(System.Math.Exp(x * System.Math.PI / 2.0));
            return 2 * angle / System.Math.PI;
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
            return 0.5 * Special.Sech(x * (System.Math.PI / 2.0));
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new HyperbolicSecantDistribution();
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
            return string.Format(formatProvider, "Sech(x)");
        }
    }
}