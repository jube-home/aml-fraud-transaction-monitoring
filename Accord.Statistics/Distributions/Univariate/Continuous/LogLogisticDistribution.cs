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
    ///     Log-Logistic distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability and statistics, the log-logistic distribution (known as the Fisk
    ///         distribution in economics) is a continuous probability distribution for a non-negative
    ///         random variable. It is used in survival analysis as a parametric model for events
    ///         whose rate increases initially and decreases later, for example mortality rate from
    ///         cancer following diagnosis or treatment. It has also been used in hydrology to model
    ///         stream flow and precipitation, and in economics as a simple model of the distribution
    ///         of wealth or income.
    ///     </para>
    ///     <para>
    ///         The log-logistic distribution is the probability distribution of a random variable
    ///         whose logarithm has a logistic distribution. It is similar in shape to the log-normal
    ///         distribution but has heavier tails. Its cumulative distribution function can be written
    ///         in closed form, unlike that of the log-normal.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Log-logistic_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Log-logistic distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Log-logistic_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This examples shows how to create a Log-Logistic distribution
    ///         and compute some of its properties and characteristics.
    ///     </para>
    ///     <code>
    /// // Create a LLD2 distribution with scale = 0.42, shape = 2.2
    /// var log = new LogLogisticDistribution(scale: 0.42, shape: 2.2);
    /// 
    /// double mean = log.Mean;     // 0.60592605102976937
    /// double median = log.Median; // 0.42
    /// double mode = log.Mode;     // 0.26892249963239817
    /// double var = log.Variance;  // 1.4357858982592435
    /// 
    /// double cdf = log.DistributionFunction(x: 1.4);           // 0.93393329906725353
    /// double pdf = log.ProbabilityDensityFunction(x: 1.4);     // 0.096960115938100763
    /// double lpdf = log.LogProbabilityDensityFunction(x: 1.4); // -2.3334555609306102
    /// 
    /// double ccdf = log.ComplementaryDistributionFunction(x: 1.4); // 0.066066700932746525
    /// double icdf = log.InverseDistributionFunction(p: cdf);       // 1.4000000000000006
    /// 
    /// double hf = log.HazardFunction(x: 1.4);            // 1.4676094699628273
    /// double chf = log.CumulativeHazardFunction(x: 1.4); // 2.7170904270953637
    /// 
    /// string str = log.ToString(CultureInfo.InvariantCulture); // LogLogistic(x; α = 0.42, β = 2.2)
    /// </code>
    /// </example>
    /// <seealso cref="LogisticDistribution" />
    /// <seealso cref="ShiftedLogLogisticDistribution" />
    [Serializable]
    public class LogLogisticDistribution : UnivariateContinuousDistribution
    {
        // Distribution parameters

        /// <summary>
        ///     Constructs a Log-Logistic distribution with unit scale and unit shape.
        /// </summary>
        public LogLogisticDistribution()
        {
            initialize(1, 1);
        }

        /// <summary>
        ///     Constructs a Log-Logistic distribution
        ///     with the given scale and unit shape.
        /// </summary>
        /// <param name="alpha">The distribution's scale value α (alpha). Default is 1.</param>
        public LogLogisticDistribution([Positive] double alpha)
        {
            if (alpha <= 0)
            {
                throw new ArgumentOutOfRangeException("alpha", "Alpha must be positive.");
            }

            initialize(alpha, 1);
        }

        /// <summary>
        ///     Constructs a Log-Logistic distribution
        ///     with the given scale and shape parameters.
        /// </summary>
        /// <param name="alpha">The distribution's scale value α (alpha). Default is 1.</param>
        /// <param name="beta">The distribution's shape value β (beta). Default is 1.</param>
        public LogLogisticDistribution([Positive] double alpha, [Positive] double beta)
        {
            if (alpha <= 0)
            {
                throw new ArgumentOutOfRangeException("alpha", "Alpha must be positive.");
            }

            if (beta <= 0)
            {
                throw new ArgumentOutOfRangeException("beta", "Beta must be positive.");
            }

            initialize(alpha, beta);
        }

        /// <summary>
        ///     Gets the distribution's scale value (α).
        /// </summary>
        public double Scale { get; private set; }

        /// <summary>
        ///     Gets the distribution's shape value (β).
        /// </summary>
        public double Shape { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Mean
        {
            get
            {
                if (Shape <= 1)
                {
                    return double.NaN;
                }

                var b = System.Math.PI / Shape;
                return Scale * b / System.Math.Sin(b);
            }
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
                Debug.Assert(Scale == base.Median);
                return Scale;
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
            get
            {
                if (Shape <= 2)
                {
                    return double.NaN;
                }

                var pb = System.Math.PI / Shape;
                var a = Scale * Scale;
                var b = 2 * pb / System.Math.Sin(2 * pb);
                var c = pb / System.Math.Sin(pb);

                return a * (b - c * c);
            }
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
                if (Shape <= 1)
                {
                    return 0;
                }

                return Scale * System.Math.Pow((Shape - 1) / (Shape + 1), 1 / Shape);
            }
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
            var xb = System.Math.Pow(x, Shape);
            var ab = System.Math.Pow(Scale, Shape);

            return xb / (ab + xb);
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
            var ba = Shape / Scale;
            var xa = x / Scale;

            var num = ba * System.Math.Pow(xa, Shape - 1);
            var den = 1 + System.Math.Pow(xa, Shape);

            return num / (den * den);
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
            return Scale * System.Math.Pow(p / (1 - p), 1 / Shape);
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
            var qdf = Scale / Shape * System.Math.Pow(p / (1 - p), 1 / Shape - 1);

            Debug.Assert(qdf == base.QuantileDensityFunction(p));

            return qdf;
        }

        /// <summary>
        ///     Gets the complementary cumulative distribution function
        ///     (ccdf) for this distribution evaluated at point <c>x</c>.
        ///     This function is also known as the Survival function.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     The Complementary Cumulative Distribution Function (CCDF) is
        ///     the complement of the Cumulative Distribution Function, or 1
        ///     minus the CDF.
        /// </remarks>
        protected internal override double InnerComplementaryDistributionFunction(double x)
        {
            var xa = x / Scale;
            var den = 1 + System.Math.Pow(xa, Shape);

            var icdf = 1 / den;

            Debug.Assert(icdf.IsEqual(base.InnerComplementaryDistributionFunction(x), 1e-10));

            return icdf;
        }

        /// <summary>
        ///     Gets the hazard function, also known as the failure rate or
        ///     the conditional failure density function for this distribution
        ///     evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The conditional failure density function <c>h(x)</c>
        ///     evaluated at <c>x</c> in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The hazard function is the ratio of the probability
        ///     density function f(x) to the survival function, S(x).
        /// </remarks>
        public override double HazardFunction(double x)
        {
            if (x < 0)
            {
                return 0;
            }

            var ba = Shape / Scale;
            var xa = x / Scale;

            var num = ba * System.Math.Pow(xa, Shape - 1);
            var den = 1 + System.Math.Pow(xa, Shape);

            var h = num / den;

            Debug.Assert(h.IsEqual(base.HazardFunction(x), 1e-10));

            return h;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new LogLogisticDistribution(Scale, Shape);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "LogLogistic(x; α = {0}, β = {1})",
                Scale.ToString(format, formatProvider),
                Shape.ToString(format, formatProvider));
        }

        private void initialize(double scale, double shape)
        {
            Scale = scale;
            Shape = shape;
        }

        /// <summary>
        ///     Creates a new <see cref="LogLogisticDistribution" /> using
        ///     the location-shape parametrization. In this parametrization,
        ///     <see cref="Beta" /> is taken as 1 / <paramref name="shape" />.
        /// </summary>
        /// <param name="location">The location parameter μ (mu) [taken as μ = α].</param>
        /// <param name="shape">The distribution's shape value σ (sigma) [taken as σ = β].</param>
        /// <returns>
        ///     A <see cref="LogLogisticDistribution" /> with α = μ  and β = 1/σ.
        /// </returns>
        public static LogLogisticDistribution FromLocationShape(double location, double shape)
        {
            return new LogLogisticDistribution(location, 1 / shape);
        }
    }
}