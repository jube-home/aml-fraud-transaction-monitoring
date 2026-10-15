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
using Accord.Diagnostics;
using Accord.Math;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Logistic distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability theory and statistics, the logistic distribution is a continuous
    ///         probability distribution. Its cumulative distribution function is the logistic
    ///         function, which appears in logistic regression and feedforward neural networks.
    ///         It resembles the normal distribution in shape but has heavier tails (higher
    ///         kurtosis). The <see cref="TukeyLambdaDistribution">Tukey lambda distribution</see>
    ///         can be considered a generalization of the logistic distribution since it adds a
    ///         shape parameter, λ (the Tukey distribution becomes logistic when λ is zero).
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Logistic_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Logistic distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Logistic_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This examples shows how to create a Logistic distribution,
    ///         compute some of its properties and generate a number of
    ///         random samples from it.
    ///     </para>
    ///     <code>
    /// // Create a logistic distribution with μ = 0.42 and scale = 3
    /// var log = new LogisticDistribution(location: 0.42, scale: 1.2);
    /// 
    /// double mean = log.Mean;     // 0.42
    /// double median = log.Median; // 0.42
    /// double mode = log.Mode;     // 0.42
    /// double var = log.Variance;  // 4.737410112522892
    /// 
    /// double cdf = log.DistributionFunction(x: 1.4); // 0.693528308197921
    /// double pdf = log.ProbabilityDensityFunction(x: 1.4); // 0.17712232827170876
    /// double lpdf = log.LogProbabilityDensityFunction(x: 1.4); // -1.7309146649427332
    /// 
    /// double ccdf = log.ComplementaryDistributionFunction(x: 1.4); // 0.306471691802079
    /// double icdf = log.InverseDistributionFunction(p: cdf); // 1.3999999999999997
    /// 
    /// double hf = log.HazardFunction(x: 1.4); // 0.57794025683160088
    /// double chf = log.CumulativeHazardFunction(x: 1.4); // 1.1826298874077226
    /// 
    /// string str = log.ToString(CultureInfo.InvariantCulture); // Logistic(x; μ = 0.42, scale = 1.2)
    /// </code>
    /// </example>
    /// <seealso cref="TukeyLambdaDistribution" />
    [Serializable]
    public class LogisticDistribution : UnivariateContinuousDistribution
    {
        // Distribution parameters

        /// <summary>
        ///     Constructs a Logistic distribution
        ///     with zero location and unit scale.
        /// </summary>
        public LogisticDistribution()
        {
            initialize(0, 1);
        }

        /// <summary>
        ///     Constructs a Logistic distribution
        ///     with given location and unit scale.
        /// </summary>
        /// <param name="location">The distribution's location value μ (mu).</param>
        public LogisticDistribution([Real] double location)
        {
            initialize(location, 1);
        }

        /// <summary>
        ///     Constructs a Logistic distribution
        ///     with given location and scale parameters.
        /// </summary>
        /// <param name="location">The distribution's location value μ (mu).</param>
        /// <param name="scale">The distribution's scale value s.</param>
        public LogisticDistribution([Real] double location, [Positive] double scale)
        {
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale", "Scale must be positive.");
            }

            initialize(location, scale);
        }

        /// <summary>
        ///     Gets the location value μ (mu).
        /// </summary>
        public double Location { get; private set; }

        /// <summary>
        ///     Gets the location value μ (mu).
        /// </summary>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Mean
        {
            get => Location;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the distribution's scale value (s).
        /// </summary>
        public double Scale { get; private set; }

        /// <summary>
        ///     Gets the median for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's median value.
        /// </value>
        public override double Median
        {
            get
            {
                Debug.Assert(Location == base.Median);
                return Location;
            }
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's variance.
        /// </value>
        public override double Variance
        {
            get => Scale * Scale * System.Math.PI * System.Math.PI / 3.0;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <remarks>
        ///     In the logistic distribution, the mode is equal
        ///     to the distribution <see cref="Mean" /> value.
        /// </remarks>
        /// <value>
        ///     The distribution's mode value.
        /// </value>
        public override double Mode
        {
            get => Location;
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
        /// <remarks>
        ///     In the logistic distribution, the entropy is
        ///     equal to <c>ln(<see cref="Scale">s</see>) + 2</c>.
        /// </remarks>
        /// <value>
        ///     The distribution's entropy.
        /// </value>
        public override double Entropy
        {
            get => System.Math.Log(Scale) + 2;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        protected internal override double InnerDistributionFunction(double x)
        {
            var z = (x - Location) / Scale;

            return 1.0 / (1 + System.Math.Exp(-z));
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
            var z = (x - Location) / Scale;

            var num = System.Math.Exp(-z);
            var a = 1 + num;
            var den = Scale * a * a;

            return num / den;
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
            var z = (x - Location) / Scale;

            var result = -z - (System.Math.Log(Scale) + 2 * Special.Log1p(System.Math.Exp(-z)));

            return result;
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
            return Location + Scale * System.Math.Log(p / (1 - p));
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
            return Scale / (p * (1 - p));
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new LogisticDistribution(Location, Scale);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Logistic(x; μ = {0}, s = {1})",
                Location.ToString(format, formatProvider),
                Scale.ToString(format, formatProvider));
        }

        private void initialize(double mean, double scale)
        {
            Location = mean;
            Scale = scale;
        }
    }
}