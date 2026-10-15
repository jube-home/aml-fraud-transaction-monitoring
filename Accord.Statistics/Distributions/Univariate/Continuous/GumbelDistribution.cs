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
    ///     Gumbel distribution (as known as the Extreme Value Type I distribution).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability theory and statistics, the Gumbel distribution is used to model
    ///         the distribution of the maximum (or the minimum) of a number of samples of various
    ///         distributions. Such a distribution might be used to represent the distribution of
    ///         the maximum level of a river in a particular year if there was a list of maximum
    ///         values for the past ten years. It is useful in predicting the chance that an extreme
    ///         earthquake, flood or other natural disaster will occur.
    ///     </para>
    ///     <para>
    ///         The potential applicability of the Gumbel distribution to represent the distribution
    ///         of maxima relates to extreme value theory which indicates that it is likely to be useful
    ///         if the distribution of the underlying sample data is of the normal or exponential type.
    ///     </para>
    ///     <para>
    ///         The Gumbel distribution is a particular case of the generalized extreme value
    ///         distribution (also known as the Fisher-Tippett distribution). It is also known
    ///         as the log-Weibull distribution and the double exponential distribution (a term
    ///         that is alternatively sometimes used to refer to the Laplace distribution). It
    ///         is related to the Gompertz distribution[citation needed]: when its density is
    ///         first reflected about the origin and then restricted to the positive half line,
    ///         a Gompertz function is obtained.
    ///     </para>
    ///     <para>
    ///         In the latent variable formulation of the multinomial logit model — common in
    ///         discrete choice theory — the errors of the latent variables follow a Gumbel
    ///         distribution. This is useful because the difference of two Gumbel-distributed
    ///         random variables has a logistic distribution.
    ///     </para>
    ///     <para>
    ///         The Gumbel distribution is named after Emil Julius Gumbel (1891–1966), based on
    ///         his original papers describing the distribution.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Gumbel_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Gumbel distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Gumbel_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to create and test the main characteristics
    ///         of an Gumbel distribution given its location and scale parameters:
    ///     </para>
    ///     <code>
    /// var gumbel = new GumbelDistribution(location: 4.795, scale: 1 / 0.392);
    /// 
    /// double mean = gumbel.Mean;     // 6.2674889410753387
    /// double median = gumbel.Median; // 5.7299819402593481
    /// double mode = gumbel.Mode;     // 4.7949999999999999
    /// double var = gumbel.Variance;  // 10.704745853604138
    /// 
    /// double cdf = gumbel.DistributionFunction(x: 3.4); // 0.17767760424788051
    /// double pdf = gumbel.ProbabilityDensityFunction(x: 3.4); // 0.12033954114322486
    /// double lpdf = gumbel.LogProbabilityDensityFunction(x: 3.4); // -2.1174380222001519
    /// 
    /// double ccdf = gumbel.ComplementaryDistributionFunction(x: 3.4); // 0.82232239575211952
    /// double icdf = gumbel.InverseDistributionFunction(p: cdf); // 3.3999999904866245
    /// 
    /// double hf = gumbel.HazardFunction(x: 1.4); // 0.03449691276402958
    /// double chf = gumbel.CumulativeHazardFunction(x: 1.4); // 0.022988793482259906
    /// 
    /// string str = gumbel.ToString(CultureInfo.InvariantCulture); // Gumbel(x; μ = 4.795, β = 2.55)
    /// </code>
    /// </example>
    [Serializable]
    public class GumbelDistribution : UnivariateContinuousDistribution,
        IFittableDistribution<double>
    {
        // Distribution parameters

        /// <summary>
        ///     Creates a new Gumbel distribution
        ///     with location zero and unit scale.
        /// </summary>
        public GumbelDistribution()
            : this(0, 1)
        {
        }

        /// <summary>
        ///     Creates a new Gumbel distribution
        ///     with the given location and scale.
        /// </summary>
        /// <param name="location">The location parameter μ (mu). Default is 0.</param>
        /// <param name="scale">The scale parameter β (beta). Default is 1.</param>
        public GumbelDistribution([Real] double location, [Positive] double scale)
        {
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale", "Scale must be positive.");
            }

            init(location, scale);
        }

        /// <summary>
        ///     Gets the distribution's location parameter mu (μ).
        /// </summary>
        public double Location { get; private set; }

        /// <summary>
        ///     Gets the distribution's scale parameter beta (β).
        /// </summary>
        public double Shape { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>The distribution's mean value.</value>
        public override double Mean
        {
            get => Location + Shape * Constants.EulerGamma;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>The distribution's variance.</value>
        public override double Variance
        {
            get => System.Math.PI * System.Math.PI / 6.0 * Shape * Shape;
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
        ///     Gets the median for this distribution.
        /// </summary>
        /// <value>The distribution's median value.</value>
        public override double Median
        {
            get
            {
                var median = Location - Shape * System.Math.Log(System.Math.Log(2));

#if DEBUG
                var expected = base.Median;
                if (!median.IsEqual(expected, 1e-6))
                {
                    throw new Exception();
                }
#endif
                return median;
            }
        }

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <value>The distribution's mode value.</value>
        public override double Mode
        {
            get => Location;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        /// <value>The distribution's entropy.</value>
        public override double Entropy
        {
            get => System.Math.Log(Shape) + Constants.EulerGamma + 1;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new GumbelDistribution(Location, Shape);
        }

        private void init(double location, double scale)
        {
            Location = location;
            Shape = scale;
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
        /// </remarks>
        protected internal override double InnerDistributionFunction(double x)
        {
            var z = (x - Location) / Shape;
            var cdf = System.Math.Exp(-System.Math.Exp(-z));
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
        /// <remarks>
        ///     <para>
        ///         The Probability Density Function (PDF) describes the
        ///         probability that a given value <c>x</c> will occur.
        ///     </para>
        /// </remarks>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var z = (x - Location) / Shape;
            return 1 / Shape * System.Math.Exp(-(z + System.Math.Exp(-z)));
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
        /// <seealso cref="UnivariateContinuousDistribution.ProbabilityDensityFunction" />
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            var z = (x - Location) / Shape;
            return System.Math.Log(1 / Shape) - (z + System.Math.Exp(-z));
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
            var z = (x - Location) / Shape;
            var expz = System.Math.Exp(-z);
            var cdf = System.Math.Exp(-expz);
            return 1.0 - cdf;
        }

        /// <summary>
        ///     Gets the cumulative hazard function for this
        ///     distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The cumulative hazard function <c>H(x)</c>
        ///     evaluated at <c>x</c> in the current distribution.
        /// </returns>
        public override double CumulativeHazardFunction(double x)
        {
            var z = (x - Location) / Shape;
            var expz = System.Math.Exp(-z);
            return -System.Math.Log(1.0 - System.Math.Exp(-expz));
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
            var alpha = 1.0 / Shape;
            var z = (x - Location) / Shape;

            var expz = System.Math.Exp(-z);

            var h = alpha * expz / (System.Math.Exp(expz) - 1.0);

#if DEBUG
            var expected = base.HazardFunction(x);
            if (!h.IsEqual(expected, 1e-4))
            {
                throw new Exception();
            }
#endif

            return h;
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
            return Location - Shape * System.Math.Log(-System.Math.Log(p));
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            double mean;
            double stdDev;

            if (weights != null)
            {
                mean = observations.WeightedMean(weights);
                stdDev = observations.WeightedStandardDeviation(weights, mean);
            }
            else
            {
                mean = observations.Mean();
                stdDev = observations.StandardDeviation(mean);
            }

            var u = mean - 0.45006 * stdDev;
            var b = stdDev * System.Math.Sqrt(6) / System.Math.PI;

            init(u, b);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        public override void Fit(double[] observations, int[] weights, IFittingOptions options)
        {
            double mean;
            double stdDev;

            if (weights != null)
            {
                mean = observations.WeightedMean(weights);
                stdDev = observations.WeightedStandardDeviation(weights, mean);
            }
            else
            {
                mean = observations.Mean();
                stdDev = observations.StandardDeviation(mean);
            }

            var u = mean - 0.45006 * stdDev;
            var b = stdDev * System.Math.Sqrt(6) / System.Math.PI;

            init(u, b);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Gumbel(x; μ = {0}, β = {1})",
                Location.ToString(format, formatProvider),
                Shape.ToString(format, formatProvider));
        }
    }
}