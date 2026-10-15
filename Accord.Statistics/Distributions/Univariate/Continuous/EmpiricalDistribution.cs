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
    ///     Empirical distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Empirical distributions are based solely on the data. This class
    ///         uses the empirical distribution function and the Gaussian kernel
    ///         density estimation to provide an univariate continuous distribution
    ///         implementation which depends only on sampled data.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Empirical Distribution Function. Available on:
    ///                     <a href=" http://en.wikipedia.org/wiki/Empirical_distribution_function">
    ///                         http://en.wikipedia.org/wiki/Empirical_distribution_function
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     PlanetMath. Empirical Distribution Function. Available on:
    ///                     <a href="http://planetmath.org/encyclopedia/EmpiricalDistributionFunction.html">
    ///                         http://planetmath.org/encyclopedia/EmpiricalDistributionFunction.html
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Kernel Density Estimation. Available on:
    ///                     <a href="http://en.wikipedia.org/wiki/Kernel_density_estimation">
    ///                         http://en.wikipedia.org/wiki/Kernel_density_estimation
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Bishop, Christopher M.; Pattern Recognition and Machine Learning.
    ///                     Springer; 1st ed. 2006.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to build an empirical distribution directly from a sample:
    ///     </para>
    ///     <code>
    ///   // Consider the following univariate samples
    ///   double[] samples = { 5, 5, 1, 4, 1, 2, 2, 3, 3, 3, 4, 3, 3, 3, 4, 3, 2, 3 };
    ///   
    ///   // Create a non-parametric, empirical distribution using those samples:
    ///   EmpiricalDistribution distribution = new EmpiricalDistribution(samples);
    ///     
    ///   // Common measures
    ///   double mean   = distribution.Mean;     // 3
    ///   double median = distribution.Median;   // 2.9999993064186787
    ///   double var    = distribution.Variance; // 1.2941176470588236
    ///   
    ///   // Cumulative distribution function
    ///   double cdf  = distribution.DistributionFunction(x: 4.2);          // 0.88888888888888884
    ///   double ccdf = distribution.ComplementaryDistributionFunction(x: 4.2); //0.11111111111111116
    ///   double icdf = distribution.InverseDistributionFunction(p: cdf);       // 4.1999999999999993
    ///   
    ///   // Probability density functions
    ///   double pdf  = distribution.ProbabilityDensityFunction(x: 4.2);    // 0.15552784414141974
    ///   double lpdf = distribution.LogProbabilityDensityFunction(x: 4.2); // -1.8609305013898356
    ///   
    ///   // Hazard (failure rate) functions
    ///   double hf  = distribution.HazardFunction(x: 4.2);           // 1.3997505972727771
    ///   double chf = distribution.CumulativeHazardFunction(x: 4.2); // 2.1972245773362191
    /// 
    ///   // Automatically estimated smooth parameter (gamma)
    ///   double smoothing = distribution.Smoothing; // 1.9144923416414432
    /// 
    ///   // String representation
    ///   string str = distribution.ToString(CultureInfo.InvariantCulture); // Fn(x; S)
    /// </code>
    /// </example>
    /// <seealso cref="EmpiricalHazardDistribution" />
    [Serializable]
    public class EmpiricalDistribution : UnivariateContinuousDistribution,
        IFittableDistribution<double, EmpiricalOptions>,
        ISampleableDistribution<double>
    {
        private double constant;
        private double? entropy;

        // Derived measures
        private double? mean;
        private double? mode;

        private int[] repeats;

        // Distribution parameters

        private double sumOfWeights;

        private WeightType type;
        private double? variance;
        private double[] weights;

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="samples">The data samples.</param>
        public EmpiricalDistribution(double[] samples)
        {
            initialize(samples, null, null, null);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="samples">The data samples.</param>
        /// <param name="smoothing">
        ///     The kernel smoothing or bandwidth to be used in density estimation.
        ///     By default, the normal distribution approximation will be used.
        /// </param>
        public EmpiricalDistribution(double[] samples, double smoothing)
        {
            initialize(samples, null, null, smoothing);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="samples">The data samples.</param>
        /// <param name="weights">
        ///     The fractional weights to use for the samples.
        ///     The weights must sum up to one.
        /// </param>
        public EmpiricalDistribution(double[] samples, double[] weights)
        {
            initialize(samples, weights, null, null);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="samples">The data samples.</param>
        /// <param name="weights">The number of repetition counts for each sample.</param>
        public EmpiricalDistribution(double[] samples, int[] weights)
        {
            initialize(samples, null, weights, null);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="samples">The data samples.</param>
        /// <param name="weights">
        ///     The fractional weights to use for the samples.
        ///     The weights must sum up to one.
        /// </param>
        /// <param name="smoothing">
        ///     The kernel smoothing or bandwidth to be used in density estimation.
        ///     By default, the normal distribution approximation will be used.
        /// </param>
        public EmpiricalDistribution(double[] samples, double[] weights, double smoothing)
        {
            initialize(samples, weights, null, smoothing);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="samples">The data samples.</param>
        /// <param name="smoothing">
        ///     The kernel smoothing or bandwidth to be used in density estimation.
        ///     By default, the normal distribution approximation will be used.
        /// </param>
        /// <param name="weights">The number of repetition counts for each sample.</param>
        public EmpiricalDistribution(double[] samples, int[] weights, double smoothing)
        {
            initialize(samples, null, weights, smoothing);
        }

        private EmpiricalDistribution()
        {
        }

        /// <summary>
        ///     Gets the samples giving this empirical distribution.
        /// </summary>
        public double[] Samples { get; private set; }

        /// <summary>
        ///     Gets the fractional weights associated with each sample. Note that
        ///     changing values on this array will not result int any effect in
        ///     this distribution. The distribution must be computed from scratch
        ///     with new values in case new weights needs to be used.
        /// </summary>
        public double[] Weights
        {
            get
            {
                if (weights == null)
                {
                    weights = new double[Samples.Length];
                    for (var i = 0; i < weights.Length; i++)
                    {
                        weights[i] = Counts[i] / (double)Length;
                    }
                }

                return weights;
            }
        }

        /// <summary>
        ///     Gets the repetition counts associated with each sample. Note that
        ///     changing values on this array will not result int any effect in
        ///     this distribution. The distribution must be computed from scratch
        ///     with new values in case new weights needs to be used.
        /// </summary>
        public int[] Counts
        {
            get
            {
                if (repeats == null)
                {
                    repeats = new int[Samples.Length];
                    for (var i = 0; i < repeats.Length; i++)
                    {
                        repeats[i] = 1;
                    }
                }

                return repeats;
            }
        }

        /// <summary>
        ///     Gets the total number of samples in this distribution.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        ///     Gets the bandwidth smoothing parameter
        ///     used in the kernel density estimation.
        /// </summary>
        public double Smoothing { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <example>
        ///     See <see cref="EmpiricalDistribution" />.
        /// </example>
        public override double Mean
        {
            get
            {
                if (mean == null)
                {
                    if (type == WeightType.None)
                    {
                        mean = Samples.Mean();
                    }

                    else if (type == WeightType.Repetition)
                    {
                        mean = Samples.WeightedMean(repeats);
                    }

                    else if (type == WeightType.Fraction)
                    {
                        mean = Samples.WeightedMean(weights);
                    }
                }

                return mean.Value;
            }
            set => throw new NotImplementedException();
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
                if (mode == null)
                {
                    if (type == WeightType.None)
                    {
                        mode = Samples.Mode();
                    }

                    else if (type == WeightType.Repetition)
                    {
                        mode = Samples.WeightedMode(repeats);
                    }

                    else if (type == WeightType.Fraction)
                    {
                        mode = Samples.WeightedMode(weights);
                    }
                }

                return mode.Value;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <example>
        ///     See <see cref="EmpiricalDistribution" />.
        /// </example>
        public override double Variance
        {
            get
            {
                if (variance == null)
                {
                    if (type == WeightType.None)
                    {
                        variance = Samples.Variance();
                    }

                    else if (type == WeightType.Repetition)
                    {
                        variance = Samples.WeightedVariance(repeats);
                    }

                    else if (type == WeightType.Fraction)
                    {
                        variance = Samples.WeightedVariance(weights);
                    }
                }

                return variance.Value;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        public override double Entropy
        {
            get
            {
                if (entropy == null)
                {
                    if (type == WeightType.None)
                    {
                        entropy = Samples.Entropy(ProbabilityDensityFunction);
                    }

                    else if (type == WeightType.Repetition)
                    {
                        entropy = Samples.WeightedEntropy(repeats, ProbabilityDensityFunction);
                    }

                    else if (type == WeightType.Fraction)
                    {
                        entropy = Samples.WeightedEntropy(weights, ProbabilityDensityFunction);
                    }
                }

                return entropy.Value;
            }
            set => throw new NotImplementedException();
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
            set => throw new NotImplementedException();
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
        public void Fit(double[] observations, double[] weights, EmpiricalOptions options)
        {
            double? smoothing = null;
            var inPlace = false;

            if (options != null)
            {
                smoothing = options.SmoothingRule(observations, weights);
                inPlace = options.InPlace;
            }

            if (!inPlace)
            {
                observations = (double[])observations.Clone();

                if (weights != null)
                {
                    weights = (double[])weights.Clone();
                }
            }

            initialize(observations, weights, null, smoothing);
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            var clone = new EmpiricalDistribution();

            clone.type = type;
            clone.sumOfWeights = sumOfWeights;
            clone.Length = Length;
            clone.Smoothing = Smoothing;
            clone.constant = constant;

            clone.Samples = (double[])Samples.Clone();

            if (weights != null)
            {
                clone.weights = (double[])weights.Clone();
            }

            if (repeats != null)
            {
                clone.repeats = (int[])repeats.Clone();
            }

            return clone;
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
        public override double[] Generate(int samples, double[] result, Random source)
        {
            if (weights == null)
            {
                for (var i = 0; i < samples; i++)
                {
                    result[i] = this.Samples[source.Next(this.Samples.Length)];
                }

                return result;
            }

            var u = source.NextDouble();
            var uniform = u * sumOfWeights;

            for (var i = 0; i < samples; i++)
            {
                double cumulativeSum = 0;
                for (var j = 0; j < weights.Length; j++)
                {
                    cumulativeSum += weights[j];

                    if (uniform < cumulativeSum)
                    {
                        result[i] = this.Samples[j];
                        break;
                    }
                }
            }

            return result;
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
            if (weights == null)
            {
                return Samples[source.Next(Samples.Length)];
            }


            var u = source.NextDouble();
            var uniform = u * sumOfWeights;

            double cumulativeSum = 0;
            for (var i = 0; i < weights.Length; i++)
            {
                cumulativeSum += weights[i];

                if (uniform < cumulativeSum)
                {
                    return Samples[i];
                }
            }

            throw new InvalidOperationException("Execution should never reach here.");
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
        /// <example>
        ///     See <see cref="EmpiricalDistribution" />.
        /// </example>
        protected internal override double InnerDistributionFunction(double x)
        {
            if (type == WeightType.None)
            {
                var sum = 0; // Normal sample, no weights
                for (var i = 0; i < Samples.Length; i++)
                {
                    if (Samples[i] <= x)
                    {
                        sum++;
                    }
                }

                return sum / (double)Length;
            }

            if (type == WeightType.Repetition)
            {
                var sum = 0; // Repetition counts weights
                for (var i = 0; i < Samples.Length; i++)
                {
                    if (Samples[i] <= x)
                    {
                        sum += repeats[i];
                    }
                }

                return sum / (double)Length;
            }

            if (type == WeightType.Fraction)
            {
                double sum = 0; // Fractional weights
                for (var i = 0; i < Samples.Length; i++)
                {
                    if (Samples[i] <= x)
                    {
                        sum += weights[i];
                    }
                }

                return sum / sumOfWeights;
            }

            throw new InvalidOperationException();
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
        /// <example>
        ///     See <see cref="EmpiricalDistribution" />.
        /// </example>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            // References:
            //  - Bishop, Christopher M.; Pattern Recognition and Machine Learning. 

            double p = 0;

            if (type == WeightType.None)
            {
                // Normal samples, not using any weights
                for (var i = 0; i < Samples.Length; i++)
                {
                    var z = (x - Samples[i]) / Smoothing;
                    p += System.Math.Exp(-z * z * 0.5);
                }
            }

            else if (type == WeightType.Repetition)
            {
                // Weighted sample using discrete counts
                for (var i = 0; i < Samples.Length; i++)
                {
                    var z = (x - Samples[i]) / Smoothing;
                    p += repeats[i] * System.Math.Exp(-z * z * 0.5);
                }
            }

            else if (type == WeightType.Fraction)
            {
                // Weighted sample using fractional weights
                for (var i = 0; i < Samples.Length; i++)
                {
                    var z = (x - Samples[i]) / Smoothing;
                    p += weights[i] * System.Math.Exp(-z * z * 0.5);
                }
            }

            return p * constant;
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
            Fit(observations, weights, options as EmpiricalOptions);
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
        public void Fit(double[] observations, int[] weights, EmpiricalOptions options)
        {
            double? smoothing = null;
            var inPlace = false;

            if (options != null)
            {
                smoothing = options.SmoothingRule(observations, null, weights);
                inPlace = options.InPlace;
            }

            if (!inPlace)
            {
                observations = (double[])observations.Clone();

                if (weights != null)
                {
                    weights = (int[])weights.Clone();
                }
            }

            initialize(observations, null, weights, smoothing);
        }

        private void initialize(double[] observations, double[] weights, int[] repeats, double? smoothing)
        {
            if (smoothing == null)
            {
                smoothing = SmoothingRule(observations, weights, repeats);
            }

            Samples = observations;
            this.weights = weights;
            this.repeats = repeats;
            this.Smoothing = smoothing.Value;


            if (weights != null)
            {
                type = WeightType.Fraction;
                Length = Samples.Length;
                sumOfWeights = weights.Sum();
                constant = 1.0 / (Constants.Sqrt2PI * this.Smoothing);
            }
            else if (repeats != null)
            {
                type = WeightType.Repetition;
                Length = repeats.Sum();
                sumOfWeights = 1.0;
                constant = 1.0 / (Constants.Sqrt2PI * this.Smoothing * Length);
            }
            else
            {
                type = WeightType.None;
                Length = Samples.Length;
                sumOfWeights = 1.0;
                constant = 1.0 / (Constants.Sqrt2PI * this.Smoothing * Length);
            }


            mean = null;
            variance = null;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Fn(x; S)");
        }

        /// <summary>
        ///     Gets the default estimative of the smoothing parameter.
        /// </summary>
        /// <remarks>
        ///     This method is based on the practical estimation of the bandwidth as
        ///     suggested in Wikipedia: http://en.wikipedia.org/wiki/Kernel_density_estimation
        /// </remarks>
        /// <param name="observations">The observations for the empirical distribution.</param>
        /// <returns>An estimative of the smoothing parameter.</returns>
        public static double SmoothingRule(double[] observations)
        {
            var sigma = observations.StandardDeviation();
            return sigma * System.Math.Pow(4.0 / (3.0 * observations.Length), 1.0 / 5.0);
        }

        /// <summary>
        ///     Gets the default estimative of the smoothing parameter.
        /// </summary>
        /// <remarks>
        ///     This method is based on the practical estimation of the bandwidth as
        ///     suggested in Wikipedia: http://en.wikipedia.org/wiki/Kernel_density_estimation
        /// </remarks>
        /// <param name="observations">The observations for the empirical distribution.</param>
        /// <param name="weights">The fractional importance for each sample. Those values must sum up to one.</param>
        /// <returns>An estimative of the smoothing parameter.</returns>
        public static double SmoothingRule(double[] observations, double[] weights)
        {
            var N = weights.Sum();
            var sigma = observations.WeightedStandardDeviation(weights);
            return sigma * System.Math.Pow(4.0 / (3.0 * N), 1.0 / 5.0);
        }

        /// <summary>
        ///     Gets the default estimative of the smoothing parameter.
        /// </summary>
        /// <remarks>
        ///     This method is based on the practical estimation of the bandwidth as
        ///     suggested in Wikipedia: http://en.wikipedia.org/wiki/Kernel_density_estimation
        /// </remarks>
        /// <param name="observations">The observations for the empirical distribution.</param>
        /// <param name="repeats">The number of times each sample should be repeated.</param>
        /// <returns>An estimative of the smoothing parameter.</returns>
        public static double SmoothingRule(double[] observations, int[] repeats)
        {
            double N = repeats.Sum();
            var sigma = observations.WeightedStandardDeviation(repeats);
            return sigma * System.Math.Pow(4.0 / (3.0 * N), 1.0 / 5.0);
        }

        /// <summary>
        ///     Gets the default estimative of the smoothing parameter.
        /// </summary>
        /// <remarks>
        ///     This method is based on the practical estimation of the bandwidth as
        ///     suggested in Wikipedia: http://en.wikipedia.org/wiki/Kernel_density_estimation
        /// </remarks>
        /// <param name="observations">The observations for the empirical distribution.</param>
        /// <param name="weights">The fractional importance for each sample. Those values must sum up to one.</param>
        /// <param name="repeats">The number of times each sample should be repeated.</param>
        /// <returns>An estimative of the smoothing parameter.</returns>
        public static double SmoothingRule(double[] observations, double[] weights, int[] repeats)
        {
            if (weights != null)
            {
                if (repeats != null)
                {
                    throw new ArgumentException("Either weights or repeats can be different from null.");
                }

                return SmoothingRule(observations, weights);
            }

            if (repeats != null)
            {
                if (weights != null)
                {
                    throw new ArgumentException("Either weights or repeats can be different from null.");
                }

                return SmoothingRule(observations, repeats);
            }

            return SmoothingRule(observations);
        }
    }
}