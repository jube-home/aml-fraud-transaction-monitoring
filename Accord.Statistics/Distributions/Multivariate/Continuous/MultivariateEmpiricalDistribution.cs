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
using Accord.Math.Decompositions;
using Accord.Statistics.Distributions.DensityKernels;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Multivariate
{
    /// <summary>
    ///     Multivariate empirical distribution.
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
    ///             <item>
    ///                 <description>
    ///                     Buch-Kromann, T.; Nonparametric Density Estimation (Multidimension), 2007. Available in
    ///                     http://www.buch-kromann.dk/tine/nonpar/Nonparametric_Density_Estimation_multidim.pdf
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     W. Härdle, M. Müller, S. Sperlich, A. Werwatz; Nonparametric and Semiparametric Models, 2004.
    ///                     Available
    ///                     in http://sfb649.wiwi.hu-berlin.de/fedc_homepage/xplore/ebooks/html/spm/spmhtmlnode18.html
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The first example shows how to fit a <see cref="MultivariateEmpiricalDistribution" />
    ///         using <see cref="GaussianKernel">Gaussian kernels</see>:
    ///     </para>
    ///     <code
    ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Multivariate\Continuous\MultivariateEmpiricalDistributionTest.cs"
    ///         region="doc_fit_gaussian" />
    ///     <para>
    ///         The second example shows how to the same as above, but using
    ///         <see cref="EpanechnikovKernel">Epanechnikov kernels</see> instead.
    ///     </para>
    ///     <code
    ///         source="Unit Tests\Accord.Tests.Statistics\Distributions\Multivariate\Continuous\MultivariateEmpiricalDistributionTest.cs"
    ///         region="doc_fit_epanechnikov" />
    /// </example>
    /// <seealso cref="IDensityKernel" />
    /// <seealso cref="Univariate.EmpiricalDistribution" />
    /// <seealso cref="GaussianKernel" />
    /// <seealso cref="EpanechnikovKernel" />
    [Serializable]
    public class MultivariateEmpiricalDistribution : MultivariateContinuousDistribution,
        IFittableDistribution<double[], MultivariateEmpiricalOptions>,
        ISampleableDistribution<double[]>
    {
        private CholeskyDecomposition chol;
        private double[,] covariance;

        private double determinant;

        private double[] mean;

        private int[] repeats;

        // Distribution parameters
        private double sumOfWeights;

        private WeightType type;
        private double[] variance;
        private double[] weights;

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="samples">The data samples.</param>
        public MultivariateEmpiricalDistribution(double[][] samples)
            : base(samples[0].Length)
        {
            initialize(null, samples, null, null, null);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="samples">The data samples.</param>
        /// <param name="weights">
        ///     The fractional weights to use for the samples.
        ///     The weights must sum up to one.
        /// </param>
        public MultivariateEmpiricalDistribution(double[][] samples, double[] weights)
            : base(samples[0].Length)
        {
            initialize(null, samples, weights, null, null);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="samples">The data samples.</param>
        /// <param name="weights">The number of repetition counts for each sample.</param>
        public MultivariateEmpiricalDistribution(double[][] samples, int[] weights)
            : base(samples[0].Length)
        {
            initialize(null, samples, null, weights, null);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="kernel">
        ///     The kernel density function to use.
        ///     Default is to use the <see cref="GaussianKernel" />.
        /// </param>
        /// <param name="samples">The data samples forming the distribution.</param>
        public MultivariateEmpiricalDistribution(IDensityKernel kernel, double[][] samples)
            : base(samples[0].Length)
        {
            initialize(kernel, samples, null, null, null);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="kernel">
        ///     The kernel density function to use.
        ///     Default is to use the <see cref="GaussianKernel" />.
        /// </param>
        /// <param name="samples">The data samples forming the distribution.</param>
        /// <param name="weights">
        ///     The fractional weights to use for the samples.
        ///     The weights must sum up to one.
        /// </param>
        public MultivariateEmpiricalDistribution(IDensityKernel kernel, double[][] samples, double[] weights)
            : base(samples[0].Length)
        {
            initialize(kernel, samples, weights, null, null);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="kernel">
        ///     The kernel density function to use.
        ///     Default is to use the <see cref="GaussianKernel" />.
        /// </param>
        /// <param name="samples">The data samples forming the distribution.</param>
        /// <param name="weights">The number of repetition counts for each sample.</param>
        public MultivariateEmpiricalDistribution(IDensityKernel kernel, double[][] samples, int[] weights)
            : base(samples[0].Length)
        {
            initialize(kernel, samples, null, weights, null);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="kernel">
        ///     The kernel density function to use.
        ///     Default is to use the <see cref="GaussianKernel" />.
        /// </param>
        /// <param name="samples">The data samples.</param>
        /// <param name="smoothing">
        ///     The kernel smoothing or bandwidth to be used in density estimation.
        ///     By default, the normal distribution approximation will be used.
        /// </param>
        public MultivariateEmpiricalDistribution(IDensityKernel kernel, double[][] samples, double[,] smoothing)
            : base(samples[0].Length)
        {
            initialize(kernel, samples, null, null, smoothing);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="kernel">
        ///     The kernel density function to use.
        ///     Default is to use the <see cref="GaussianKernel" />.
        /// </param>
        /// <param name="samples">The data samples.</param>
        /// <param name="weights">The number of repetition counts for each sample.</param>
        /// <param name="smoothing">
        ///     The kernel smoothing or bandwidth to be used in density estimation.
        ///     By default, the normal distribution approximation will be used.
        /// </param>
        public MultivariateEmpiricalDistribution(IDensityKernel kernel, double[][] samples, int[] weights,
            double[,] smoothing)
            : base(samples[0].Length)
        {
            initialize(kernel, samples, null, weights, smoothing);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="kernel">
        ///     The kernel density function to use.
        ///     Default is to use the <see cref="GaussianKernel" />.
        /// </param>
        /// <param name="samples">The data samples.</param>
        /// <param name="weights">
        ///     The fractional weights to use for the samples.
        ///     The weights must sum up to one.
        /// </param>
        /// <param name="smoothing">
        ///     The kernel smoothing or bandwidth to be used in density estimation.
        ///     By default, the normal distribution approximation will be used.
        /// </param>
        public MultivariateEmpiricalDistribution(IDensityKernel kernel, double[][] samples, double[] weights,
            double[,] smoothing)
            : base(samples[0].Length)
        {
            initialize(kernel, samples, weights, null, smoothing);
        }

        /// <summary>
        ///     Creates a new Empirical Distribution from the data samples.
        /// </summary>
        /// <param name="samples">The data samples.</param>
        /// <param name="weights">The number of repetition counts for each sample.</param>
        /// <param name="smoothing">
        ///     The kernel smoothing or bandwidth to be used in density estimation.
        ///     By default, the normal distribution approximation will be used.
        /// </param>
        public MultivariateEmpiricalDistribution(double[][] samples, int[] weights, double[,] smoothing)
            : base(samples[0].Length)
        {
            initialize(Kernel, samples, null, weights, smoothing);
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
        public MultivariateEmpiricalDistribution(double[][] samples, double[] weights, double[,] smoothing)
            : base(samples[0].Length)
        {
            initialize(Kernel, samples, weights, null, smoothing);
        }

        private MultivariateEmpiricalDistribution(int dimension)
            : base(dimension)
        {
        }

        /// <summary>
        ///     Gets the kernel density function used in this distribution.
        /// </summary>
        public IDensityKernel Kernel { get; private set; }

        /// <summary>
        ///     Gets the samples giving this empirical distribution.
        /// </summary>
        public double[][] Samples { get; private set; }

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
        public double[,] Smoothing { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>
        ///     A vector containing the mean values for the distribution.
        /// </value>
        public override double[] Mean
        {
            get
            {
                if (mean == null)
                {
                    if (type == WeightType.None)
                    {
                        mean = Samples.Mean(0);
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

                return mean;
            }
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>
        ///     A vector containing the variance values for the distribution.
        /// </value>
        public override double[] Variance
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

                return variance;
            }
        }

        /// <summary>
        ///     Gets the variance-covariance matrix for this distribution.
        /// </summary>
        /// <value>
        ///     A matrix containing the covariance values for the distribution.
        /// </value>
        public override double[,] Covariance
        {
            get
            {
                if (covariance == null)
                {
                    if (type == WeightType.None)
                    {
                        covariance = Samples.Covariance(Mean).ToMatrix(); // TODO: Switch to double[][]
                    }

                    else if (type == WeightType.Repetition)
                    {
                        covariance = Samples.WeightedCovariance(repeats);
                    }

                    else if (type == WeightType.Fraction)
                    {
                        covariance = Samples.WeightedCovariance(weights);
                    }
                }

                return covariance;
            }
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">The array of observations to fit the model against.</param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        public void Fit(double[][] observations, double[] weights, MultivariateEmpiricalOptions options)
        {
            double[,] smoothing = null;
            var inPlace = false;

            if (options != null)
            {
                smoothing = options.SmoothingRule(observations);
                inPlace = options.InPlace;
            }

            observations = inPlace ? observations : Matrix.MemberwiseClone(observations);

            if (weights != null)
            {
                weights = inPlace ? weights : (double[])weights.Clone();
            }

            initialize(Kernel, observations, weights, null, smoothing);
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            var e = new MultivariateEmpiricalDistribution(Dimension);

            e.chol = (CholeskyDecomposition)chol.Clone();
            e.determinant = determinant;
            e.Kernel = Kernel;
            e.Length = Length;
            e.repeats = (int[])repeats.Clone();
            e.Samples = Matrix.MemberwiseClone(Samples);
            e.Smoothing = Matrix.MemberwiseClone(Smoothing);
            e.sumOfWeights = sumOfWeights;
            e.type = type;

            if (e.weights != null)
            {
                e.weights = (double[])weights.Clone();
            }

            if (e.repeats != null)
            {
                e.repeats = (int[])repeats.Clone();
            }

            return e;
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
        public override double[][] Generate(int samples, double[][] result, Random source)
        {
            for (var i = 0; i < samples; i++)
            {
                var j = source.Next(this.Samples.Length);
                Array.Copy(this.Samples[j], result[i], Dimension);
            }

            return result;
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
        protected internal override double InnerProbabilityDensityFunction(double[] x)
        {
            // http://www.buch-kromann.dk/tine/nonpar/Nonparametric_Density_Estimation_multidim.pdf
            // http://sfb649.wiwi.hu-berlin.de/fedc_homepage/xplore/ebooks/html/spm/spmhtmlnode18.html

            double sum = 0;
            var delta = new double[Dimension];

            if (type == WeightType.None)
            {
                for (var i = 0; i < Samples.Length; i++)
                {
                    for (var j = 0; j < x.Length; j++)
                    {
                        delta[j] = x[j] - Samples[i][j];
                    }

                    sum += Kernel.Function(chol.Solve(delta));
                }

                return sum / (Samples.Length * determinant);
            }

            if (type == WeightType.Repetition)
            {
                for (var i = 0; i < Samples.Length; i++)
                {
                    for (var j = 0; j < x.Length; j++)
                    {
                        delta[j] = x[j] - Samples[i][j];
                    }

                    sum += Kernel.Function(chol.Solve(delta)) * repeats[i];
                }

                return sum / (Length * determinant);
            }

            if (type == WeightType.Fraction)
            {
                for (var i = 0; i < Samples.Length; i++)
                {
                    for (var j = 0; j < x.Length; j++)
                    {
                        delta[j] = x[j] - Samples[i][j];
                    }

                    sum += Kernel.Function(chol.Solve(delta)) * weights[i];
                }

                return sum / (sumOfWeights * determinant);
            }

            throw new InvalidOperationException();
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
        protected internal override double InnerDistributionFunction(double[] x)
        {
            if (type == WeightType.None)
            {
                var sum = 0; // Normal sample, no weights
                for (var i = 0; i < Samples.Length; i++)
                {
                    var j = 0;
                    for (; j < x.Length; j++)
                    {
                        if (Samples[i][j] > x[j])
                        {
                            break;
                        }
                    }

                    if (j == x.Length)
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
                    var j = 0;
                    for (; j < x.Length; j++)
                    {
                        if (Samples[i][j] > x[j])
                        {
                            break;
                        }
                    }

                    if (j == x.Length)
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
                    var j = 0;
                    for (; j < x.Length; j++)
                    {
                        if (Samples[i][j] > x[j])
                        {
                            break;
                        }
                    }

                    if (j == x.Length)
                    {
                        sum += weights[i];
                    }
                }

                return sum / sumOfWeights;
            }

            throw new InvalidOperationException();
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
        public override void Fit(double[][] observations, double[] weights, IFittingOptions options)
        {
            Fit(observations, weights, options as MultivariateEmpiricalOptions);
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
        public override void Fit(double[][] observations, int[] weights, IFittingOptions options)
        {
            Fit(observations, weights, options as MultivariateEmpiricalOptions);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">The array of observations to fit the model against.</param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        public void Fit(double[][] observations, int[] weights, MultivariateEmpiricalOptions options)
        {
            double[,] smoothing = null;
            var inPlace = false;

            if (options != null)
            {
                smoothing = options.SmoothingRule(observations);
                inPlace = options.InPlace;
            }

            observations = inPlace ? observations : Matrix.MemberwiseClone(observations);

            if (weights != null)
            {
                weights = inPlace ? weights : (int[])weights.Clone();
            }

            initialize(Kernel, observations, null, weights, smoothing);
        }

        private void initialize(IDensityKernel kernel, double[][] observations,
            double[] weights, int[] repeats, double[,] smoothing)
        {
            if (smoothing == null)
            {
                smoothing = SilvermanRule(observations, weights, repeats);
            }

            if (kernel == null)
            {
                kernel = new GaussianKernel(Dimension);
            }

            this.Kernel = kernel;
            Samples = observations;
            this.Smoothing = smoothing;
            determinant = smoothing.Determinant();

            this.weights = weights;
            this.repeats = repeats;

            chol = new CholeskyDecomposition(smoothing);


            if (weights != null)
            {
                type = WeightType.Fraction;
                Length = Samples.Length;
                sumOfWeights = weights.Sum();
            }
            else if (repeats != null)
            {
                type = WeightType.Repetition;
                Length = repeats.Sum();
                sumOfWeights = 1.0;
            }
            else
            {
                type = WeightType.None;
                Length = Samples.Length;
                sumOfWeights = 1.0;
            }

            mean = null;
            variance = null;
            covariance = null;
        }

        /// <summary>
        ///     Gets the Silverman's rule. estimative of the smoothing parameter.
        ///     This is the default smoothing rule applied used when estimating
        ///     <see cref="MultivariateEmpiricalDistribution" />s.
        /// </summary>
        /// <remarks>
        ///     This method is described on Wikipedia, at
        ///     http://en.wikipedia.org/wiki/Multivariate_kernel_density_estimation
        /// </remarks>
        /// <param name="observations">The observations for the empirical distribution.</param>
        /// <returns>An estimative of the smoothing parameter.</returns>
        public static double[,] SilvermanRule(double[][] observations)
        {
            // Silverman's rule
            //  - http://en.wikipedia.org/wiki/Multivariate_kernel_density_estimation

            var sigma = observations.StandardDeviation();

            double d = sigma.Length;
            double n = observations.Length;

            return silverman(sigma, d, n);
        }

        /// <summary>
        ///     Gets the Silverman's rule. estimative of the smoothing parameter.
        ///     This is the default smoothing rule applied used when estimating
        ///     <see cref="MultivariateEmpiricalDistribution" />s.
        /// </summary>
        /// <remarks>
        ///     This method is described on Wikipedia, at
        ///     http://en.wikipedia.org/wiki/Multivariate_kernel_density_estimation
        /// </remarks>
        /// <param name="observations">The observations for the empirical distribution.</param>
        /// <param name="weights">The fractional importance for each sample. Those values must sum up to one.</param>
        /// <returns>An estimative of the smoothing parameter.</returns>
        public static double[,] SilvermanRule(double[][] observations, double[] weights)
        {
            // Silverman's rule
            //  - http://en.wikipedia.org/wiki/Multivariate_kernel_density_estimation

            var sigma = observations.WeightedStandardDeviation(weights);

            double d = sigma.Length;
            var n = weights.Sum();

            return silverman(sigma, d, n);
        }

        /// <summary>
        ///     Gets the Silverman's rule. estimative of the smoothing parameter.
        ///     This is the default smoothing rule applied used when estimating
        ///     <see cref="MultivariateEmpiricalDistribution" />s.
        /// </summary>
        /// <remarks>
        ///     This method is described on Wikipedia, at
        ///     http://en.wikipedia.org/wiki/Multivariate_kernel_density_estimation
        /// </remarks>
        /// <param name="observations">The observations for the empirical distribution.</param>
        /// <param name="weights">The number of times each sample should be repeated.</param>
        /// <returns>An estimative of the smoothing parameter.</returns>
        public static double[,] SilvermanRule(double[][] observations, int[] weights)
        {
            // Silverman's rule
            //  - http://en.wikipedia.org/wiki/Multivariate_kernel_density_estimation

            var sigma = observations.WeightedStandardDeviation(weights);

            double d = sigma.Length;
            double n = weights.Sum();

            return silverman(sigma, d, n);
        }

        /// <summary>
        ///     Gets the Silverman's rule. estimative of the smoothing parameter.
        ///     This is the default smoothing rule applied used when estimating
        ///     <see cref="MultivariateEmpiricalDistribution" />s.
        /// </summary>
        /// <remarks>
        ///     This method is described on Wikipedia, at
        ///     http://en.wikipedia.org/wiki/Multivariate_kernel_density_estimation
        /// </remarks>
        /// <param name="observations">The observations for the empirical distribution.</param>
        /// <param name="weights">The fractional importance for each sample. Those values must sum up to one.</param>
        /// <param name="repeats">The number of times each sample should be repeated.</param>
        /// <returns>An estimative of the smoothing parameter.</returns>
        public static double[,] SilvermanRule(double[][] observations, double[] weights, int[] repeats)
        {
            if (weights != null)
            {
                return SilvermanRule(observations, weights);
            }

            if (repeats != null)
            {
                return SilvermanRule(observations, repeats);
            }

            return SilvermanRule(observations);
        }

        private static double[,] silverman(double[] sigma, double d, double n)
        {
            var smoothing = new double[sigma.Length, sigma.Length];
            for (var i = 0; i < sigma.Length; i++)
            {
                var a = System.Math.Pow(4.0 / (d + 2.0), 1.0 / (d + 4.0));
                var b = System.Math.Pow(n, -1.0 / (d + 4.0));
                smoothing[i, i] = a * b * sigma[i];
            }

            return smoothing;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Empirical(X)");
        }
    }
}