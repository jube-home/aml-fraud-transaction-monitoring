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

using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Distributions
{
    /// <summary>
    ///     Common interface for univariate probability distributions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This interface is implemented by both univariate
    ///         <see cref="UnivariateDiscreteDistribution">
    ///             Discrete Distributions
    ///         </see>
    ///         and
    ///         <see cref="UnivariateContinuousDistribution">
    ///             Continuous
    ///             Distributions
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         For Multivariate distributions, see <see cref="IMultivariateDistribution" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="NormalDistribution" />
    /// <seealso cref="GammaDistribution" />
    /// <seealso cref="UniformContinuousDistribution" />
    /// <seealso cref="UniformDiscreteDistribution" />
    public interface IUnivariateDistribution : IDistribution
    {
        /// <summary>
        ///     Gets the mean value for the distribution.
        /// </summary>
        /// <value>The distribution's mean.</value>
        double Mean { get; }

        /// <summary>
        ///     Gets the variance value for the distribution.
        /// </summary>
        /// <value>The distribution's variance.</value>
        double Variance { get; }

        /// <summary>
        ///     Gets the median value for the distribution.
        /// </summary>
        /// <value>The distribution's median.</value>
        double Median { get; }

        /// <summary>
        ///     Gets the mode value for the distribution.
        /// </summary>
        /// <value>The distribution's mode.</value>
        double Mode { get; }

        /// <summary>
        ///     Gets entropy of the distribution.
        /// </summary>
        /// <value>The distribution's entropy.</value>
        double Entropy { get; }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="DoubleRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        DoubleRange Support { get; set; }

        /// <summary>
        ///     Gets the Quartiles for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="DoubleRange" /> object containing the first quartile
        ///     (Q1) as its minimum value, and the third quartile (Q2) as the maximum.
        /// </value>
        DoubleRange Quartiles { get; }

        /// <summary>
        ///     Gets the distribution range within a given percentile.
        /// </summary>
        /// <remarks>
        ///     If <c>0.25</c> is passed as the <paramref name="percentile" /> argument,
        ///     this function returns the same as the <see cref="Quartiles" /> function.
        /// </remarks>
        /// <param name="percentile">
        ///     The percentile at which the distribution ranges will be returned.
        /// </param>
        /// <value>
        ///     A <see cref="DoubleRange" /> object containing the minimum value
        ///     for the distribution value, and the third quartile (Q2) as the maximum.
        /// </value>
        DoubleRange GetRange(double percentile);

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        double DistributionFunction(double x);

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for this
        ///     distribution in the semi-closed interval (a; b] given as
        ///     <c>P(a &lt; X ≤ b)</c>.
        /// </summary>
        /// <param name="a">The start of the semi-closed interval (a; b].</param>
        /// <param name="b">The end of the semi-closed interval (a; b].</param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        double DistributionFunction(double a, double b);

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range. For a
        ///     univariate distribution, this should be a single
        ///     double value. For a multivariate distribution,
        ///     this should be a double array.
        /// </param>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        double ProbabilityFunction(double x);

        /// <summary>
        ///     Gets the log-probability density function (pdf)
        ///     for this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range. For a
        ///     univariate distribution, this should be a single
        ///     double value. For a multivariate distribution,
        ///     this should be a double array.
        /// </param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        double LogProbabilityFunction(double x);

        /// <summary>
        ///     Gets the inverse of the cumulative distribution function (icdf) for
        ///     this distribution evaluated at probability <c>p</c>. This function
        ///     is also known as the Quantile function.
        /// </summary>
        /// <remarks>
        ///     The Inverse Cumulative Distribution Function (ICDF) specifies, for
        ///     a given probability, the value which the random variable will be at,
        ///     or below, with that probability.
        /// </remarks>
        /// <param name="p">A probability value between 0 and 1.</param>
        /// <returns>
        ///     A sample which could original the given probability
        ///     value when applied in the <see cref="DistributionFunction(double)" />.
        /// </returns>
        double InverseDistributionFunction(double p);

        /// <summary>
        ///     Gets the complementary cumulative distribution function
        ///     (ccdf) for this distribution evaluated at point <c>x</c>.
        ///     This function is also known as the Survival function.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Complementary Cumulative Distribution Function (CCDF) is
        ///     the complement of the Cumulative Distribution Function, or 1
        ///     minus the CDF.
        /// </remarks>
        double ComplementaryDistributionFunction(double x);

        /// <summary>
        ///     Gets the hazard function, also known as the failure rate or
        ///     the conditional failure density function for this distribution
        ///     evaluated at point <c>x</c>.
        /// </summary>
        /// <remarks>
        ///     The hazard function is the ratio of the probability
        ///     density function f(x) to the survival function, S(x).
        /// </remarks>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <returns>
        ///     The conditional failure density function <c>h(x)</c>
        ///     evaluated at <c>x</c> in the current distribution.
        /// </returns>
        double HazardFunction(double x);

        /// <summary>
        ///     Gets the cumulative hazard function for this
        ///     distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <returns>
        ///     The cumulative hazard function <c>H(x)</c>
        ///     evaluated at <c>x</c> in the current distribution.
        /// </returns>
        double CumulativeHazardFunction(double x);

        /// <summary>
        ///     Gets the cumulative hazard function for this
        ///     distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <returns>
        ///     The cumulative hazard function <c>H(x)</c>
        ///     evaluated at <c>x</c> in the current distribution.
        /// </returns>
        double LogCumulativeHazardFunction(double x);

        /// <summary>
        ///     Gets the first derivative of the
        ///     <see cref="InverseDistributionFunction">
        ///         inverse distribution function
        ///     </see>
        ///     (icdf) for this distribution evaluated
        ///     at probability <c>p</c>.
        /// </summary>
        /// <param name="p">A probability value between 0 and 1.</param>
        double QuantileDensityFunction(double p);
    }
}