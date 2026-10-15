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
using System.ComponentModel;
using Accord.Math;
using Accord.Math.Optimization;
using Accord.Math.Random;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     The 4-parameter Beta distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The generalized beta distribution is a family of continuous probability distributions defined
    ///         on any interval (min, max) parameterized by two positive shape parameters and two real location
    ///         parameters, typically denoted by α, β, a and b. The beta distribution can be suited to the
    ///         statistical modeling of proportions in applications where values of proportions equal to 0 or 1
    ///         do not occur. One theoretical case where the beta distribution arises is as the distribution of
    ///         the ratio formed by one random variable having a Gamma distribution divided by the sum of it and
    ///         another independent random variable also having a Gamma distribution with the same scale parameter
    ///         (but possibly different shape parameter).
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Beta_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Beta distribution.
    ///                         Available from: http://en.wikipedia.org/wiki/Beta_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="https://en.wikipedia.org/wiki/Three-point_estimation">
    ///                         Wikipedia, The Free Encyclopedia. Three-point estimation.
    ///                         Available from: https://en.wikipedia.org/wiki/Three-point_estimation
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://broadleaf.com.au/resource-material/beta-pert-origins/">
    ///                         Broadleaf Capital International Pty Ltd. Beta PERT origins.
    ///                         Available from: http://broadleaf.com.au/resource-material/beta-pert-origins/
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://mech.vub.ac.be/teaching/info/Ontwerpmethodologie/Appendix%20les%202%20PERT.pdf">
    ///                         Malcolm, D. G., Roseboom J. H., Clark C.E., and Fazar, W. Application of a technique of
    ///                         research
    ///                         and development program evaluation, Operations Research, 7, 646-669, 1959. Available from:
    ///                         http://mech.vub.ac.be/teaching/info/Ontwerpmethodologie/Appendix%20les%202%20PERT.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://connection.ebscohost.com/c/articles/18246172/pert-model-distribution-activity-time">
    ///                         Clark, C. E. The PERT model for the distribution of an activity time, Operations Research, 10,
    ///                         405-406,
    ///                         1962. Available from:
    ///                         http://connection.ebscohost.com/c/articles/18246172/pert-model-distribution-activity-time
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Note: Simpler examples are also available at the <see cref="BetaDistribution" /> page.
    ///     </para>
    ///     <para>
    ///         The following example shows how to create a simpler 2-parameter Beta
    ///         distribution and compute some of its properties and measures.
    ///     </para>
    ///     <code
    ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Univariate\Continuous\GeneralizedBetaDistributionTest.cs"
    ///         region="doc_create2" />
    ///     <para>
    ///         The following example shows how to create a 4-parameter (Generalized) Beta
    ///         distribution and compute some of its properties and measures.
    ///     </para>
    ///     <code
    ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Univariate\Continuous\GeneralizedBetaDistributionTest.cs"
    ///         region="doc_create" />
    ///     <para>
    ///         The following example shows how to create a 4-parameter Beta distribution
    ///         with a three-point estimate using PERT.
    ///     </para>
    ///     <code
    ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Univariate\Continuous\GeneralizedBetaDistributionTest.cs"
    ///         region="doc_pert" />
    ///     <para>
    ///         The following example shows how to create a 4-parameter Beta distribution
    ///         with a three-point estimate using Vose's modification for PERT.
    ///     </para>
    ///     <code
    ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Univariate\Continuous\GeneralizedBetaDistributionTest.cs"
    ///         region="doc_pert2" />
    ///     <para>
    ///         The next example shows how to generate 1000 new samples from a Beta distribution:
    ///     </para>
    ///     <code>
    ///   // Using the distribution's parameters
    ///   double[] samples = GeneralizedBetaDistribution.Random(alpha: 2, beta: 3, min: 0, max: 1, samples: 1000);
    ///     
    ///   // Using an existing distribution
    ///   var b = new GeneralizedBetaDistribution(alpha: 1, beta: 2);
    ///   double[] new_samples = b.Generate(1000);
    /// </code>
    ///     <para>
    ///         And finally, how to estimate the parameters of a Beta distribution from
    ///         a set of observations, using either the Method-of-moments or the Maximum
    ///         Likelihood Estimate.
    ///     </para>
    ///     <code>
    /// // First we will be drawing 100000 observations from a 4-parameter 
    /// //   Beta distribution with α = 2, β = 3, min = 10 and max = 15:
    /// 
    /// double[] samples = GeneralizedBetaDistribution.Random(alpha: 2, beta: 3, min: 10, max: 15, samples: 100000);
    /// 
    /// // We can estimate a distribution with the known max and min
    /// var B = GeneralizedBetaDistribution.Estimate(samples, 10, 15);
    /// 
    /// // We can explicitly ask for a Method-of-moments estimation
    /// var mm = GeneralizedBetaDistribution.Estimate(samples, 10, 15,
    ///     new GeneralizedBetaOptions { Method = BetaEstimationMethod.Moments });
    ///     
    /// // or explicitly ask for the Maximum Likelihood estimation
    /// var mle = GeneralizedBetaDistribution.Estimate(samples, 10, 15,
    ///     new GeneralizedBetaOptions { Method = BetaEstimationMethod.MaximumLikelihood });
    /// </code>
    /// </example>
    /// <seealso cref="BetaDistribution" />
    [Serializable]
    public class GeneralizedBetaDistribution : UnivariateContinuousDistribution,
        ISampleableDistribution<double>, IFittableDistribution<double, GeneralizedBetaOptions>
    {
        // Distribution parameters

        private double constant;
        private double? entropy;

        /// <summary>
        ///     Constructs a Beta distribution defined in the
        ///     interval (0,1) with the given parameters α and β.
        /// </summary>
        /// <param name="alpha">The shape parameter α (alpha).</param>
        /// <param name="beta">The shape parameter β (beta).</param>
        public GeneralizedBetaDistribution([Positive] [DefaultValue(1)] double alpha,
            [Positive] [DefaultValue(1)] double beta)
            : this(alpha, beta, 0, 1)
        {
        }

        /// <summary>
        ///     Constructs a Beta distribution defined in the
        ///     interval (a, b) with parameters α, β, a and b.
        /// </summary>
        /// <param name="alpha">The shape parameter α (alpha).</param>
        /// <param name="beta">The shape parameter β (beta).</param>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        public GeneralizedBetaDistribution([Positive] [DefaultValue(1)] double alpha,
            [Positive] [DefaultValue(1)] double beta,
            [Real] [DefaultValue(0)] double min, [Real] [DefaultValue(1)] double max)
        {
            if (min > max)
            {
                throw new ArgumentOutOfRangeException("max",
                    "The maximum value 'max' must be greater than the minimum value 'min'.");
            }

            if (alpha <= 0)
            {
                throw new ArgumentOutOfRangeException("alpha", "The shape parameter alpha must be positive.");
            }

            if (beta <= 0)
            {
                throw new ArgumentOutOfRangeException("beta", "The shape parameter beta must be positive.");
            }

            initialize(min, max, alpha, beta);
        }

        /// <summary>
        ///     Gets the minimum value A.
        /// </summary>
        public double Min { get; private set; }

        /// <summary>
        ///     Gets the maximum value B.
        /// </summary>
        public double Max { get; private set; }

        /// <summary>
        ///     Gets the shape parameter α (alpha)
        /// </summary>
        public double Alpha { get; private set; }

        /// <summary>
        ///     Gets the shape parameter β (beta).
        /// </summary>
        public double Beta { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution,
        ///     defined as (a + 4 * m + 6 * b).
        /// </summary>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Mean
        {
            get
            {
                var mu = Alpha / (Alpha + Beta);
                return mu * (Max - Min) + Min;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the variance for this distribution,
        ///     defined as ((b - a) / (k+2))²
        /// </summary>
        /// <value>
        ///     The distribution's variance.
        /// </value>
        public override double Variance
        {
            get
            {
                var var = Alpha * Beta / ((Alpha + Beta) * (Alpha + Beta) * (Alpha + Beta + 1));
                return var * (Max - Min);
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <remarks>
        ///     The beta distribution's mode is given
        ///     by <c>(a - 1) / (a + b - 2).</c>
        /// </remarks>
        /// <value>
        ///     The distribution's mode value.
        /// </value>
        public override double Mode
        {
            get
            {
                var mode = (Alpha - 1.0) / (Alpha + Beta - 2.0);
                return mode * (Max - Min) + Min;
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the distribution support, defined as (<see cref="Min" />, <see cref="Max" />).
        /// </summary>
        public override DoubleRange Support
        {
            get => new(Min, Max);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        /// <value>The distribution's entropy.</value>
        public override double Entropy
        {
            get
            {
                if (entropy == null)
                {
                    var lnBab = System.Math.Log(Math.Beta.Function(Alpha, Beta));
                    var da = Gamma.Digamma(Alpha);
                    var db = Gamma.Digamma(Beta);
                    var dab = Gamma.Digamma(Alpha + Beta);
                    entropy = lnBab - (Alpha - 1) * da - (Beta - 1) * db + (Alpha + Beta - 2) * dab;
                }

                return entropy.Value;
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against.
        ///     The array elements can be either of type double (for univariate data) or type
        ///     double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting,
        ///     such as regularization constants and additional parameters.
        /// </param>
        public void Fit(double[] observations, double[] weights, GeneralizedBetaOptions options)
        {
            var fixMax = true;
            var fixMin = true;
            var sorted = false;
            var useMLE = false;
            var imax = -1;
            var imin = -1;

            if (options != null)
            {
                fixMax = options.FixMax;
                fixMin = options.FixMin;
                sorted = options.IsSorted;
                imin = options.MinIndex;
                imax = options.MaxIndex;
                useMLE = options.Method == BetaEstimationMethod.MaximumLikelihood;
            }

            if (!sorted)
            {
                Array.Sort(observations, weights);
            }

            if (!fixMin)
            {
                Min = TriangularDistribution.GetMin(observations, weights, out imin);
            }

            if (!fixMax)
            {
                Max = TriangularDistribution.GetMax(observations, weights, out imax);
            }

            if (imin == -1)
            {
                imin = TriangularDistribution.FindMin(observations, Min);
            }

            if (imax == -1)
            {
                imax = TriangularDistribution.FindMax(observations, Max);
            }


            var mean = GetMean(observations, weights, imin, imax);
            var var = GetVariance(observations, weights, mean, imin, imax);

            fitMoments(Min, Max, mean, var);

            if (useMLE)
            {
                if (weights == null)
                {
                    double sum1 = 0, sum2 = 0;
                    for (var i = imin; i <= imax; i++)
                    {
                        sum1 += System.Math.Log(observations[i]);
                        sum2 += System.Math.Log(1 - observations[i]);
                    }

                    fitMLE(sum1, sum2, observations.Length);
                }
                else
                {
                    double sum1 = 0, sum2 = 0, sumw = 0;
                    for (var i = imin; i <= imax; i++)
                    {
                        sum1 += System.Math.Log(observations[i]) * weights[i];
                        sum2 += System.Math.Log(1 - observations[i]) * weights[i];
                        sumw += weights[i];
                    }

                    fitMLE(sum1, sum2, sumw);
                }
            }
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new GeneralizedBetaDistribution(Alpha, Beta, Min, Max);
        }

        /// <summary>
        ///     Constructs a BetaPERT distribution defined in the interval (a, b)
        ///     using Vose's PERT estimation for the parameters a, b, mode and λ.
        /// </summary>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <param name="mode">The most likely value m.</param>
        /// <returns>
        ///     A Beta distribution initialized using the Vose's PERT method.
        /// </returns>
        public static GeneralizedBetaDistribution Vose(double min, double max, double mode)
        {
            return Vose(min, max, mode, 4);
        }

        /// <summary>
        ///     Constructs a BetaPERT distribution defined in the interval (a, b)
        ///     using Vose's PERT estimation for the parameters a, b, mode and λ.
        /// </summary>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <param name="mode">The most likely value m.</param>
        /// <param name="scale">The scale parameter λ (lambda). Default is 4.</param>
        /// <returns>
        ///     A Beta distribution initialized using the Vose's PERT method.
        /// </returns>
        public static GeneralizedBetaDistribution Vose(double min, double max, double mode, double scale)
        {
            // http://pubsonline.informs.org/doi/pdf/10.1287/ited.1080.0013

            var mu = (min + scale * mode + max) / (scale + 2);
            var sd = (max - min) / (scale + 2);

            // Vise's equations for the Beta distribution:
            var alpha = (mu - min) / (max - min) * ((mu - min) * (max - mu) / (sd * sd) - 1);
            var beta = alpha * (max - mu) / (mu - min);

            return new GeneralizedBetaDistribution(alpha, beta, min, max);
        }

        /// <summary>
        ///     Constructs a BetaPERT distribution defined in the interval (a, b)
        ///     using usual PERT estimation for the parameters a, b, mode and λ.
        /// </summary>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <param name="mode">The most likely value m.</param>
        /// <returns>
        ///     A Beta distribution initialized using the PERT method.
        /// </returns>
        public static GeneralizedBetaDistribution Pert(double min, double max, double mode)
        {
            return Pert(min, max, mode, 4);
        }

        /// <summary>
        ///     Constructs a BetaPERT distribution defined in the interval (a, b)
        ///     using usual PERT estimation for the parameters a, b, mode and λ.
        /// </summary>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <param name="mode">The most likely value m.</param>
        /// <param name="scale">The scale parameter λ (lambda). Default is 4.</param>
        /// <returns>
        ///     A Beta distribution initialized using the PERT method.
        /// </returns>
        public static GeneralizedBetaDistribution Pert(double min, double max, double mode, double scale)
        {
            var mu = (min + scale * mode + max) / (scale + 2);

            var alpha = 1 + scale / 2;

            // Standard equations for the Beta distribution:
            if (mu != mode)
            {
                alpha = (mu - min) * (2 * mode - min - max) / ((mode - mu) * (max - min));
            }

            var beta = alpha * (max - mu) / (mu - min);

            return new GeneralizedBetaDistribution(alpha, beta, min, max);
        }

        /// <summary>
        ///     Constructs a BetaPERT distribution defined in the interval (a, b)
        ///     using Golenko-Ginzburg observation that the mode is often at 2/3
        ///     of the guessed interval.
        /// </summary>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <returns>
        ///     A Beta distribution initialized using the Golenko-Ginzburg's method.
        /// </returns>
        public static GeneralizedBetaDistribution GolenkoGinzburg(double min, double max)
        {
            return new GeneralizedBetaDistribution(2, 3, min, max);
        }

        /// <summary>
        ///     Constructs a standard Beta distribution defined in the interval (0, 1)
        ///     based on the number of successed and trials for an experiment.
        /// </summary>
        /// <param name="successes">The number of success <c>r</c>. Default is 0.</param>
        /// <param name="trials">The number of trials <c>n</c>. Default is 1.</param>
        /// <returns>
        ///     A standard Beta distribution initialized using the given parameters.
        /// </returns>
        public static GeneralizedBetaDistribution Standard(int successes, int trials)
        {
            return new GeneralizedBetaDistribution(successes + 1, trials - successes + 1);
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        protected internal override double InnerDistributionFunction(double x)
        {
            var z = (x - Min) / (Max - Min);

            return Math.Beta.Incomplete(Alpha, Beta, z);
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
            var z = Math.Beta.IncompleteInverse(Alpha, Beta, p);
            var x = z * (Max - Min) + Min;
            return x;
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The probability of <c>x</c> occurring in the current distribution.
        /// </returns>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var length = Max - Min;
            var z = (x - Min) / length;

            var a = System.Math.Pow(z, Alpha - 1);
            var b = System.Math.Pow(1 - z, Beta - 1);
            return constant * a * b / length;
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
        ///     <para>
        ///         The Probability Density Function (PDF) describes the
        ///         probability that a given value <c>x</c> will occur.
        ///     </para>
        /// </remarks>
        /// <seealso cref="UnivariateContinuousDistribution.ProbabilityDensityFunction(double)" />
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            var length = Max - Min;
            var z = (x - Min) / length;

            var a = (Alpha - 1) * System.Math.Log(z);
            var b = (Beta - 1) * System.Math.Log(1 - z);
            return System.Math.Log(constant) + a + b - System.Math.Log(length);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "B(x; α = {0}, β = {1}, min = {2}, max = {3})",
                Alpha.ToString(format, formatProvider),
                Beta.ToString(format, formatProvider),
                Min.ToString(format, formatProvider),
                Max.ToString(format, formatProvider));
        }

        private void initialize(double min, double max, double alpha, double beta)
        {
            this.Alpha = alpha;
            this.Beta = beta;
            this.Min = min;
            this.Max = max;

            constant = 1.0 / Math.Beta.Function(alpha, beta);
            entropy = null;
        }

        private void fitMoments(double min, double max, double mean, double var)
        {
            var length = max - min;
            mean = (mean - min) / length;
            var = var / (length * length);

            if (var >= mean * (1.0 - mean))
            {
                throw new NotSupportedException();
            }

            var u = mean * (1 - mean) / var - 1.0;
            var alpha = mean * u;
            var beta = (1 - mean) * u;

            initialize(min, max, alpha, beta);
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
        ///     Optional arguments which may be used during fitting,
        ///     such as regularization constants and additional parameters.
        /// </param>
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            Fit(observations, weights, options as GeneralizedBetaOptions);
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
        ///     Optional arguments which may be used during fitting,
        ///     such as regularization constants and additional parameters.
        /// </param>
        public override void Fit(double[] observations, int[] weights, IFittingOptions options)
        {
            Fit(observations, weights, options as GeneralizedBetaOptions);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against.
        ///     The array elements can be either of type double (for univariate data) or type
        ///     double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting,
        ///     such as regularization constants and additional parameters.
        /// </param>
        public void Fit(double[] observations, int[] weights, GeneralizedBetaOptions options)
        {
            var fixMax = true;
            var fixMin = true;
            var sorted = false;
            var useMLE = false;
            var imax = -1;
            var imin = -1;

            if (options != null)
            {
                fixMax = options.FixMax;
                fixMin = options.FixMin;
                sorted = options.IsSorted;
                imin = options.MinIndex;
                imax = options.MaxIndex;
                useMLE = options.Method == BetaEstimationMethod.MaximumLikelihood;
            }

            if (!sorted)
            {
                Array.Sort(observations, weights);
            }

            if (!fixMin)
            {
                Min = TriangularDistribution.GetMin(observations, weights, out imin);
            }

            if (!fixMax)
            {
                Max = TriangularDistribution.GetMax(observations, weights, out imax);
            }

            if (imin == -1)
            {
                imin = TriangularDistribution.FindMin(observations, Min);
            }

            if (imax == -1)
            {
                imax = TriangularDistribution.FindMax(observations, Max);
            }


            var mean = GetMean(observations, weights, imin, imax);
            var var = GetVariance(observations, weights, mean, imin, imax);

            fitMoments(Min, Max, mean, var);

            if (useMLE)
            {
                if (weights == null)
                {
                    double sum1 = 0, sum2 = 0;
                    for (var i = imin; i <= imax; i++)
                    {
                        sum1 += System.Math.Log(observations[i]);
                        sum2 += System.Math.Log(1 - observations[i]);
                    }

                    fitMLE(sum1, sum2, observations.Length);
                }
                else
                {
                    double sum1 = 0, sum2 = 0, sumw = 0;
                    for (var i = imin; i <= imax; i++)
                    {
                        sum1 += System.Math.Log(observations[i]) * weights[i];
                        sum2 += System.Math.Log(1 - observations[i]) * weights[i];
                        sumw += weights[i];
                    }

                    fitMLE(sum1, sum2, sumw);
                }
            }
        }

        private static double GetMean(double[] observations, double[] weights, int imin, int imax)
        {
            double mean;

            if (weights == null)
            {
                double sum = 0;
                for (var i = imin; i <= imax; i++)
                {
                    sum += observations[i];
                }

                mean = sum / (imax - imin + 1);
            }
            else
            {
                double sum = 0;
                double weightSum = 0;
                for (var i = imin; i <= imax; i++)
                {
                    sum += weights[i] * observations[i];
                    weightSum += weights[i];
                }

                mean = sum / weightSum;
            }

            return mean;
        }

        private void fitMLE(double sum1, double sum2, double n)
        {
            var gradient = new double[2];

            var bfgs = new BoundedBroydenFletcherGoldfarbShanno(2);
            bfgs.LowerBounds[0] = 1e-100;
            bfgs.LowerBounds[1] = 1e-100;
            bfgs.Solution[0] = Alpha;
            bfgs.Solution[1] = Beta;

            bfgs.Function = parameters =>
                BetaDistribution.LogLikelihood(sum1, sum2, n, parameters[0], parameters[1]);

            bfgs.Gradient = parameters =>
                BetaDistribution.Gradient(sum1, sum2, n, parameters[0], parameters[1], gradient);

            if (!bfgs.Minimize())
            {
                throw new ConvergenceException();
            }

            Alpha = bfgs.Solution[0];
            Beta = bfgs.Solution[1];
        }

        private static double GetVariance(double[] observations, double[] weights, double mean, int imin, int imax)
        {
            double variance;

            if (weights == null)
            {
                double sum = 0;
                for (var i = imin; i <= imax; i++)
                {
                    sum += (observations[i] - mean) * (observations[i] - mean);
                }

                variance = sum / (imax - imin);
            }
            else
            {
                var sum = 0.0;
                var squareSum = 0.0;
                var weightSum = 0.0;

                for (var i = 0; i < observations.Length; i++)
                {
                    var z = observations[i] - mean;
                    var w = weights[i];

                    sum += w * (z * z);

                    weightSum += w;
                    squareSum += w * w;
                }

                // variance = sum / weightSum;
                variance = sum / (weightSum - squareSum / weightSum);
            }

            return variance;
        }

        private static double GetMean(double[] observations, int[] weights, int imin, int imax)
        {
            double mean;

            if (weights == null)
            {
                double sum = 0;
                for (var i = imin; i <= imax; i++)
                {
                    sum += observations[i];
                }

                mean = sum / (imax - imin);
            }
            else
            {
                double sum = 0;
                double weightSum = 0;
                for (var i = imin; i <= imax; i++)
                {
                    sum += weights[i] * observations[i];
                    weightSum += weights[i];
                }

                mean = sum / weightSum;
            }

            return mean;
        }

        private static double GetVariance(double[] observations, int[] weights, double mean, int imin, int imax)
        {
            double variance;

            if (weights == null)
            {
                double sum = 0;
                for (var i = imin; i <= imax; i++)
                {
                    sum += (observations[i] - mean) * (observations[i] - mean);
                }

                variance = sum / (imax - imin - 1);
            }
            else
            {
                var sum = 0.0;
                var weightSum = 0.0;

                for (var i = 0; i < observations.Length; i++)
                {
                    var z = observations[i] - mean;
                    double w = weights[i];

                    sum += w * (z * z);

                    weightSum += w;
                }

                variance = sum / (weightSum - 1);
            }

            return variance;
        }

        /// <summary>
        ///     Estimates a new Beta distribution from a set of observations.
        /// </summary>
        public static GeneralizedBetaDistribution Estimate(double[] samples, int min, int max)
        {
            var beta = new GeneralizedBetaDistribution(1, 1, min, max);
            beta.Fit(samples);
            return beta;
        }

        /// <summary>
        ///     Estimates a new Beta distribution from a set of weighted observations.
        /// </summary>
        public static GeneralizedBetaDistribution Estimate(double[] samples, int min, int max, double[] weights)
        {
            var beta = new GeneralizedBetaDistribution(1, 1, min, max);
            beta.Fit(samples, weights, null);
            return beta;
        }

        /// <summary>
        ///     Estimates a new Beta distribution from a set of weighted observations.
        /// </summary>
        public static GeneralizedBetaDistribution Estimate(double[] samples, int min, int max, double[] weights,
            GeneralizedBetaOptions options)
        {
            var beta = new GeneralizedBetaDistribution(1, 1, min, max);
            beta.Fit(samples, weights, options);
            return beta;
        }

        /// <summary>
        ///     Estimates a new Beta distribution from a set of observations.
        /// </summary>
        public static GeneralizedBetaDistribution Estimate(double[] samples, int min, int max,
            GeneralizedBetaOptions options)
        {
            var beta = new GeneralizedBetaDistribution(1, 1, min, max);
            beta.Fit(samples, (double[])null, options);
            return beta;
        }

        #region ISamplableDistribution<double> Members

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
            return Random(Alpha, Beta, Min, Max, samples, result, source);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public override double Generate(Random source)
        {
            return Random(Alpha, Beta, Min, Max, source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Beta distribution with the given parameters.
        /// </summary>
        /// <param name="alpha">The shape parameter α (alpha).</param>
        /// <param name="beta">The shape parameter β (beta).</param>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <returns>An array of double values sampled from the specified Beta distribution.</returns>
        public static double[] Random(double alpha, double beta, double min, double max, int samples)
        {
            return Random(alpha, beta, min, max, samples, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Beta distribution with the given parameters.
        /// </summary>
        /// <param name="alpha">The shape parameter α (alpha).</param>
        /// <param name="beta">The shape parameter β (beta).</param>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>An array of double values sampled from the specified Beta distribution.</returns>
        public static double[] Random(double alpha, double beta, double min, double max, int samples, double[] result)
        {
            return Random(alpha, beta, min, max, samples, result, Generator.Random);
        }

        /// <summary>
        ///     Generates a random observation from a
        ///     Beta distribution with the given parameters.
        /// </summary>
        /// <param name="alpha">The shape parameter α (alpha).</param>
        /// <param name="beta">The shape parameter β (beta).</param>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <returns>A random double value sampled from the specified Beta distribution.</returns>
        public static double Random(double alpha, double beta, double min, double max)
        {
            return Random(alpha, beta, min, max, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Beta distribution with the given parameters.
        /// </summary>
        /// <param name="alpha">The shape parameter α (alpha).</param>
        /// <param name="beta">The shape parameter β (beta).</param>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>An array of double values sampled from the specified Beta distribution.</returns>
        public static double[] Random(double alpha, double beta, double min, double max, int samples, Random source)
        {
            return Random(alpha, beta, min, max, samples, new double[samples], source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the
        ///     Beta distribution with the given parameters.
        /// </summary>
        /// <param name="alpha">The shape parameter α (alpha).</param>
        /// <param name="beta">The shape parameter β (beta).</param>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>An array of double values sampled from the specified Beta distribution.</returns>
        public static double[] Random(double alpha, double beta, double min, double max, int samples, double[] result,
            Random source)
        {
            BetaDistribution.Random(alpha, beta, samples, result, source);

            if (min != 0 || max != 1)
            {
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = result[i] * (max - min) + min;
                }
            }

            return result;
        }

        /// <summary>
        ///     Generates a random observation from a
        ///     Beta distribution with the given parameters.
        /// </summary>
        /// <param name="alpha">The shape parameter α (alpha).</param>
        /// <param name="beta">The shape parameter β (beta).</param>
        /// <param name="min">The minimum possible value a.</param>
        /// <param name="max">The maximum possible value b.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random double value sampled from the specified Beta distribution.</returns>
        public static double Random(double alpha, double beta, double min, double max, Random source)
        {
            var r = BetaDistribution.Random(alpha, beta, source);

            return r * (max - min) + min;
        }

        #endregion
    }
}