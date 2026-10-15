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
using System.Threading;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.MachineLearning.Bayes
{
#if !MONO
    /// <summary>
    ///     Base class for Naive Bayes learning algorithms.
    /// </summary>
    /// <typeparam name="TModel">The type for the Naive Bayes model to be learned.</typeparam>
    /// <typeparam name="TDistribution">The univariate distribution to be used as components in the Naive Bayes distribution.</typeparam>
    /// <typeparam name="TInput">The type for the samples modeled by the distribution.</typeparam>
    /// <typeparam name="TOptions">The fitting options for the independent distribution.</typeparam>
    public abstract class NaiveBayesLearningBase<TModel, TDistribution, TInput, TOptions> :
        ISupervisedLearning<TModel, TInput[], double[]>,
        ISupervisedLearning<TModel, TInput[], int>,
        IParallel
        where TDistribution : IFittableDistribution<TInput>,
        IUnivariateDistribution<TInput>,
        IUnivariateDistribution
        where TOptions : IndependentOptions, new()
        where TModel : NaiveBayes<TDistribution, TInput>
    {
        internal bool optimized;

        /// <summary>
        ///     Constructs a new Naïve Bayes learning algorithm.
        /// </summary>
        public NaiveBayesLearningBase()
        {
            Empirical = true;
            ParallelOptions = new ParallelOptions();
            Options = new TOptions();
            Options.Transposed = true;
            Distribution = (classIndex, variableIndex) =>
            {
                try
                {
                    return Activator.CreateInstance<TDistribution>();
                }
                catch
                {
                    throw new InvalidOperationException(
                        "Please set the Distribution property to specify how the initial distributions should be created.");
                }
            };
        }

        /// <summary>
        ///     Gets or sets the model being learned.
        /// </summary>
        public TModel Model { get; set; }

        /// <summary>
        ///     Gets or sets whether the class priors should be estimated
        ///     from the data.
        /// </summary>
        public bool Empirical { get; set; }

        /// <summary>
        ///     Gets or sets the fitting options to use when
        ///     estimating the class-specific distributions.
        /// </summary>
        public TOptions Options { get; set; }

        /// <summary>
        ///     Gets or sets the distribution creation function. This function can
        ///     be used to specify how the initial distributions of the model should
        ///     be created. By default, this function attempts to call the empty
        ///     constructor of the distribution using <c>Activator.CreateInstance()</c>.
        /// </summary>
        public Func<int, int, TDistribution> Distribution { get; set; }

        /// <summary>
        ///     Gets or sets the parallelization options for this algorithm.
        /// </summary>
        public ParallelOptions ParallelOptions { get; set; }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        public CancellationToken Token
        {
            get => ParallelOptions.CancellationToken;
            set => ParallelOptions.CancellationToken = value;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weight">The weight of importance for each input-output pair.</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public virtual TModel Learn(TInput[][] x, double[][] y, double[] weight = null)
        {
            var originalX = x;
            Tools.CheckArgs(x, y, weight, () =>
            {
                if (Model == null)
                {
                    Model = Create(originalX, y.Columns());
                }

                return Model;
            });

            // For efficiency
            x = x.Transpose();

            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                // For each class
                for (var i = 0; i < Model.NumberOfOutputs; i++)
                {
                    InnerLearn(x, y, weight, i);
                }
            }
            else
            {
                // For each class
                Parallel.For(0, Model.NumberOfOutputs, ParallelOptions, i =>
                    InnerLearn(x, y, weight, i));
            }

            return Model;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weight">The weight of importance for each input-output pair.</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public virtual TModel Learn(TInput[][] x, int[] y, double[] weight = null)
        {
            Tools.CheckArgs(x, y, weight, () =>
            {
                if (Model == null)
                {
                    Model = Create(x, y.Max() + 1);
                }

                return Model;
            });

            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                for (var i = 0; i < Model.NumberOfOutputs; i++)
                {
                    InnerLearn(x, y, weight, i);
                }
            }
            else
            {
                // For each class
                Parallel.For(0, Model.NumberOfOutputs, ParallelOptions, i =>
                    InnerLearn(x, y, weight, i));
            }

            return Model;
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected abstract TModel Create(TInput[][] x, int y);

        private void InnerLearn(TInput[][] x, int[] y, double[] weight, int classIndex)
        {
            // Estimate conditional distributions
            // Get variables values in class i
            var sampleIndicesInClass = y.Find(y_i => y_i == classIndex);
            var samplesInClass = x.Get(sampleIndicesInClass, true);
            var classWeights = weight == null ? null : weight.Get(sampleIndicesInClass);

            if (Empirical)
            {
                if (weight == null)
                {
                    Model.Priors[classIndex] = sampleIndicesInClass.Length / (double)x.Length;
                }
                else
                {
                    Model.Priors[classIndex] = classWeights.Sum() / weight.Sum();
                }
            }

            Fit(classIndex, samplesInClass, classWeights, true);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weight">The weight of importance for each input-output pair.</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public virtual TModel Learn(TInput[][] x, int[][] y, double[] weight = null)
        {
            return Learn(x, y.ToDouble(), weight);
        }

        private void InnerLearn(TInput[][] x, double[][] y, double[] weight, int classIndex)
        {
            // Estimate conditional distributions
            // Get variables values in class i
            var target = y.GetColumn(classIndex);

            if (weight != null)
            {
                target.Multiply(weight, target);
            }

            if (Empirical)
            {
                Model.Priors[classIndex] = weight == null ? target.Sum() / x.Length : target.Sum() / weight.Sum();
            }

            Fit(classIndex, x, target, true);
        }

        /// <summary>
        ///     Fits one of the distributions in the naive bayes model.
        /// </summary>
        protected virtual void Fit(int i, TInput[][] values, double[] weights, bool transposed)
        {
            Options.Transposed = transposed;

            var fit = Model.Distributions[i] as IFittableDistribution<TInput[], TOptions>;
            if (fit != null)
            {
                // Use a more efficient call if available
                fit.Fit(values, weights, Options);
                optimized = true;
            }
            else
            {
                // Use a generic call (which might need type casting)
                Model.Distributions[i].Fit(values, weights, Options);
                optimized = false;
            }
        }
    }
#endif
}