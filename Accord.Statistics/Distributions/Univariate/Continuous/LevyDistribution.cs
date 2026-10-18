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
    ///     Lévy distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability theory and statistics, the Lévy distribution, named after Paul Lévy, is a continuous
    ///         probability distribution for a non-negative random variable. In spectroscopy, this distribution, with
    ///         frequency as the dependent variable, is known as a van der Waals profile. It is a special case of the
    ///         inverse-gamma distribution.
    ///     </para>
    ///     <para>
    ///         It is one of the few distributions that are stable and that have probability density functions that can
    ///         be expressed analytically, the others being the normal distribution and the Cauchy distribution. All three
    ///         are special cases of the stable distributions, which do not generally have a probability density function
    ///         which can be expressed analytically.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="https://en.wikipedia.org/wiki/L%C3%A9vy_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Lévy distribution. Available on:
    ///                         https://en.wikipedia.org/wiki/L%C3%A9vy_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This examples shows how to create a Lévy distribution
    ///         and how to compute some of its measures and properties.
    ///     </para>
    ///     <code>
    /// // Create a new Lévy distribution on 1 with scale 4.2:
    /// var levy = new LevyDistribution(location: 1, scale: 4.2);
    /// 
    /// double mean = levy.Mean;     // +inf
    /// double median = levy.Median; // 10.232059220934481
    /// double mode = levy.Mode;     // NaN
    /// double var = levy.Variance;  // +inf
    /// 
    /// double cdf = levy.DistributionFunction(x: 1.4); // 0.0011937454448720029
    /// double pdf = levy.ProbabilityDensityFunction(x: 1.4); // 0.016958939623898304
    /// double lpdf = levy.LogProbabilityDensityFunction(x: 1.4); // -4.0769601727487803
    /// 
    /// double ccdf = levy.ComplementaryDistributionFunction(x: 1.4); // 0.99880625455512795
    /// double icdf = levy.InverseDistributionFunction(p: cdf); // 1.3999999
    /// 
    /// double hf = levy.HazardFunction(x: 1.4); // 0.016979208476674869
    /// double chf = levy.CumulativeHazardFunction(x: 1.4); // 0.0011944585265140923
    /// 
    /// string str = levy.ToString(CultureInfo.InvariantCulture); // Lévy(x; μ = 1, c = 4.2)
    /// </code>
    /// </example>
    [Serializable]
    public class LevyDistribution : UnivariateContinuousDistribution
    {
        // Distribution parameters

        // Derived measures
        private double? median;

        /// <summary>
        ///     Constructs a new <see cref="LevyDistribution" />
        ///     with zero location and unit scale.
        /// </summary>
        public LevyDistribution()
        {
            initialize(0, 1);
        }

        /// <summary>
        ///     Constructs a new <see cref="LevyDistribution" /> in
        ///     the given <paramref name="location" /> and with unit scale.
        /// </summary>
        /// <param name="location">The distribution's location.</param>
        public LevyDistribution([Real] double location)
        {
            initialize(location, 1);
        }

        /// <summary>
        ///     Constructs a new <see cref="LevyDistribution" /> in the
        ///     given <paramref name="location" /> and <paramref name="scale" />.
        /// </summary>
        /// <param name="location">The distribution's location.</param>
        /// <param name="scale">The distribution's scale.</param>
        public LevyDistribution([Real] double location, [Positive] double scale)
        {
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale", "Scale must be positive.");
            }

            initialize(location, scale);
        }

        /// <summary>
        ///     Gets the location μ (mu) for this distribution.
        /// </summary>
        public double Location { get; private set; }

        /// <summary>
        ///     Gets the location c for this distribution.
        /// </summary>
        public double Scale { get; private set; } = 1;

        /// <summary>
        ///     Gets the mean for this distribution, which for
        ///     the Levy distribution is always positive infinity.
        /// </summary>
        /// <value>
        ///     This property always returns <c>Double.PositiveInfinity</c>.
        /// </value>
        public override double Mean
        {
            get => double.PositiveInfinity;
            set { } //Not Implemented.
        }

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
                if (!median.HasValue)
                {
                    var a = Normal.Inverse(0.75);
                    var m = Location + Scale / (a * a);

#if DEBUG
                    var baseMedian = base.Median;
                    if (!m.IsEqual(baseMedian, 1e-10))
                    {
                        throw new Exception();
                    }
#endif

                    median = m;
                }

                return median.Value;
            }
        }

        /// <summary>
        ///     Gets the variance for this distribution, which for
        ///     the Levy distribution is always positive infinity.
        /// </summary>
        /// <value>
        ///     This property always returns <c>Double.PositiveInfinity</c>.
        /// </value>
        public override double Variance
        {
            get => double.PositiveInfinity;
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
                if (Location == 0)
                {
                    return Scale / 3.0;
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
            get => new(Location, double.PositiveInfinity);
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
            get => (1.0 + 3.0 * Constants.EulerGamma + System.Math.Log(16 * System.Math.PI * Scale * Scale)) / 2.0;
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
            var cdf = Special.Erfc(System.Math.Sqrt(Scale / (2 * (x - Location))));

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
            var z = x - Location;
            var a = System.Math.Sqrt(Scale / (2.0 * System.Math.PI));
            var b = System.Math.Exp(-(Scale / (2 * z)));
            var c = System.Math.Pow(z, 3.0 / 2.0);

            return a * b / c;
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
        /// <remarks>
        ///     The Inverse Cumulative Distribution Function (ICDF) specifies, for
        ///     a given probability, the value which the random variable will be at,
        ///     or below, with that probability.
        /// </remarks>
        protected internal override double InnerInverseDistributionFunction(double p)
        {
            var a = Normal.Inverse(1.0 - p / 2.0);
            var icdf = Location + Scale / (a * a);

#if DEBUG
            var baseValue = base.InnerInverseDistributionFunction(p);
            if (!baseValue.IsEqual(icdf, 1e-5))
            {
                throw new Exception();
            }
#endif

            return icdf;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new LevyDistribution(Location, Scale);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Lévy(x; μ = {0}, c = {1})",
                Location.ToString(format, formatProvider),
                Scale.ToString(format, formatProvider));
        }

        private void initialize(double mu, double c)
        {
            Location = mu;
            Scale = c;
        }
    }
}