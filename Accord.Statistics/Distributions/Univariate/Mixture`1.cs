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
using System.Linq;
using System.Text;
using Accord.Math;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Mixture of univariate probability distributions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A mixture density is a probability density function which is expressed
    ///         as a convex combination (i.e. a weighted sum, with non-negative weights
    ///         that sum to 1) of other probability density functions. The individual
    ///         density functions that are combined to make the mixture density are
    ///         called the mixture components, and the weights associated with each
    ///         component are called the mixture weights.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Mixture_density">
    ///                         Wikipedia, The Free Encyclopedia. Mixture density. Available on:
    ///                         http://en.wikipedia.org/wiki/Mixture_density
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">
    ///     The type of the univariate component distributions.
    /// </typeparam>
    /// <example>
    ///     <code>
    ///   // Create a new mixture containing two Normal distributions
    ///   Mixture&lt;NormalDistribution> mix = new Mixture&lt;NormalDistribution>(
    ///       new NormalDistribution(2, 1), new NormalDistribution(5, 1));
    ///   
    ///   // Common measures
    ///   double mean   = mix.Mean;     // 3.5
    ///   double median = mix.Median;   // 3.4999998506015895
    ///   double var    = mix.Variance; // 3.25
    ///   
    ///   // Cumulative distribution functions
    ///   double cdf = mix.DistributionFunction(x: 4.2);               // 0.59897597553494908
    ///   double ccdf = mix.ComplementaryDistributionFunction(x: 4.2); // 0.40102402446505092
    ///   
    ///   // Probability mass functions
    ///   double pmf1 = mix.ProbabilityDensityFunction(x: 1.2); // 0.14499174984363708
    ///   double pmf2 = mix.ProbabilityDensityFunction(x: 2.3); // 0.19590437513747333
    ///   double pmf3 = mix.ProbabilityDensityFunction(x: 3.7); // 0.13270883471234715
    ///   double lpmf = mix.LogProbabilityDensityFunction(x: 4.2); // -1.8165661905848629
    ///   
    ///   // Quantile function
    ///   double icdf1 = mix.InverseDistributionFunction(p: 0.17); // 1.5866611690305095
    ///   double icdf2 = mix.InverseDistributionFunction(p: 0.46); // 3.1968506765456883
    ///   double icdf3 = mix.InverseDistributionFunction(p: 0.87); // 5.6437596300843076
    ///   
    ///   // Hazard (failure rate) functions
    ///   double hf = mix.HazardFunction(x: 4.2);            // 0.40541978256972522
    ///   double chf = mix.CumulativeHazardFunction(x: 4.2); // 0.91373394208601633
    ///   
    ///   // String representation:
    ///   // Mixture(x; 0.5 * N(x; μ = 5, σ² = 1) + 0.5 * N(x; μ = 5, σ² = 1))
    ///   string str = mix.ToString(CultureInfo.InvariantCulture);
    /// </code>
    ///     <para>
    ///         The following example shows how to estimate (fit) a Mixture of Normal distributions
    ///         from weighted data:
    ///     </para>
    ///     <code>
    /// // Randomly initialize some mixture components
    /// NormalDistribution[] components = new NormalDistribution[2];
    /// components[0] = new NormalDistribution(2, 1);
    /// components[1] = new NormalDistribution(5, 1);
    /// 
    /// // Create an initial mixture
    /// var mixture = new Mixture&lt;NormalDistribution>(components);
    /// 
    /// // Now, suppose we have a weighted data
    /// // set. Those will be the input points:
    /// 
    /// double[] points = { 0, 3, 1, 7, 3, 5, 1, 2, -1, 2, 7, 6, 8, 6 }; // (14 points)
    /// 
    /// // And those are their respective unnormalized weights:
    /// double[] weights = { 1, 1, 1, 2, 2, 1, 1, 1, 2, 1, 2, 3, 1, 1 }; // (14 weights)
    /// 
    /// // Let's normalize the weights so they sum up to one:
    /// weights = weights.Divide(weights.Sum());
    /// 
    /// // Now we can fit our model to the data:
    /// mixture.Fit(points, weights);   // done!
    /// 
    /// // Our model will be:
    /// double mean1 = mixture.Components[0].Mean; // 1.41126
    /// double mean2 = mixture.Components[1].Mean; // 6.53301
    /// 
    /// // With mixture weights
    /// double pi1 = mixture.Coefficients[0]; // 0.51408
    /// double pi2 = mixture.Coefficients[0]; // 0.48591
    /// 
    /// // If we need the GaussianMixtureModel functionality, we can
    /// // use the estimated mixture to initialize a new model:
    /// GaussianMixtureModel gmm = new GaussianMixtureModel(mixture);
    /// 
    /// mean1 = gmm.Gaussians[0].Mean[0]; // 1.41126 (same)
    /// mean2 = gmm.Gaussians[1].Mean[0]; // 6.53301 (same)
    /// 
    /// p1 = gmm.Gaussians[0].Proportion; // 0.51408 (same)
    /// p2 = gmm.Gaussians[1].Proportion; // 0.48591 (same)
    /// </code>
    /// </example>
    /// <seealso cref="MultivariateMixture{T}" />
    /// <seealso cref="ExpectationMaximization{T}" />
    /// <seealso cref="LogExpectationMaximization{T}" />
    [Serializable]
    public class Mixture<T> : UnivariateContinuousDistribution, IMixture<T>,
        IFittableDistribution<double, MixtureOptions>,
        ISampleableDistribution<double>
        where T : IUnivariateDistribution<double>
    {
        // cache
        private IDistribution<double>[] cache;

        // distribution parameters

        // distributions measures
        private double? mean;
        private DoubleRange? range;
        private ISampleableDistribution<double>[] sampleable;
        private double? variance;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Mixture&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="components">The mixture distribution components.</param>
        public Mixture(params T[] components)
        {
            if (components == null)
            {
                throw new ArgumentNullException("components");
            }

            this.Components = components;

            Coefficients = new double[components.Length];
            for (var i = 0; i < Coefficients.Length; i++)
            {
                Coefficients[i] = 1.0 / Coefficients.Length;
            }

            cache = new IDistribution<double>[Coefficients.Length];
            for (var i = 0; i < cache.Length; i++)
            {
                cache[i] = components[i];
            }

            initialize();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Mixture&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="coefficients">The mixture weight coefficients.</param>
        /// <param name="components">The mixture distribution components.</param>
        public Mixture(double[] coefficients, params T[] components)
        {
            if (components == null)
            {
                throw new ArgumentNullException("components");
            }

            if (coefficients == null)
            {
                throw new ArgumentNullException("coefficients");
            }

            if (coefficients.Length != components.Length)
            {
                throw new ArgumentException(
                    "The coefficient and component arrays should have the same length.",
                    "components");
            }

            this.Components = components;
            this.Coefficients = coefficients;

            cache = new IDistribution<double>[coefficients.Length];
            for (var i = 0; i < cache.Length; i++)
            {
                cache[i] = components[i];
            }

            initialize();
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        public override double Mean
        {
            get
            {
                if (mean == null)
                {
                    var mu = 0.0;
                    for (var i = 0; i < Coefficients.Length; i++)
                    {
                        mu += Coefficients[i] * Components[i].Mean;
                    }

                    mean = mu;
                }

                return mean.Value;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <remarks>
        ///     References: Lidija Trailovic and Lucy Y. Pao, Variance Estimation and
        ///     Ranking of Gaussian Mixture Distributions in Target Tracking
        ///     Applications, Department of Electrical and Computer Engineering
        /// </remarks>
        public override double Variance
        {
            get
            {
                if (variance == null)
                {
                    var mu = Mean;
                    var var = 0.0;
                    for (var i = 0; i < Coefficients.Length; i++)
                    {
                        var w = Coefficients[i];
                        var m = Components[i].Mean;
                        var v = Components[i].Variance;
                        var += w * (v + m * m);
                    }

                    variance = var - mu * mu;
                }

                return variance.Value;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     This method is not supported.
        /// </summary>
        public override double Mode
        {
            get => throw new NotSupportedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     This method is not supported.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
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
            get
            {
                if (range == null)
                {
                    var min = Components.Min(p => p.Support.Min);
                    var max = Components.Max(p => p.Support.Max);
                    range = new DoubleRange(min, max);
                }

                return range.Value;
            }
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
        public void Fit(double[] observations, double[] weights, MixtureOptions options)
        {
            var pdf = new IFittableDistribution<double>[Coefficients.Length];
            for (var i = 0; i < Components.Length; i++)
            {
                pdf[i] = (IFittableDistribution<double>)Components[i];
            }

            var log = options != null && options.Logarithm;

            if (log)
            {
                if (weights != null)
                {
                    throw new ArgumentException("The model fitting algorithm does not"
                                                + " currently support different weights when the logarithm option"
                                                + " is enabled. To avoid this exception, pass 'null' as the second"
                                                + " parameter's value when calling this method.");
                }

                var em = new LogExpectationMaximization<double>(Coefficients, pdf);

                if (options != null)
                {
                    em.InnerOptions = options.InnerOptions;
                    em.Convergence.MaxIterations = options.MaxIterations;
                    em.Convergence.Tolerance = options.Threshold;
                    em.ParallelOptions = options.ParallelOptions;
                }

                em.Compute(observations);

#pragma warning disable 612, 618
                if (options != null)
                {
                    options.Iterations = em.Convergence.CurrentIteration;
                }
#pragma warning restore 612, 618
            }
            else
            {
                var em = new ExpectationMaximization<double>(Coefficients, pdf);

                if (options != null)
                {
                    em.InnerOptions = options.InnerOptions;
                    em.Convergence.MaxIterations = options.MaxIterations;
                    em.Convergence.Tolerance = options.Threshold;
                    em.ParallelOptions = options.ParallelOptions;
                }

                em.Compute(observations, weights);

#pragma warning disable 612, 618
                if (options != null)
                {
                    options.Iterations = em.Convergence.CurrentIteration;
                }
#pragma warning restore 612, 618
            }

            for (var i = 0; i < Components.Length; i++)
            {
                cache[i] = Components[i] = (T)pdf[i];
            }

            initialize();
        }

        /// <summary>
        ///     Gets the mixture components.
        /// </summary>
        public T[] Components { get; }

        /// <summary>
        ///     Gets the weight coefficients.
        /// </summary>
        public double[] Coefficients { get; }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            // Clone the mixture coefficients
            var pi = (double[])Coefficients.Clone();

            // Clone the mixture components
            var pdf = new T[Components.Length];
            for (var i = 0; i < Components.Length; i++)
            {
                pdf[i] = (T)Components[i].Clone();
            }

            return new Mixture<T>(pi, pdf);
        }

        private void initialize()
        {
            mean = null;
            variance = null;
            range = null;
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
            var r = 0.0;
            for (var i = 0; i < Components.Length; i++)
            {
                r += Coefficients[i] * Components[i].ProbabilityFunction(x);
            }

            return r;
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for one of
        ///     the component distributions evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="componentIndex">The index of the desired component distribution.</param>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The probability of <c>x</c> occurring in the component distribution,
        ///     computed as the PDF of the component distribution times its mixture
        ///     coefficient.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        public double ProbabilityDensityFunction(int componentIndex, double x)
        {
            return Coefficients[componentIndex] * Components[componentIndex].ProbabilityFunction(x);
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
            var r = double.NegativeInfinity;
            for (var i = 0; i < Components.Length; i++)
            {
                r = Special.LogSum(r, System.Math.Log(Coefficients[i]) + Components[i].LogProbabilityFunction(x));
            }

            return r;
        }

        /// <summary>
        ///     Gets the log-probability density function (pdf) for one
        ///     of the component distributions evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="componentIndex">The index of the desired component distribution.</param>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c> occurring in the
        ///     component distribution, computed as the PDF of the component
        ///     distribution times its mixture coefficient.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        public double LogProbabilityDensityFunction(int componentIndex, double x)
        {
            return System.Math.Log(Coefficients[componentIndex]) + Components[componentIndex].LogProbabilityFunction(x);
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
            var r = 0.0;
            for (var i = 0; i < Components.Length; i++)
            {
                r += Coefficients[i] * Components[i].DistributionFunction(x);
            }

            return r;
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for one
        ///     component of this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="componentIndex">The component distribution's index.</param>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        public double DistributionFunction(int componentIndex, double x)
        {
            return Coefficients[componentIndex] * Components[componentIndex].DistributionFunction(x);
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
            var mixOptions = options as MixtureOptions;

            if (options != null && mixOptions == null)
            {
                mixOptions = new MixtureOptions
                {
                    InnerOptions = options
                };
            }

            Fit(observations, weights, mixOptions);
        }

        /// <summary>
        ///     Computes the log-likelihood of the distribution
        ///     for a given set of observations.
        /// </summary>
        public double LogLikelihood(double[] observations, double[] weights)
        {
            return ExpectationMaximization<double>.LogLikelihood(Coefficients, cache, observations,
                weights, weights.Sum());
        }

        /// <summary>
        ///     Computes the log-likelihood of the distribution
        ///     for a given set of observations.
        /// </summary>
        public double LogLikelihood(double[] observations)
        {
            return ExpectationMaximization<double>.LogLikelihood(Coefficients, cache, observations);
        }

        /// <summary>
        ///     Estimates a new mixture model from a given set of observations.
        /// </summary>
        /// <param name="data">A set of observations.</param>
        /// <param name="components">The initial components of the mixture model.</param>
        /// <returns>Returns a new Mixture fitted to the given observations.</returns>
        public static Mixture<T> Estimate(double[] data, params T[] components)
        {
            var mixture = new Mixture<T>(components);
            mixture.Fit(data);
            return mixture;
        }

        /// <summary>
        ///     Estimates a new mixture model from a given set of observations.
        /// </summary>
        /// <param name="data">A set of observations.</param>
        /// <param name="coefficients">The initial mixture coefficients.</param>
        /// <param name="components">The initial components of the mixture model.</param>
        /// <returns>Returns a new Mixture fitted to the given observations.</returns>
        public static Mixture<T> Estimate(double[] data, double[] coefficients, params T[] components)
        {
            var mixture = new Mixture<T>(coefficients, components);
            mixture.Fit(data);
            return mixture;
        }

        /// <summary>
        ///     Estimates a new mixture model from a given set of observations.
        /// </summary>
        /// <param name="data">A set of observations.</param>
        /// <param name="coefficients">The initial mixture coefficients.</param>
        /// <param name="threshold">The convergence threshold for the Expectation-Maximization estimation.</param>
        /// <param name="components">The initial components of the mixture model.</param>
        /// <returns>Returns a new Mixture fitted to the given observations.</returns>
        public static Mixture<T> Estimate(double[] data, double threshold, double[] coefficients, params T[] components)
        {
            var mixture = new Mixture<T>(coefficients, components);
            mixture.Fit(data, new MixtureOptions(threshold));
            return mixture;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var sb = new StringBuilder();
            sb.Append("Mixture(x; ");

            for (var i = 0; i < Coefficients.Length; i++)
            {
                sb.AppendFormat("{0}*", Coefficients[i].ToString(format, formatProvider));

                var fmt = Components[i] as IFormattable;

                if (fmt != null)
                {
                    sb.AppendFormat(fmt.ToString(format, formatProvider));
                }
                else
                {
                    sb.AppendFormat(fmt.ToString());
                }

                if (i < Coefficients.Length - 1)
                {
                    sb.Append(" + ");
                }
            }

            sb.Append(")");

            return sb.ToString();
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
            if (sampleable == null)
            {
                sampleable = new ISampleableDistribution<double>[Components.Length];
                for (var i = 0; i < sampleable.Length; i++)
                {
                    sampleable[i] = Components[i] as ISampleableDistribution<double>;
                }
            }

            for (var i = 0; i < samples; i++)
            {
                // Choose one coefficient at random
                var j = GeneralDiscreteDistribution.Random(Coefficients, source);

                // Sample from the chosen coefficient
                result[i] = sampleable[j].Generate();
            }

            return result;
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public override double Generate(Random source)
        {
            if (sampleable == null)
            {
                sampleable = new ISampleableDistribution<double>[Components.Length];
                for (var i = 0; i < sampleable.Length; i++)
                {
                    sampleable[i] = Components[i] as ISampleableDistribution<double>;
                }
            }

            // Choose one coefficient at random
            var j = GeneralDiscreteDistribution.Random(Coefficients, source);

            // Sample from the chosen coefficient
            return sampleable[j].Generate();
        }

        #endregion
    }
}