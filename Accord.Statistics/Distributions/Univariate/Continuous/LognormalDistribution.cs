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
    ///     Log-Normal (Galton) distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The log-normal distribution is a probability distribution of a random
    ///         variable whose logarithm is normally distributed.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Log-normal distribution.
    ///                     Available on: http://en.wikipedia.org/wiki/Log-normal_distribution
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     NIST/SEMATECH e-Handbook of Statistical Methods. Lognormal Distribution.
    ///                     Available on: http://www.itl.nist.gov/div898/handbook/eda/section3/eda3669.htm
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Weisstein, Eric W. "Normal Distribution Function." From MathWorld--A Wolfram Web
    ///                     Resource. http://mathworld.wolfram.com/NormalDistributionFunction.html
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // Create a new Log-normal distribution with μ = 2.79 and σ = 1.10
    ///   var log = new LognormalDistribution(location: 0.42, shape: 1.1);
    /// 
    ///   // Common measures
    ///   double mean = log.Mean;     // 2.7870954605658511
    ///   double median = log.Median; // 1.5219615583481305
    ///   double var = log.Variance;  // 18.28163603621158
    /// 
    ///   // Cumulative distribution functions
    ///   double cdf = log.DistributionFunction(x: 0.27);           // 0.057961222885664958
    ///   double ccdf = log.ComplementaryDistributionFunction(x: 0.27); // 0.942038777114335
    ///   double icdf = log.InverseDistributionFunction(p: cdf);        // 0.26999997937815973
    ///   
    ///   // Probability density functions
    ///   double pdf = log.ProbabilityDensityFunction(x: 0.27);     // 0.39035530085982068
    ///   double lpdf = log.LogProbabilityDensityFunction(x: 0.27); // -0.94069792674674835
    /// 
    ///   // Hazard (failure rate) functions
    ///   double hf = log.HazardFunction(x: 0.27);            // 0.41437285846720867
    ///   double chf = log.CumulativeHazardFunction(x: 0.27); // 0.059708840588116374
    /// 
    ///   // String representation
    ///   string str = log.ToString("N2", CultureInfo.InvariantCulture); // Lognormal(x; μ = 2.79, σ = 1.10)
    /// </code>
    /// </example>
    /// <seealso cref="NormalDistribution" />
    [Serializable]
    public class LognormalDistribution : UnivariateContinuousDistribution,
        IFittableDistribution<double, NormalOptions>,
        ISampleableDistribution<double>, IFormattable
    {
        private double constant; // 1/sqrt(2*pi*shape²)
        private double? entropy;

        private bool immutable;

        // Distribution parameters

        // Distribution measures
        private double? mean;

        // Derived measures
        private double? variance;

        /// <summary>
        ///     Constructs a Log-Normal (Galton) distribution
        ///     with zero location and unit shape.
        /// </summary>
        public LognormalDistribution()
        {
            initialize(Location, Shape, Shape * Shape);
        }

        /// <summary>
        ///     Constructs a Log-Normal (Galton) distribution
        ///     with given location and unit shape.
        /// </summary>
        /// <param name="location">The distribution's location value μ (mu).</param>
        public LognormalDistribution([Real] double location)
        {
            initialize(location, Shape, Shape * Shape);
        }

        /// <summary>
        ///     Constructs a Log-Normal (Galton) distribution
        ///     with given mean and standard deviation.
        /// </summary>
        /// <param name="location">The distribution's location value μ (mu).</param>
        /// <param name="shape">The distribution's shape deviation σ (sigma).</param>
        public LognormalDistribution([Real] double location, [Positive] double shape)
        {
            initialize(location, shape, shape * shape);
        }

        /// <summary>
        ///     Shape parameter σ (sigma) of
        ///     the log-normal distribution.
        /// </summary>
        public double Shape { get; private set; } = 1;

        /// <summary>
        ///     Squared shape parameter σ² (sigma-squared)
        ///     of the log-normal distribution.
        /// </summary>
        public double Shape2 { get; private set; }

        /// <summary>
        ///     Location parameter μ (mu) of the log-normal distribution.
        /// </summary>
        public double Location { get; private set; }

        /// <summary>
        ///     Gets the Mean for this Log-Normal distribution.
        /// </summary>
        /// <remarks>
        ///     The Lognormal distribution's mean is
        ///     defined as <c>exp(μ + σ²/2).</c>
        /// </remarks>
        public override double Mean
        {
            get
            {
                if (mean == null)
                {
                    mean = System.Math.Exp(Location + Shape2 / 2.0);
                }

                return mean.Value;
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the Variance (the square of the standard
        ///     deviation) for this Log-Normal distribution.
        /// </summary>
        /// <remarks>
        ///     The Lognormal distribution's variance is
        ///     defined as <c>(exp(σ²) - 1) * exp(2*μ + σ²)</c>.
        /// </remarks>
        public override double Variance
        {
            get
            {
                if (variance == null)
                {
                    variance = (System.Math.Exp(Shape2) - 1.0) * System.Math.Exp(2 * Location + Shape2);
                }

                return variance.Value;
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
            get => System.Math.Exp(Location - Shape2);
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
        ///     Gets the Entropy for this Log-Normal distribution.
        /// </summary>
        public override double Entropy
        {
            get
            {
                if (entropy == null)
                {
                    entropy = 0.5 + 0.5 * System.Math.Log(2.0 * System.Math.PI * Shape2) + Location;
                }

                return entropy.Value;
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the Standard Log-Normal Distribution,
        ///     with location set to zero and unit shape.
        /// </summary>
        public static LognormalDistribution Standard { get; } = new() { immutable = true };

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
        public void Fit(double[] observations, double[] weights, NormalOptions options)
        {
            if (immutable)
            {
                throw new InvalidOperationException();
            }

            double mu, var;

            observations = observations.Log();

            if (weights != null)
            {
#if DEBUG
                for (var i = 0; i < weights.Length; i++)
                {
                    if (double.IsNaN(weights[i]) || double.IsInfinity(weights[i]))
                    {
                        throw new ArgumentException("Invalid numbers in the weight vector.", "weights");
                    }
                }
#endif

                // Compute weighted mean
                mu = observations.WeightedMean(weights);

                // Compute weighted variance
                var = observations.WeightedVariance(weights, mu);
            }
            else
            {
                // Compute weighted mean
                mu = observations.Mean();

                // Compute weighted variance
                var = observations.Variance(mu);
            }

            if (options != null)
            {
                // Parse optional estimation options
                var regularization = options.Regularization;

                if (var == 0 || double.IsNaN(var) || double.IsInfinity(var))
                {
                    var = regularization;
                }
            }

            initialize(mu, System.Math.Sqrt(var), var);
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new LognormalDistribution(Location, Shape);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Lognormal(x; μ = {0}, σ = {1})",
                Mean.ToString(format, formatProvider),
                Shape.ToString(format, formatProvider));
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     the this Log-Normal distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         The Cumulative Distribution Function (CDF) describes the cumulative
        ///         probability that a given value or any value smaller than it will occur.
        ///     </para>
        ///     <para>
        ///         The calculation is computed through the relationship to the error function
        ///         as <see cref="Special.Erfc">erfc</see>(-z/sqrt(2)) / 2. See
        ///         [Weisstein] for more details.
        ///     </para>
        ///     <para>
        ///         References:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     Weisstein, Eric W. "Normal Distribution Function." From MathWorld--A Wolfram Web
        ///                     Resource. http://mathworld.wolfram.com/NormalDistributionFunction.html
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        /// <example>
        ///     See <see cref="LognormalDistribution" />.
        /// </example>
        protected internal override double InnerDistributionFunction(double x)
        {
            var z = (System.Math.Log(x) - Location) / Shape;
            return 0.5 * Special.Erfc(-z / Constants.Sqrt2);
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     the Normal distribution evaluated at point <c>x</c>.
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
        /// <example>
        ///     See <see cref="LognormalDistribution" />.
        /// </example>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var z = (System.Math.Log(x) - Location) / Shape;
            return constant * System.Math.Exp(-z * z * 0.5) / x;
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
        ///     See <see cref="LognormalDistribution" />.
        /// </example>
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            var z = (System.Math.Log(x) - Location) / Shape;
            return System.Math.Log(constant) + -z * z * 0.5 - System.Math.Log(x);
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
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            var normalOptions = options as NormalOptions;
            if (options != null && normalOptions == null)
            {
                throw new ArgumentException("The specified options' type is invalid.", "options");
            }

            Fit(observations, weights, normalOptions);
        }

        private void initialize(double mu, double dev, double var)
        {
            Location = mu;
            Shape = dev;
            Shape2 = var;

            // Compute derived values
            constant = 1.0 / (Constants.Sqrt2PI * dev);
        }

        /// <summary>
        ///     Estimates a new Log-Normal distribution from a given set of observations.
        /// </summary>
        public static LognormalDistribution Estimate(double[] observations)
        {
            return Estimate(observations, null, null);
        }

        /// <summary>
        ///     Estimates a new Log-Normal distribution from a given set of observations.
        /// </summary>
        public static LognormalDistribution Estimate(double[] observations, NormalOptions options)
        {
            return Estimate(observations, null, options);
        }

        /// <summary>
        ///     Estimates a new Log-Normal distribution from a given set of observations.
        /// </summary>
        public static LognormalDistribution Estimate(double[] observations, double[] weights,
            NormalOptions options = null)
        {
            var n = new LognormalDistribution();
            n.Fit(observations, weights, options);
            return n;
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
            return Random(Location, Shape, samples, result, source);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public override double Generate(Random source)
        {
            return Random(Location, Shape, source);
        }

        /// <summary>
        ///     Generates a random observation from the
        ///     Lognormal distribution with the given parameters.
        /// </summary>
        /// <param name="location">The distribution's location value.</param>
        /// <param name="shape">The distribution's shape deviation.</param>
        /// <returns>A random double value sampled from the specified Lognormal distribution.</returns>
        public static double Random(double location, double shape)
        {
            return Random(location, shape, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Lognormal distribution with the given parameters.
        /// </summary>
        /// <param name="location">The distribution's location value.</param>
        /// <param name="shape">The distribution's shape deviation.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <returns>An array of double values sampled from the specified Lognormal distribution.</returns>
        public static double[] Random(double location, double shape, int samples)
        {
            return Random(location, shape, samples, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Lognormal distribution with the given parameters.
        /// </summary>
        /// <param name="location">The distribution's location value.</param>
        /// <param name="shape">The distribution's shape deviation.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>An array of double values sampled from the specified Lognormal distribution.</returns>
        public static double[] Random(double location, double shape, int samples, double[] result)
        {
            return Random(location, shape, samples, result, Generator.Random);
        }

        /// <summary>
        ///     Generates a random observation from the
        ///     Lognormal distribution with the given parameters.
        /// </summary>
        /// <param name="location">The distribution's location value.</param>
        /// <param name="shape">The distribution's shape deviation.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random double value sampled from the specified Lognormal distribution.</returns>
        public static double Random(double location, double shape, Random source)
        {
            return System.Math.Exp(location + shape * NormalDistribution.Random(source));
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Lognormal distribution with the given parameters.
        /// </summary>
        /// <param name="location">The distribution's location value.</param>
        /// <param name="shape">The distribution's shape deviation.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>An array of double values sampled from the specified Lognormal distribution.</returns>
        public static double[] Random(double location, double shape, int samples, Random source)
        {
            return Random(location, shape, samples, new double[samples], source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Lognormal distribution with the given parameters.
        /// </summary>
        /// <param name="location">The distribution's location value.</param>
        /// <param name="shape">The distribution's shape deviation.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>An array of double values sampled from the specified Lognormal distribution.</returns>
        public static double[] Random(double location, double shape, int samples, double[] result, Random source)
        {
            NormalDistribution.Random(samples, result, source);

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = System.Math.Exp(location + shape * result[i]);
            }

            return result;
        }

        #endregion
    }
}