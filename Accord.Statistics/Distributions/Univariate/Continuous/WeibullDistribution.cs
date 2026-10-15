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
using Accord.Math.Random;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Weibull distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability theory and statistics, the Weibull distribution is a
    ///         continuous probability distribution. It is named after Waloddi Weibull,
    ///         who described it in detail in 1951, although it was first identified by
    ///         Fréchet (1927) and first applied by Rosin and Rammler (1933) to describe a
    ///         particle size distribution.
    ///     </para>
    ///     <para>
    ///         The Weibull distribution is related to a number of other probability distributions;
    ///         in particular, it interpolates between the
    ///         <see cref="ExponentialDistribution">
    ///             exponential distribution
    ///         </see>
    ///         (for k = 1) and the
    ///         <see cref="RayleighDistribution">
    ///             Rayleigh distribution
    ///         </see>
    ///         (when k = 2).
    ///     </para>
    ///     <para>
    ///         If the quantity x is a "time-to-failure", the Weibull distribution gives a
    ///         distribution for which the failure rate is proportional to a power of time.
    ///         The shape parameter, k, is that power plus one, and so this parameter can be
    ///         interpreted directly as follows:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 A value of k &lt; 1 indicates that the failure rate decreases over time. This
    ///                 happens if there is significant "infant mortality", or defective items failing
    ///                 early and the failure rate decreasing over time as the defective items are
    ///                 weeded out of the population.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 A value of k = 1 indicates that the failure rate is constant over time. This
    ///                 might suggest random external events are causing mortality, or failure.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 A value of k > 1 indicates that the failure rate increases with time. This
    ///                 happens if there is an "aging" process, or parts that are more likely to fail
    ///                 as time goes on.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         In the field of materials science, the shape parameter <c>k</c> of a distribution
    ///         of strengths is known as the Weibull modulus.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Weibull_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Weibull distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Weibull_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///    // Create a new Weibull distribution with λ = 0.42 and k = 1.2
    ///    var weilbull = new WeibullDistribution(scale: 0.42, shape: 1.2);
    ///    
    ///    // Common measures
    ///    double mean = weilbull.Mean;     // 0.39507546046784414
    ///    double median = weilbull.Median; // 0.30945951550913292
    ///    double var = weilbull.Variance;  // 0.10932249666369542
    ///    double mode = weilbull.Mode;     // 0.094360430821809421
    ///    
    ///    // Cumulative distribution functions
    ///    double cdf = weilbull.DistributionFunction(x: 1.4);           //  0.98560487188700052
    ///    double pdf = weilbull.ProbabilityDensityFunction(x: 1.4);     //  0.052326687031379278
    ///    double lpdf = weilbull.LogProbabilityDensityFunction(x: 1.4); // -2.9502487697674415
    ///    
    ///    // Probability density functions
    ///    double ccdf = weilbull.ComplementaryDistributionFunction(x: 1.4); // 0.22369885565908001
    ///    double icdf = weilbull.InverseDistributionFunction(p: cdf);       // 1.400000001051205
    ///    
    ///    // Hazard (failure rate) functions
    ///    double hf = weilbull.HazardFunction(x: 1.4);            // 1.1093328057258516
    ///    double chf = weilbull.CumulativeHazardFunction(x: 1.4); // 1.4974545260150962
    ///    
    ///    // String representation
    ///    string str = weilbull.ToString(CultureInfo.InvariantCulture); // Weibull(x; λ = 0.42, k = 1.2)
    /// </code>
    /// </example>
    [Serializable]
    public class WeibullDistribution : UnivariateContinuousDistribution,
        ISampleableDistribution<double>, IFormattable
    {
        // Distribution parameters

        /// <summary>
        ///     Initializes a new instance of the <see cref="WeibullDistribution" /> class.
        /// </summary>
        /// <param name="scale">The scale parameter λ (lambda).</param>
        /// <param name="shape">The shape parameter k.</param>
        public WeibullDistribution([Positive] double shape, [Positive] double scale)
        {
            if (shape <= 0) // k
            {
                throw new ArgumentOutOfRangeException("shape", "Shape (k) must be greater than zero.");
            }

            if (scale <= 0) // lambda
            {
                throw new ArgumentOutOfRangeException("scale", "Scale (lambda) must be greater than zero.");
            }

            Shape = shape;
            Scale = scale;
        }

        /// <summary>
        ///     Gets the shape parameter k.
        /// </summary>
        /// <value>The value for this distribution's shape parameter k.</value>
        public double Shape { get; private set; }

        /// <summary>
        ///     Gets the scale parameter  λ (lambda).
        /// </summary>
        /// <value>The value for this distribution's scale parameter λ (lambda).</value>
        public double Scale { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>The distribution's mean value.</value>
        public override double Mean
        {
            get => Scale * Gamma.Function(1 + 1 / Shape);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>The distribution's variance.</value>
        public override double Variance
        {
            get => Scale * Scale * Gamma.Function(1 + 2 / Shape) - Mean * Mean;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the median for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's median value.
        /// </value>
        public override double Median => Scale * System.Math.Pow(System.Math.Log(2.0), 1.0 / Shape);

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's mode value.
        /// </value>
        public override double Mode
        {
            get => Shape > 1 ? Scale * System.Math.Pow((Shape - 1) / Shape, 1 / Shape) : 0;
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
        ///     Gets the entropy for this distribution.
        /// </summary>
        /// <value>The distribution's entropy.</value>
        public override double Entropy
        {
            get => Constants.EulerGamma * (1 - 1 / Shape) + System.Math.Log(Scale / Shape) + 1;
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
            return string.Format(formatProvider, "Weibull(x; λ = {0}, k = {1})",
                Scale.ToString(format, formatProvider),
                Shape.ToString(format, formatProvider));
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new WeibullDistribution(Shape, Scale);
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
            var exp = System.Math.Exp(-System.Math.Pow(x / Scale, Shape));
            return 1.0 - exp;
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
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var a = System.Math.Pow(x / Scale, Shape - 1);
            var b = System.Math.Exp(-System.Math.Pow(x / Scale, Shape));
            return Shape / Scale * a * b;
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
            return System.Math.Log(Shape / Scale) + (Shape - 1) * System.Math.Log(x / Scale) -
                   System.Math.Pow(x / Scale, Shape);
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
        public override double HazardFunction(double x)
        {
            return Shape / Scale * System.Math.Pow(x / Scale, Shape - 1);
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
            return System.Math.Pow(x / Scale, Shape);
        }

        /// <summary>
        ///     Gets the complementary cumulative distribution function
        ///     (ccdf) for this distribution evaluated at point <c>x</c>.
        ///     This function is also known as the Survival function.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        protected internal override double InnerComplementaryDistributionFunction(double x)
        {
            return System.Math.Exp(-System.Math.Pow(x / Scale, Shape));
        }

        /// <summary>
        ///     Gets the inverse of the <see cref="UnivariateContinuousDistribution.ComplementaryDistributionFunction(Double)" />.
        ///     The inverse complementary distribution function is also known as the
        ///     inverse survival Function.
        /// </summary>
        public double InverseComplementaryDistributionFunction(double p)
        {
            return Scale * System.Math.Pow(-System.Math.Log(p), 1 / Shape);
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
            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }

            if (observations.Length < 2)
            {
                throw new ArgumentException("At least two observations are required.", "observations");
            }

            if (weights != null && weights.Length != observations.Length)
            {
                throw new DimensionMismatchException("weights",
                    "The weights vector must have the same length as the observations.");
            }

            double max = 0;
            double totalWeight = 0;
            double sumLog = 0;
            for (var i = 0; i < observations.Length; i++)
            {
                if (!(observations[i] > 0) || double.IsInfinity(observations[i]))
                {
                    throw new ArgumentOutOfRangeException("observations",
                        "Weibull observations must be positive and finite.");
                }

                max = System.Math.Max(max, observations[i]);
            }

            var scaled = new double[observations.Length];
            var logs = new double[observations.Length];
            for (var i = 0; i < observations.Length; i++)
            {
                var w = weights == null ? 1.0 : weights[i];
                scaled[i] = observations[i] / max;
                logs[i] = System.Math.Log(scaled[i]);
                totalWeight += w;
                sumLog += w * logs[i];
            }

            var meanLog = sumLog / totalWeight;

            Func<double, double> shapeEquation = shape =>
            {
                double num = 0;
                double den = 0;
                for (var i = 0; i < scaled.Length; i++)
                {
                    var w = weights == null ? 1.0 : weights[i];
                    var p = w * System.Math.Pow(scaled[i], shape);
                    num += p * logs[i];
                    den += p;
                }

                return num / den - 1.0 / shape - meanLog;
            };

            var lower = 1e-3;
            var upper = 1.0;
            while (shapeEquation(upper) < 0 && upper < 1e6)
            {
                upper *= 2;
            }

            for (var iteration = 0; iteration < 200; iteration++)
            {
                var mid = 0.5 * (lower + upper);
                if (shapeEquation(mid) < 0)
                {
                    lower = mid;
                }
                else
                {
                    upper = mid;
                }

                if (upper - lower < 1e-13 * upper)
                {
                    break;
                }
            }

            var kHat = 0.5 * (lower + upper);

            double sumPower = 0;
            for (var i = 0; i < scaled.Length; i++)
            {
                var w = weights == null ? 1.0 : weights[i];
                sumPower += w * System.Math.Pow(scaled[i], kHat);
            }

            Shape = kHat;
            Scale = max * System.Math.Pow(sumPower / totalWeight, 1.0 / kHat);
        }

        #region ISampleableDistribution<double> Members

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
        public override double[] Generate(int samples, double[] result, Random source)
        {
            return Random(Shape, Scale, samples, result, source);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random observations drawn from this distribution.</returns>
        public override double Generate(Random source)
        {
            return Random(Shape, Scale, source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Weibull distribution with the given parameters.
        /// </summary>
        /// <param name="scale">The scale parameter lambda.</param>
        /// <param name="shape">The shape parameter k.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <returns>An array of double values sampled from the specified Weibull distribution.</returns>
        public static double[] Random(double shape, double scale, int samples)
        {
            return Random(shape, scale, samples, new double[samples]);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Weibull distribution with the given parameters.
        /// </summary>
        /// <param name="scale">The scale parameter lambda.</param>
        /// <param name="shape">The shape parameter k.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>An array of double values sampled from the specified Weibull distribution.</returns>
        public static double[] Random(double shape, double scale, int samples, Random source)
        {
            return Random(shape, scale, samples, new double[samples], source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Weibull distribution with the given parameters.
        /// </summary>
        /// <param name="scale">The scale parameter lambda.</param>
        /// <param name="shape">The shape parameter k.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>An array of double values sampled from the specified Weibull distribution.</returns>
        public static double[] Random(double shape, double scale, int samples, double[] result)
        {
            return Random(shape, scale, samples, result, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Weibull distribution with the given parameters.
        /// </summary>
        /// <param name="scale">The scale parameter lambda.</param>
        /// <param name="shape">The shape parameter k.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>An array of double values sampled from the specified Weibull distribution.</returns>
        public static double[] Random(double shape, double scale, int samples, double[] result, Random source)
        {
            for (var i = 0; i < samples; i++)
            {
                result[i] = scale * System.Math.Pow(-System.Math.Log(source.NextDouble()), 1 / shape);
            }

            return result;
        }

        /// <summary>
        ///     Generates a random observation from the
        ///     Weibull distribution with the given parameters.
        /// </summary>
        /// <param name="scale">The scale parameter lambda.</param>
        /// <param name="shape">The shape parameter k.</param>
        /// <returns>A random double value sampled from the specified Weibull distribution.</returns>
        public static double Random(double shape, double scale)
        {
            return Random(shape, scale, Generator.Random);
        }

        /// <summary>
        ///     Generates a random observation from the
        ///     Weibull distribution with the given parameters.
        /// </summary>
        /// <param name="scale">The scale parameter lambda.</param>
        /// <param name="shape">The shape parameter k.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random double value sampled from the specified Weibull distribution.</returns>
        public static double Random(double shape, double scale, Random source)
        {
            return scale * System.Math.Pow(-System.Math.Log(source.NextDouble()), 1 / shape);
        }

        #endregion
    }
}