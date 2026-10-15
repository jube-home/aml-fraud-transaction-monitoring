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
    ///     Skew Normal distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability theory and statistics, the skew normal distribution is a
    ///         <see cref="UnivariateContinuousDistribution">continuous probability distribution</see>
    ///         that generalises the <see cref="NormalDistribution">normal distribution</see> to allow
    ///         for non-zero <see cref="NormalDistribution.Skewness">skewness</see>.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="https://en.wikipedia.org/wiki/Skew_normal_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Skew normal distribution. Available on:
    ///                         https://en.wikipedia.org/wiki/Skew_normal_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This examples shows how to create a Skew normal distribution
    ///         and compute some of its properties and derived measures.
    ///     </para>
    ///     <code>
    /// // Create a Skew normal distribution with location 2, scale 3 and shape 4.2
    /// var skewNormal = new SkewNormalDistribution(location: 2, scale: 3, shape: 4.2);
    /// 
    /// double mean = skewNormal.Mean;     // 4.3285611780515953
    /// double median = skewNormal.Median; // 4.0230040653062265
    /// double var = skewNormal.Variance;  // 3.5778028400709641
    /// double mode = skewNormal.Mode;     // 3.220622226764422
    /// 
    /// double cdf = skewNormal.DistributionFunction(x: 1.4); // 0.020166854942526125
    /// double pdf = skewNormal.ProbabilityDensityFunction(x: 1.4); // 0.052257431834162059
    /// double lpdf = skewNormal.LogProbabilityDensityFunction(x: 1.4); // -2.9515731621912877
    /// 
    /// double ccdf = skewNormal.ComplementaryDistributionFunction(x: 1.4); // 0.97983314505747388
    /// double icdf = skewNormal.InverseDistributionFunction(p: cdf); // 1.3999998597203041
    /// 
    /// double hf = skewNormal.HazardFunction(x: 1.4); // 0.053332990517581239
    /// double chf = skewNormal.CumulativeHazardFunction(x: 1.4); // 0.020372981958858238
    /// 
    /// string str = skewNormal.ToString(CultureInfo.InvariantCulture); // Sn(x; ξ = 2, ω = 3, α = 4.2)
    /// </code>
    /// </example>
    /// <seealso cref="NormalDistribution" />
    /// <seealso cref="Multivariate.MultivariateNormalDistribution" />
    [Serializable]
    public class SkewNormalDistribution : UnivariateContinuousDistribution
    {
        // Derived parameters
        private double delta;

        // Distribution parameters

        /// <summary>
        ///     Constructs a Skew normal distribution with
        ///     zero location, unit scale and zero shape.
        /// </summary>
        public SkewNormalDistribution()
        {
            initialize(Location, Scale, Shape);
        }

        /// <summary>
        ///     Constructs a Skew normal distribution with
        ///     given location, unit scale and zero skewness.
        /// </summary>
        /// <param name="location">The distribution's location value ξ (ksi).</param>
        public SkewNormalDistribution([Real] double location)
        {
            initialize(location, Scale, Shape);
        }

        /// <summary>
        ///     Constructs a Skew normal distribution with
        ///     given location and scale and zero skewness.
        /// </summary>
        /// <param name="location">The distribution's location value ξ (ksi).</param>
        /// <param name="scale">The distribution's scale value ω (omega).</param>
        public SkewNormalDistribution([Real] double location, [Positive] double scale)
        {
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale", "Scale must be positive.");
            }

            initialize(location, scale, Shape);
        }

        /// <summary>
        ///     Constructs a Skew normal distribution
        ///     with given mean and standard deviation.
        /// </summary>
        /// <param name="location">The distribution's location value ξ (ksi).</param>
        /// <param name="scale">The distribution's scale value ω (omega).</param>
        /// <param name="shape">The distribution's shape value α (alpha).</param>
        public SkewNormalDistribution([Real] double location, [Positive] double scale, [Real] double shape)
        {
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale", "Scale must be positive.");
            }

            initialize(location, scale, shape);
        }

        /// <summary>
        ///     Gets the skew-normal distribution's location value  ξ (ksi).
        /// </summary>
        public double Location { get; private set; }

        /// <summary>
        ///     Gets the skew-normal distribution's scale value ω (omega).
        /// </summary>
        public double Scale { get; private set; } = 1;

        /// <summary>
        ///     Gets the skew-normal distribution's shape value α (alpha).
        /// </summary>
        public double Shape { get; private set; }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Mean
        {
            get => Location + Scale * delta * System.Math.Sqrt(2.0 / System.Math.PI);
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
            get => Scale * Scale * (1 - 2 * delta * delta / System.Math.PI);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the skewness for this distribution.
        /// </summary>
        public double Skewness
        {
            get
            {
                var a = (4 - System.Math.PI) / 2.0;
                var b = delta * System.Math.Sqrt(2.0 / System.Math.PI);
                var c = 1 - 2 * delta * delta / System.Math.PI;
                return a * (b * b * b) / System.Math.Pow(c, 3 / 2);
            }
        }

        /// <summary>
        ///     Gets the excess kurtosis for this distribution.
        /// </summary>
        public double Kurtosis
        {
            get
            {
                var a = 2 * (System.Math.PI * 3);
                var b = delta * System.Math.Sqrt(2.0 / System.Math.PI);
                var c = 1 - 2 * delta * delta / System.Math.PI;
                return a * (b * b * b * b) / (c * c);
            }
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
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        protected internal override double InnerDistributionFunction(double x)
        {
            var z = (x - Location) / Scale;

            var cdf = Math.Normal.Function(z) - 2 * OwensT.Function(z, Shape);

            return cdf;
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The probability of <c>x</c> occurring in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var z = (x - Location) / Scale;

            var a = Math.Normal.Derivative(z);
            var b = Math.Normal.Function(Shape * z);

            return 2 / Scale * a * b;
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
            var z = (x - Location) / Scale;

            var a = Math.Normal.LogDerivative(z);
            var b = System.Math.Log(Math.Normal.Function(Shape * z));

            return System.Math.Log(2 / Scale) + a + b;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new SkewNormalDistribution(Location, Scale, Shape);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Sn(x; ξ = {0}, ω = {1}, α = {2})",
                Location.ToString(format, formatProvider),
                Scale.ToString(format, formatProvider),
                Shape.ToString(format, formatProvider));
        }

        private void initialize(double location, double scale, double shape)
        {
            Location = location;
            Scale = scale;
            Shape = shape;

            // Compute derived values
            delta = shape / System.Math.Sqrt(1 + shape * shape);
        }

        /// <summary>
        ///     Create a new <see cref="SkewNormalDistribution" /> that
        ///     corresponds to a <see cref="NormalDistribution" /> with
        ///     the given mean and standard deviation.
        /// </summary>
        /// <param name="mean">The distribution's mean value μ (mu).</param>
        /// <param name="stdDev">The distribution's standard deviation σ (sigma).</param>
        /// <returns>
        ///     A <see cref="SkewNormalDistribution" /> representing
        ///     a <see cref="NormalDistribution" /> with the given parameters.
        /// </returns>
        public static SkewNormalDistribution Normal(int mean, double stdDev)
        {
            return new SkewNormalDistribution(mean, stdDev);
        }
    }
}