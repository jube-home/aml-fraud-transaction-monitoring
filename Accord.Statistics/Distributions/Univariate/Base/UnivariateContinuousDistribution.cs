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

// A note on compatibility: Up to version 3.5, users were supposed to implement their own probability
// distributions by inheriting from this class and overriding the public members ProbabilityDensityFunction,
// DistributionFunction, etc. However, since those were public methods this meant that users (and I) had to
// write validation checks on every method override, resulting in lots of duplicated code. Starting from version
// 3.6, users should override methods that start with "Inner" in their name, such as InnerProbabilityDensityFunction 
// and InnerDistributionFunction. The framework will have already validated the inputs of those functions, and
// will also take care to check whether the implementation of those functions is correct.

// For now, compatibility mode is enabled for release builds, meaning that old code that has been written
// using the old way will keep working. However, debug (development) builds will have this feature turned
// off to force new classes to be implemented using this new way.

# if !DEBUG
#define COMPATIBILITY
#endif

using System;
using System.ComponentModel.DataAnnotations;
using Accord.Math;
using Accord.Math.Optimization;
using Accord.Math.Random;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Abstract class for univariate continuous probability Distributions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A probability distribution identifies either the probability of each value of an
    ///         unidentified random variable (when the variable is discrete), or the probability
    ///         of the value falling within a particular interval (when the variable is continuous).
    ///     </para>
    ///     <para>
    ///         The probability distribution describes the range of possible values that a random
    ///         variable can attain and the probability that the value of the random variable is
    ///         within any (measurable) subset of that range.
    ///     </para>
    ///     <para>
    ///         The function describing the probability that a given value will occur is called
    ///         the probability function (or probability density function, abbreviated PDF), and
    ///         the function describing the cumulative probability that a given value or any value
    ///         smaller than it will occur is called the distribution function (or cumulative
    ///         distribution function, abbreviated CDF).
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Probability_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Probability distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Probability_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://mathworld.wolfram.com/StatisticalDistribution.html">
    ///                         Weisstein, Eric W. "Statistical Distribution." From MathWorld--A Wolfram Web Resource.
    ///                         http://mathworld.wolfram.com/StatisticalDistribution.html
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="NormalDistribution" />
    /// <seealso cref="GammaDistribution" />
    [Serializable]
    public abstract class UnivariateContinuousDistribution : DistributionBase,
        IDistribution, IUnivariateDistribution, IUnivariateDistribution<double>,
        ISampleableDistribution<double>,
        IFormattable
    {
        [NonSerialized] private double? median;

        [NonSerialized] private double? mode;

        [NonSerialized] private DoubleRange? quartiles;

        [NonSerialized] private double? stdDev;

        /// <summary>
        ///     Constructs a new UnivariateDistribution class.
        /// </summary>
        protected UnivariateContinuousDistribution()
        {
        }

        /// <summary>
        ///     Gets the Standard Deviation (the square root of
        ///     the variance) for the current distribution.
        /// </summary>
        /// <value>The distribution's standard deviation.</value>
        public virtual double StandardDeviation
        {
            get
            {
                if (!stdDev.HasValue)
                {
                    stdDev = System.Math.Sqrt(Variance);
                }

                return stdDev.Value;
            }
            set { }
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public double[] Generate(int samples)
        {
            return Generate(samples, new double[samples], Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public double[] Generate(int samples, double[] result)
        {
            return Generate(samples, result, Generator.Random);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public double Generate()
        {
            return Generate(Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public double[] Generate(int samples, Random source)
        {
            return Generate(samples, new double[samples], source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public virtual double[] Generate(int samples, double[] result, Random source)
        {
            for (var i = 0; i < samples; i++)
            {
                result[i] = InverseDistributionFunction(source.NextDouble());
            }

            return result;
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public virtual double Generate(Random source)
        {
            return InverseDistributionFunction(source.NextDouble());
        }

        double ISampleableDistribution<double>.Generate(double result)
        {
            return Generate();
        }

        double ISampleableDistribution<double>.Generate(double result, Random source)
        {
            return Generate(source);
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>The distribution's mean value.</value>
        public abstract double Mean { get; set; }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>The distribution's variance.</value>
        public abstract double Variance { get; set; }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        /// <value>The distribution's entropy.</value>
        public abstract double Entropy { get; set; }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="DoubleRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public abstract DoubleRange Support { get; set; }

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <value>The distribution's mode value.</value>
        public virtual double Mode
        {
            get
            {
                if (mode == null)
                {
                    mode = BrentSearch.Maximize(ProbabilityDensityFunction, Quartiles.Min, Quartiles.Max, 1e-10);
                }

                return mode.Value;
            }
            set { }
        }

        /// <summary>
        ///     Gets the Quartiles for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="DoubleRange" /> object containing the first quartile
        ///     (Q1) as its minimum value, and the third quartile (Q2) as the maximum.
        /// </value>
        public virtual DoubleRange Quartiles
        {
            get
            {
                if (quartiles == null)
                {
                    var min = InverseDistributionFunction(0.25);
                    var max = InverseDistributionFunction(0.75);
                    quartiles = new DoubleRange(min, max);
                }

                return quartiles.Value;
            }
            set { }
        }

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
        public virtual DoubleRange GetRange(double percentile)
        {
            if (percentile <= 0 || percentile > 1)
            {
                throw new ArgumentOutOfRangeException("percentile", "The percentile must be between 0 and 1.");
            }

            var a = InverseDistributionFunction(1.0 - percentile);
            var b = InverseDistributionFunction(percentile);

            if (b > a)
            {
                return new DoubleRange(a, b);
            }

            return new DoubleRange(b, a);
        }

        /// <summary>
        ///     Gets the median for this distribution.
        /// </summary>
        /// <value>The distribution's median value.</value>
        public virtual double Median
        {
            get
            {
                if (median == null)
                {
                    median = InverseDistributionFunction(0.5);
                }

                return median.Value;
            }
            set { }
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        public
#if COMPATIBILITY
        virtual
#endif
            double DistributionFunction(double x)
        {
            if (double.IsNaN(x))
            {
                throw new ArgumentOutOfRangeException("x", "The input argument is NaN.");
            }

            if (double.IsNegativeInfinity(Support.Min) && double.IsNegativeInfinity(x))
            {
                return 0;
            }

            if (x < Support.Min) // Needed by: InverseChiSquare
            {
                return 0;
            }

            if (x >= Support.Max) // Needed by: GrubbDistribution
            {
                return 1;
            }

            var result = InnerDistributionFunction(x);

            if (double.IsNaN(result))
            {
                throw new InvalidOperationException("CDF computation generated NaN values.");
            }

            if (result < 0 || result > 1)
            {
                throw new InvalidOperationException("CDF computation generated values out of the [0,1] range.");
            }

            return result;
        }

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
        public
#if COMPATIBILITY
        virtual
#endif
            double DistributionFunction(double a, double b)
        {
            if (a > b)
            {
                throw new ArgumentOutOfRangeException("b",
                    "The start of the interval a must be smaller than b.");
            }

            if (a == b)
            {
                return 0;
            }

            return DistributionFunction(b) - DistributionFunction(a);
        }

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
        public
#if COMPATIBILITY
        virtual
#endif
            double ComplementaryDistributionFunction(double x)
        {
            if (double.IsNaN(x))
            {
                throw new ArgumentOutOfRangeException("x", "The input argument is NaN.");
            }

            if (double.IsNegativeInfinity(Support.Min) && double.IsNegativeInfinity(x))
            {
                return 1;
            }

            if (x < Support.Min)
            {
                return 1;
            }

            if (x >= Support.Max)
            {
                return 0;
            }

            var result = InnerComplementaryDistributionFunction(x);

            if (double.IsNaN(result))
            {
                throw new InvalidOperationException("CCDF computation generated NaN values.");
            }

            if (result < 0 || result > 1)
            {
                throw new InvalidOperationException("CCDF computation generated values out of the [0,1] range.");
            }

            return result;
        }

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
        public double InverseDistributionFunction(
#if !NET35
            [Range(0, 1)]
#endif
            double p)
        {
            if (p < 0.0 || p > 1.0)
            {
                throw new ArgumentOutOfRangeException("p", "Value must be between 0 and 1.");
            }

            if (double.IsNaN(p))
            {
                throw new ArgumentOutOfRangeException("p", "Value is Not-a-Number (NaN).");
            }

            if (p == 0)
            {
                return Support.Min;
            }

            if (p == 1)
            {
                return Support.Max;
            }

            var result = InnerInverseDistributionFunction(p);

            if (double.IsNaN(result))
            {
                throw new InvalidOperationException("invCDF computation generated NaN values.");
            }

            if (result < Support.Min || result > Support.Max)
            {
                throw new InvalidOperationException(
                    "invCDF computation generated values outside the distribution supported range.");
            }

            return result;
        }

        /// <summary>
        ///     Gets the first derivative of the
        ///     <see cref="InverseDistributionFunction">
        ///         inverse distribution function
        ///     </see>
        ///     (icdf) for this distribution evaluated
        ///     at probability <c>p</c>.
        /// </summary>
        /// <param name="p">A probability value between 0 and 1.</param>
        public virtual double QuantileDensityFunction(double p)
        {
            return 1.0 / ProbabilityDensityFunction(InverseDistributionFunction(p));
        }

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
        public virtual double HazardFunction(double x)
        {
            var f = ProbabilityDensityFunction(x);
            if (f == 0)
            {
                return 0;
            }

            var s = ComplementaryDistributionFunction(x);
            return f / s;
        }

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
        public virtual double CumulativeHazardFunction(double x)
        {
            return -System.Math.Log(ComplementaryDistributionFunction(x));
        }

        /// <summary>
        ///     Gets the log of the cumulative hazard function for this
        ///     distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <returns>
        ///     The cumulative hazard function <c>H(x)</c>
        ///     evaluated at <c>x</c> in the current distribution.
        /// </returns>
        public virtual double LogCumulativeHazardFunction(double x)
        {
            var result = System.Math.Log(-System.Math.Log(ComplementaryDistributionFunction(x)));
#if DEBUG
            var expected = System.Math.Log(CumulativeHazardFunction(x));
            if (!expected.IsEqual(result, rtol: 1e-8))
            {
                throw new Exception();
            }
#endif
            return result;
        }

        double IDistribution<double>.ProbabilityFunction(double x)
        {
            return ProbabilityDensityFunction(x);
        }

        double IDistribution<double>.LogProbabilityFunction(double x)
        {
            return LogProbabilityDensityFunction(x);
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
#if COMPATIBILITY
        protected internal virtual double InnerDistributionFunction(double x)
        {
            throw new NotImplementedException();
        }
#else
        protected internal abstract double InnerDistributionFunction(double x);
#endif

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
        protected internal virtual double InnerComplementaryDistributionFunction(double x)
        {
            return 1.0 - DistributionFunction(x);
        }

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
        protected internal virtual double InnerInverseDistributionFunction(double p)
        {
            var lowerBounded = !double.IsInfinity(Support.Min);
            var upperBounded = !double.IsInfinity(Support.Max);

            double lower;
            double upper;
            double f;

            if (lowerBounded && upperBounded)
            {
                lower = Support.Min;
                upper = Support.Max;
            }

            else if (lowerBounded && !upperBounded)
            {
                lower = Support.Min;
                upper = lower + 1;

                f = DistributionFunction(lower);

                if (f > p)
                {
                    while (f > p && !double.IsInfinity(upper))
                    {
                        upper += 2 * (upper - lower) + 1;
                        f = DistributionFunction(upper);
                    }
                }
                else if (f < p)
                {
                    while (f < p && !double.IsInfinity(upper))
                    {
                        upper += 2 * (upper - lower) + 1;
                        f = DistributionFunction(upper);
                    }
                }
                else
                {
                    return lower;
                }
            }

            else if (!lowerBounded && upperBounded)
            {
                upper = Support.Max;
                lower = upper - 1;

                f = DistributionFunction(upper);

                if (f > p)
                {
                    while (f > p && !double.IsInfinity(lower))
                    {
                        lower = lower - 2 * lower;
                        f = DistributionFunction(lower);
                    }
                }
                else if (f < p)
                {
                    while (f < p && !double.IsInfinity(lower))
                    {
                        lower = lower - 2 * lower;
                        f = DistributionFunction(lower);
                    }
                }
                else
                {
                    return upper;
                }
            }

            else // completely unbounded
            {
                lower = 0;
                upper = 0;

                f = DistributionFunction(0);

                if (f > p)
                {
                    while (f > p && !double.IsInfinity(lower))
                    {
                        upper = lower;
                        lower = 2 * lower - 1;
                        f = DistributionFunction(lower);
                    }
                }
                else if (f < p)
                {
                    while (f < p && !double.IsInfinity(upper))
                    {
                        lower = upper;
                        upper = 2 * upper + 1;
                        f = DistributionFunction(upper);
                    }
                }
                else
                {
                    return 0;
                }
            }

            if (double.IsNegativeInfinity(lower))
            {
                lower = double.MinValue;
            }

            if (double.IsPositiveInfinity(upper))
            {
                upper = double.MaxValue;
            }

            var value = BrentSearch.Find(DistributionFunction, p, lower, upper);

            return value;
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        public
#if COMPATIBILITY
        virtual
#endif
            double ProbabilityDensityFunction(double x)
        {
            if (double.IsNaN(x))
            {
                throw new ArgumentOutOfRangeException("x", "The input argument is NaN.");
            }

            if (x < Support.Min || x > Support.Max)
            {
                return 0;
            }

            var result = InnerProbabilityDensityFunction(x);

            if (double.IsNaN(result))
            {
                throw new InvalidOperationException("PDF computation generated NaN values.");
            }

            return result;
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
#if COMPATIBILITY
        protected internal virtual double InnerProbabilityDensityFunction(double x)
        {
            throw new NotImplementedException();
        }
#else
        protected internal abstract double InnerProbabilityDensityFunction(double x);
#endif

        /// <summary>
        ///     Gets the log-probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        public
#if COMPATIBILITY
        virtual
#endif
            double LogProbabilityDensityFunction(double x)
        {
            if (double.IsNaN(x))
            {
                throw new ArgumentOutOfRangeException("x", "The input argument is NaN.");
            }

            if (x < Support.Min || x > Support.Max)
            {
                return double.NegativeInfinity;
            }

            var result = InnerLogProbabilityDensityFunction(x);

            if (double.IsNaN(result))
            {
                throw new InvalidOperationException("LogPDF computation generated NaN values.");
            }

            return result;
        }

        /// <summary>
        ///     Gets the log-probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        protected internal virtual double InnerLogProbabilityDensityFunction(double x)
        {
            return System.Math.Log(ProbabilityDensityFunction(x));
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        /// <example>
        ///     <para>
        ///         The following example shows how to fit a <see cref="NormalDistribution" /> using
        ///         the <see cref="Fit(double[], double[])" /> method. However, any other kind of
        ///         distribution could be fit in the exactly same way. Please consider the code below
        ///         as an example only:
        ///     </para>
        ///     <code
        ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Univariate\Continuous\UnivariateContinuousDistributionTest.cs"
        ///         region="doc_fit" />
        ///     <para>
        ///         If you would like futher examples, please take a look at the documentation page
        ///         for the distribution you would like to fit for more details and configuration
        ///         options you would like or need to control.
        ///     </para>
        /// </example>
        public void Fit(double[] observations)
        {
            Fit(observations, (IFittingOptions)null);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">
        ///     The weight vector containing the weight for each of the samples.
        /// </param>
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        /// <example>
        ///     <para>
        ///         The following example shows how to fit a <see cref="NormalDistribution" /> using
        ///         the <see cref="Fit(double[], double[])" /> method. However, any other kind of
        ///         distribution could be fit in the exactly same way. Please consider the code below
        ///         as an example only:
        ///     </para>
        ///     <code
        ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Univariate\Continuous\UnivariateContinuousDistributionTest.cs"
        ///         region="doc_fit" />
        ///     <para>
        ///         If you would like futher examples, please take a look at the documentation page
        ///         for the distribution you would like to fit for more details and configuration
        ///         options you would like or need to control.
        ///     </para>
        /// </example>
        public void Fit(double[] observations, double[] weights)
        {
            Fit(observations, weights, null);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">
        ///     The weight vector containing the weight for each of the samples.
        /// </param>
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        /// <example>
        ///     <para>
        ///         The following example shows how to fit a <see cref="NormalDistribution" /> using
        ///         the <see cref="Fit(double[], double[])" /> method. However, any other kind of
        ///         distribution could be fit in the exactly same way. Please consider the code below
        ///         as an example only:
        ///     </para>
        ///     <code
        ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Univariate\Continuous\UnivariateContinuousDistributionTest.cs"
        ///         region="doc_fit" />
        ///     <para>
        ///         If you would like futher examples, please take a look at the documentation page
        ///         for the distribution you would like to fit for more details and configuration
        ///         options you would like or need to control.
        ///     </para>
        /// </example>
        public void Fit(double[] observations, int[] weights)
        {
            Fit(observations, weights, null);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
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
        /// <example>
        ///     <para>
        ///         The following example shows how to fit a <see cref="NormalDistribution" /> using
        ///         the <see cref="Fit(double[], double[])" /> method. However, any other kind of
        ///         distribution could be fit in the exactly same way. Please consider the code below
        ///         as an example only:
        ///     </para>
        ///     <code
        ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Univariate\Continuous\UnivariateContinuousDistributionTest.cs"
        ///         region="doc_fit" />
        ///     <para>
        ///         If you would like futher examples, please take a look at the documentation page
        ///         for the distribution you would like to fit for more details and configuration
        ///         options you would like or need to control.
        ///     </para>
        /// </example>
        public void Fit(double[] observations, IFittingOptions options)
        {
            Fit(observations, (double[])null, options);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">
        ///     The weight vector containing the weight for each of the samples.
        /// </param>
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
        /// <example>
        ///     <para>
        ///         The following example shows how to fit a <see cref="NormalDistribution" /> using
        ///         the <see cref="Fit(double[], double[])" /> method. However, any other kind of
        ///         distribution could be fit in the exactly same way. Please consider the code below
        ///         as an example only:
        ///     </para>
        ///     <code
        ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Univariate\Continuous\UnivariateContinuousDistributionTest.cs"
        ///         region="doc_fit" />
        ///     <para>
        ///         If you would like futher examples, please take a look at the documentation page
        ///         for the distribution you would like to fit for more details and configuration
        ///         options you would like or need to control.
        ///     </para>
        /// </example>
        public virtual void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">
        ///     The weight vector containing the weight for each of the samples.
        /// </param>
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
        /// <example>
        ///     <para>
        ///         The following example shows how to fit a <see cref="NormalDistribution" /> using
        ///         the <see cref="Fit(double[], double[])" /> method. However, any other kind of
        ///         distribution could be fit in the exactly same way. Please consider the code below
        ///         as an example only:
        ///     </para>
        ///     <code
        ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Univariate\Continuous\UnivariateContinuousDistributionTest.cs"
        ///         region="doc_fit" />
        ///     <para>
        ///         If you would like futher examples, please take a look at the documentation page
        ///         for the distribution you would like to fit for more details and configuration
        ///         options you would like or need to control.
        ///     </para>
        /// </example>
        public virtual void Fit(double[] observations, int[] weights, IFittingOptions options)
        {
            if (weights != null)
            {
                throw new NotSupportedException();
            }

            Fit(observations, (double[])null, options);
        }

        #region IDistribution explicit members

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
        double IDistribution.DistributionFunction(double[] x)
        {
            return DistributionFunction(x[0]);
        }

        /// <summary>
        ///     Gets the complementary cumulative distribution function
        ///     (ccdf) for this distribution evaluated at point <c>x</c>.
        ///     This function is also known as the Survival function.
        /// </summary>
        /// <remarks>
        ///     The Complementary Cumulative Distribution Function (CCDF) is
        ///     the complement of the Cumulative Distribution Function, or 1
        ///     minus the CDF.
        /// </remarks>
        double IDistribution.ComplementaryDistributionFunction(double[] x)
        {
            return ComplementaryDistributionFunction(x[0]);
        }

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
        double IDistribution.ProbabilityFunction(double[] x)
        {
            return ProbabilityDensityFunction(x[0]);
        }

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
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        double IUnivariateDistribution.ProbabilityFunction(double x)
        {
            return ProbabilityDensityFunction(x);
        }

        /// <summary>
        ///     Gets the log-probability density function (pdf) for
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
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        double IDistribution.LogProbabilityFunction(double[] x)
        {
            return LogProbabilityDensityFunction(x[0]);
        }

        /// <summary>
        ///     Gets the log-probability density function (pdf) for
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
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        double IUnivariateDistribution.LogProbabilityFunction(double x)
        {
            return LogProbabilityDensityFunction(x);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        void IDistribution.Fit(Array observations)
        {
            (this as IDistribution).Fit(observations, (IFittingOptions)null);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">
        ///     The weight vector containing the weight for each of the samples.
        /// </param>
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        void IDistribution.Fit(Array observations, double[] weights)
        {
            (this as IDistribution).Fit(observations, weights, null);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">
        ///     The weight vector containing the weight for each of the samples.
        /// </param>
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        void IDistribution.Fit(Array observations, int[] weights)
        {
            (this as IDistribution).Fit(observations, weights, null);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
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
        void IDistribution.Fit(Array observations, IFittingOptions options)
        {
            (this as IDistribution).Fit(observations, (double[])null, options);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">
        ///     The weight vector containing the weight for each of the samples.
        /// </param>
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
        void IDistribution.Fit(Array observations, double[] weights, IFittingOptions options)
        {
            var univariate = observations as double[];
            if (univariate != null)
            {
                Fit(univariate, weights, options);
                return;
            }

            var multivariate = observations as double[][];
            if (multivariate != null)
            {
                Fit(multivariate.Concatenate(), weights, options);
                return;
            }

            throw new ArgumentException("Invalid input type.", "observations");
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">
        ///     The weight vector containing the weight for each of the samples.
        /// </param>
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
        void IDistribution.Fit(Array observations, int[] weights, IFittingOptions options)
        {
            var univariate = observations as double[];
            if (univariate != null)
            {
                Fit(univariate, weights, options);
                return;
            }

            var multivariate = observations as double[][];
            if (multivariate != null)
            {
                Fit(multivariate.Concatenate(), weights, options);
                return;
            }

            throw new ArgumentException("Invalid input type.", "observations");
        }

        #endregion
    }
}