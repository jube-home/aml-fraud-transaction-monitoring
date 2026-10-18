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
using System.Threading.Tasks;
using Accord.Diagnostics;
using Accord.Math;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Gaussian mixture model clustering.
    /// </summary>
    /// <remarks>
    ///     Gaussian Mixture Models are one of most widely used model-based
    ///     clustering methods. This specialized class provides a wrap-up
    ///     around the
    ///     <see cref="MultivariateMixture{Normal}">
    ///         Mixture&lt;NormalDistribution&gt;
    ///     </see>
    ///     distribution and provides
    ///     mixture initialization using the K-Means clustering algorithm.
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Clustering\GaussianMixtureModelTest.cs" region="doc_learn" />
    /// </example>
    /// <seealso cref="KMeans" />
    /// <seealso cref="MeanShift" />
    [Serializable]
    public class GaussianMixtureModel : ParallelLearningBase,
        IUnsupervisedLearning<GaussianClusterCollection, double[], int>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GaussianMixtureModel" /> class.
        /// </summary>
        /// <param name="components">
        ///     The number of clusters in the clustering problem. This will be
        ///     used to set the number of components in the mixture model.
        /// </param>
        public GaussianMixtureModel(int components)
        {
            if (components <= 0)
            {
                throw new ArgumentOutOfRangeException("components",
                    "The number of components should be greater than zero.");
            }

            constructor(components);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GaussianMixtureModel" /> class.
        /// </summary>
        /// <param name="kmeans">
        ///     The initial solution as a K-Means clustering.
        /// </param>
        public GaussianMixtureModel(KMeans kmeans)
        {
            if (kmeans == null)
            {
                throw new ArgumentNullException("kmeans");
            }

            constructor(kmeans.K);

            Gaussians.Initialize(kmeans);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GaussianMixtureModel" /> class.
        /// </summary>
        /// <param name="mixture">
        ///     The initial solution as a mixture of normal distributions.
        /// </param>
        public GaussianMixtureModel(Mixture<NormalDistribution> mixture)
        {
            if (mixture == null)
            {
                throw new ArgumentNullException("mixture");
            }

            constructor(mixture.Components.Length);

            Gaussians.Initialize(mixture);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GaussianMixtureModel" /> class.
        /// </summary>
        /// <param name="mixture">
        ///     The initial solution as a mixture of normal distributions.
        /// </param>
        public GaussianMixtureModel(MultivariateMixture<MultivariateNormalDistribution> mixture)
        {
            if (mixture == null)
            {
                throw new ArgumentNullException("mixture");
            }

            constructor(mixture.Components.Length);

            Gaussians.Initialize(mixture);
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations to
        ///     be performed by the method. If set to zero, no
        ///     iteration limit will be imposed. Default is 0.
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        ///     Gets or sets the convergence criterion for the
        ///     Expectation-Maximization algorithm. Default is 1e-3.
        /// </summary>
        /// <value>The convergence threshold.</value>
        public double Tolerance { get; set; }

        /// <summary>
        ///     Gets or sets whether cluster labels should be computed
        ///     at the end of the learning iteration. Setting to <c>False</c>
        ///     might save a few computations in case they are not necessary.
        /// </summary>
        public bool ComputeLabels { get; set; }

        /// <summary>
        ///     Gets or sets whether the log-likelihood should be computed
        ///     at the end of the learning iteration. Setting to <c>False</c>
        ///     might save a few computations in case they are not necessary.
        /// </summary>
        public bool ComputeLogLikelihood { get; set; }

        /// <summary>
        ///     Gets the log-likelihood of the model at the last iteration.
        /// </summary>
        public double LogLikelihood { get; private set; }

        /// <summary>
        ///     Gets or sets how many random initializations to try.
        ///     Default is 3.
        /// </summary>
        public int Initializations { get; set; }

        /// <summary>
        ///     Gets how many iterations have been performed in the last call
        ///     to <see cref="Compute(double[][])" />.
        /// </summary>
        public int Iterations { get; private set; }

        /// <summary>
        ///     Gets or sets whether to make computations using the log
        ///     -domain. This might improve accuracy on large datasets.
        /// </summary>
        public bool UseLogarithm { get; set; }

        /// <summary>
        ///     Gets or sets the fitting options for the component
        ///     Gaussian distributions of the mixture model.
        /// </summary>
        /// <value>The fitting options for inner Gaussian distributions.</value>
        public NormalOptions Options { get; set; }

        /// <summary>
        ///     Gets the Gaussian components of the mixture model.
        /// </summary>
        public GaussianClusterCollection Gaussians { get; private set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public GaussianClusterCollection Learn(double[][] x, double[] weights = null)
        {
            if (Gaussians.Model == null)
            {
                Initialize(x, weights);
            }

            // Create the mixture options
            var mixtureOptions = new MixtureOptions
            {
                Threshold = Tolerance,
                InnerOptions = Options,
                MaxIterations = MaxIterations,
                Logarithm = UseLogarithm,
                ParallelOptions = ParallelOptions
            };

            var model = Gaussians.Model;

            // Fit a multivariate Gaussian distribution
            model.Fit(x, weights, mixtureOptions);
#pragma warning disable 612, 618
            Iterations = mixtureOptions.Iterations;
#pragma warning restore 612, 618

            for (var i = 0; i < Gaussians.Model.Components.Length; i++)
            {
                Gaussians.Centroids[i] = new MixtureComponent<MultivariateNormalDistribution>(model, i);
            }

            if (ComputeLogLikelihood)
            {
                LogLikelihood = model.LogLikelihood(x);
            }

            Debug.Assert(Gaussians.NumberOfClasses == Gaussians.Model.Components.Length);
            Debug.Assert(Gaussians.NumberOfOutputs == Gaussians.Model.Components.Length);
            Debug.Assert(Gaussians.NumberOfInputs == x[0].Length);

            return Gaussians;
        }

        private void constructor(int components)
        {
            ComputeLabels = true;
            ComputeLogLikelihood = true;
            Tolerance = 1e-3;
            Initializations = 3;
            MaxIterations = 0;
            UseLogarithm = true;
            Options = new NormalOptions();
            ParallelOptions = new ParallelOptions();

            // Initialize the model using the created objects.
            Gaussians = new GaussianClusterCollection(components);
        }

        /// <summary>
        ///     Divides the input data into K clusters modeling each
        ///     cluster as a multivariate Gaussian distribution.
        /// </summary>
        [Obsolete("Please use Learn(x) instead.")]
        public int[] Compute(double[][] data)
        {
            return Learn(data).Decide(data);
        }

        /// <summary>
        ///     Divides the input data into K clusters modeling each
        ///     cluster as a multivariate Gaussian distribution.
        /// </summary>
        [Obsolete("Please use Learn(x) instead.")]
        public int[] Compute(double[][] data, double[] weights)
        {
            return Learn(data, weights).Decide(data);
        }

        /// <summary>
        ///     Initializes the model with initial values obtained
        ///     through a run of the K-Means clustering algorithm.
        /// </summary>
        public void Initialize(double[][] data, double[] weights)
        {
            var kmeans = new KMeans[Initializations];
            var errors = new double[Initializations];

            Parallel.For(0, kmeans.Length, ParallelOptions, i =>
            {
                // Create a new K-Means algorithm
                kmeans[i] = new KMeans(Gaussians.Count)
                {
                    ComputeCovariances = true,
                    ComputeError = true,
                    UseSeeding = Seeding.KMeansPlusPlus,
                    Tolerance = Tolerance
                };

                // Compute the K-Means
                kmeans[i].Learn(data, weights);

                errors[i] = kmeans[i].Error;
            });

            var index = errors.ArgMin();

            // Initialize the model with K-Means
            Gaussians.Initialize(kmeans[index]);
        }

        /// <summary>
        ///     Initializes the model with initial values obtained
        ///     through a run of the K-Means clustering algorithm.
        /// </summary>
        public void Initialize(KMeans kmeans)
        {
            Gaussians.Initialize(kmeans);
        }

        /// <summary>
        ///     Initializes the model with initial values.
        /// </summary>
        public void Initialize(MultivariateNormalDistribution[] distributions)
        {
            Gaussians.Initialize(distributions);
        }

        /// <summary>
        ///     Initializes the model with initial values.
        /// </summary>
        public void Initialize(NormalDistribution[] distributions)
        {
            Gaussians.Initialize(distributions);
        }

        /// <summary>
        ///     Initializes the model with initial values.
        /// </summary>
        public void Initialize(double[] coefficients, MultivariateNormalDistribution[] distributions)
        {
            Gaussians.Initialize(coefficients, distributions);
        }

        /// <summary>
        ///     Initializes the model with initial values.
        /// </summary>
        public void Initialize(double[] coefficients, NormalDistribution[] distributions)
        {
            Gaussians.Initialize(coefficients, distributions);
        }

        /// <summary>
        ///     Initializes the model with initial values.
        /// </summary>
        public void Initialize(MultivariateMixture<MultivariateNormalDistribution> mixture)
        {
            Gaussians.Initialize(mixture);
        }

        /// <summary>
        ///     Initializes the model with initial values.
        /// </summary>
        public void Initialize(Mixture<NormalDistribution> mixture)
        {
            Gaussians.Initialize(mixture);
        }

        /// <summary>
        ///     Gets a copy of the mixture distribution modeled by this Gaussian Mixture Model.
        /// </summary>
        public MultivariateMixture<MultivariateNormalDistribution> ToMixtureDistribution()
        {
            return Gaussians.ToMixtureDistribution();
        }

        # region Deprecated

        /// <summary>
        ///     Divides the input data into K clusters modeling each
        ///     cluster as a multivariate Gaussian distribution.
        /// </summary>
        [Obsolete("Please set the properties of this class directly and use Learn(double[]) instead.")]
        public double Compute(double[][] data, GaussianMixtureModelOptions options)
        {
            Options = options.NormalOptions;
            MaxIterations = options.Iterations;
            UseLogarithm = options.Logarithm;
            Tolerance = options.Threshold;
            ParallelOptions = options.ParallelOptions;

            Compute(data, options.Weights);

            return LogLikelihood;
        }

        /// <summary>
        ///     Divides the input data into K clusters modeling each
        ///     cluster as a multivariate Gaussian distribution.
        /// </summary>
        [Obsolete("Please set the tolerance threshold using the Tolerance property and use Compute(double[]) instead.")]
        public double Compute(double[][] data, double threshold)
        {
            Tolerance = threshold;
            Learn(data);
            return LogLikelihood;
        }

        #endregion
    }

    /// <summary>
    ///     Options for Gaussian Mixture Model fitting.
    /// </summary>
    /// <remarks>
    ///     This class provides different options that can be passed to a
    ///     <see cref="GaussianMixtureModel" /> object when calling its
    ///     <see cref="GaussianMixtureModel.Compute(double[][], GaussianMixtureModelOptions)" />
    ///     method.
    /// </remarks>
    /// <seealso cref="GaussianMixtureModel" />
    [Obsolete("This class will be removed.")]
    public class GaussianMixtureModelOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GaussianMixtureModelOptions" /> class.
        /// </summary>
        public GaussianMixtureModelOptions()
        {
            Threshold = 1e-3;
            ParallelOptions = new ParallelOptions();
        }

        /// <summary>
        ///     Gets or sets the convergence criterion for the
        ///     Expectation-Maximization algorithm. Default is 1e-3.
        /// </summary>
        /// <value>The convergence threshold.</value>
        public double Threshold { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     to be performed by the Expectation-Maximization
        ///     algorithm. Default is zero (iterate until convergence).
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        ///     Gets or sets whether to make computations using the log
        ///     -domain. This might improve accuracy on large datasets.
        /// </summary>
        public bool Logarithm { get; set; }

        /// <summary>
        ///     Gets or sets the sample weights. If set to null,
        ///     the data will be assumed equal weights. Default
        ///     is null.
        /// </summary>
        public double[] Weights { get; set; }

        /// <summary>
        ///     Gets or sets the fitting options for the component
        ///     Gaussian distributions of the mixture model.
        /// </summary>
        /// <value>The fitting options for inner Gaussian distributions.</value>
        public NormalOptions NormalOptions { get; set; }

        /// <summary>
        ///     Gets or sets parallelization options.
        /// </summary>
        public ParallelOptions ParallelOptions { get; set; }
    }
}