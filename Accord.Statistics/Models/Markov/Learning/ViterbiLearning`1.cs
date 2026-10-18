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
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Models.Markov.Learning
{
#pragma warning disable 612, 618

    /// <summary>
    ///     Obsolete. Please use ViterbiLearning&lt;TDistribution, TObservation> instead.
    /// </summary>
    [Obsolete("Please use ViterbiLearning<TDistribution, TObservation> instead.")]
    public class ViterbiLearning<TDistribution> : BaseViterbiLearning<double[][]>,
        IUnsupervisedLearning, IConvergenceLearning
        where TDistribution : IDistribution
    {
        private readonly MaximumLikelihoodLearning<TDistribution> mle;

        /// <summary>
        ///     Creates a new instance of the Viterbi learning algorithm.
        /// </summary>
        public ViterbiLearning(HiddenMarkovModel<TDistribution> model)
        {
            mle = new MaximumLikelihoodLearning<TDistribution>(model);
        }

        /// <summary>
        ///     Gets the model being trained.
        /// </summary>
        public HiddenMarkovModel<TDistribution> Model => mle.Model;

        /// <summary>
        ///     Gets or sets the distribution fitting options
        ///     to use when estimating distribution densities
        ///     during learning.
        /// </summary>
        /// <value>The distribution fitting options.</value>
        public IFittingOptions FittingOptions
        {
            get => mle.FittingOptions;
            set => mle.FittingOptions = value;
        }

        /// <summary>
        ///     Gets or sets whether to use Laplace's rule
        ///     of succession to avoid zero probabilities.
        /// </summary>
        /// <remarks>
        ///     When this property is set, it will only affect the estimation
        ///     of the transition and initial state probabilities. To control
        ///     the estimation of the emission probabilities, please use the
        ///     corresponding <see cref="FittingOptions" /> property.
        /// </remarks>
        public bool UseLaplaceRule
        {
            get => mle.UseLaplaceRule;
            set => mle.UseLaplaceRule = value;
        }

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        /// <remarks>
        ///     Learning problem. Given some training observation sequences O = {o1, o2, ..., oK}
        ///     and general structure of HMM (numbers of hidden and visible states), determine
        ///     HMM parameters M = (A, B, pi) that best fit training data.
        /// </remarks>
        public double Run(params Array[] observations)
        {
            var model = mle.Model;

            // Convert the generic representation to a vector of multivariate sequences
            var vectorObservations = new double[observations.Length][][];
            for (var i = 0; i < observations.Length; i++)
            {
                vectorObservations[i] = convert(observations[i], model.Dimension);
            }

            return base.Run(vectorObservations);
        }

        /// <summary>
        ///     Runs one single epoch (iteration) of the learning algorithm.
        /// </summary>
        /// <param name="inputs">The observation sequences.</param>
        /// <param name="outputs">A vector to be populated with the decoded Viterbi sequences.</param>
        protected override void RunEpoch(double[][][] inputs, int[][] outputs)
        {
            var model = mle.Model;

            // Compute the Viterbi path for all sequences
            for (var i = 0; i < inputs.Length; i++)
            {
                outputs[i] = model.Decode(inputs[i]);
            }

            // Compute Maximum Likelihood Estimation 
            mle.Run(inputs, outputs);
        }

        /// <summary>
        ///     Computes the log-likelihood for the current model for the given observations.
        /// </summary>
        /// <param name="observations">The observation vectors.</param>
        /// <returns>The log-likelihood of the observations belonging to the model.</returns>
        protected override double ComputeLogLikelihood(double[][][] observations)
        {
            var model = mle.Model;

            var logLikelihood = double.NegativeInfinity;
            for (var i = 0; i < observations.Length; i++)
            {
                logLikelihood = Special.LogSum(logLikelihood, model.Evaluate(observations[i]));
            }

            return logLikelihood;
        }

        /// <summary>
        ///     Converts a univariate or multivariate array
        ///     of observations into a two-dimensional jagged array.
        /// </summary>
        private static double[][] convert(Array array, int dimension)
        {
            var multivariate = array as double[][];
            if (multivariate != null)
            {
                return multivariate;
            }

            var univariate = array as double[];
            if (univariate != null)
            {
                return univariate.Split(dimension);
            }

            throw new ArgumentException("Invalid array argument type.", "array");
        }
    }
}