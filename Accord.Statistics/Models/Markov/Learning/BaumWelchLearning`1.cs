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
using Accord.Diagnostics;
using Accord.Math;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Models.Markov.Learning
{
#pragma warning disable 612, 618
    /// <summary>
    ///     Obsolete. Please use <see cref="BaumWelchLearning{TDistribution, TObservation}" /> instead.
    /// </summary>
    [Obsolete("Please use BaumWelchLearning<TDistribution, TObservation> instead.")]
    public class BaumWelchLearning<TDistribution> : BaseBaumWelchLearning, IUnsupervisedLearning, IConvergenceLearning
        where TDistribution : IDistribution
    {
        private Array samples;

        private double[][][] vectorObservations;
        private double[] weights;

        /// <summary>
        ///     Creates a new instance of the Baum-Welch learning algorithm.
        /// </summary>
        public BaumWelchLearning(HiddenMarkovModel<TDistribution> model)
            : base(model)
        {
            this.Model = model;
        }

        /// <summary>
        ///     Gets the model being trained.
        /// </summary>
        public HiddenMarkovModel<TDistribution> Model { get; }

        /// <summary>
        ///     Gets or sets the distribution fitting options
        ///     to use when estimating distribution densities
        ///     during learning.
        /// </summary>
        /// <value>The distribution fitting options.</value>
        public IFittingOptions FittingOptions { get; set; }

        /// <summary>
        ///     Runs the Baum-Welch learning algorithm for hidden Markov models.
        /// </summary>
        /// <remarks>
        ///     Learning problem. Given some training observation sequences O = {o1, o2, ..., oK}
        ///     and general structure of HMM (numbers of hidden and visible states), determine
        ///     HMM parameters M = (A, B, pi) that best fit training data.
        /// </remarks>
        /// <param name="observations">
        ///     The sequences of univariate or multivariate observations used to train the model.
        ///     Can be either of type double[] (for the univariate case) or double[][] for the
        ///     multivariate case.
        /// </param>
        /// <returns>
        ///     The average log-likelihood for the observations after the model has been trained.
        /// </returns>
        public new double Run(params Array[] observations)
        {
            // Convert the generic representation to a vector of multivariate sequences
            vectorObservations = new double[observations.Length][][];
            for (var i = 0; i < vectorObservations.Length; i++)
            {
                vectorObservations[i] = MarkovHelperMethods.checkAndConvert(observations[i], Model.Dimension);
            }


            // Sample array, used to store all observations as a sample vector
            //   will be useful when fitting the distribution models.
            if (Model.Emissions[0] is IUnivariateDistribution)
            {
                samples = Matrix.Stack(vectorObservations).Concatenate(
                );
            }
            else
            {
                samples = Matrix.Stack(vectorObservations);
            }

            weights = new double[samples.Length];


            return base.Run(observations);
        }

        /// <summary>
        ///     Computes the ksi matrix of probabilities for a given observation
        ///     referenced by its index in the input training data.
        /// </summary>
        /// <param name="index">The index of the observation in the input training data.</param>
        /// <param name="lnFwd">The matrix of forward probabilities for the observation.</param>
        /// <param name="lnBwd">The matrix of backward probabilities for the observation.</param>
        protected override void ComputeKsi(int index, double[,] lnFwd, double[,] lnBwd)
        {
            var states = Model.States;
            var logA = Model.Transitions;
            var B = Model.Emissions;

            var sequence = vectorObservations[index];

            var T = sequence.Length;
            var logKsi = LogKsi[index];
            var w = LogWeights[index];


            for (var t = 0; t < T - 1; t++)
            {
                var lnsum = double.NegativeInfinity;
                var x = sequence[t + 1];

                for (var i = 0; i < states; i++)
                {
                    for (var j = 0; j < states; j++)
                    {
                        var b = B[j].LogProbabilityFunction(x);
                        logKsi[t][i, j] = lnFwd[t, i] + lnBwd[t + 1, j] + logA[i, j] + b + w;
                        lnsum = Special.LogSum(lnsum, logKsi[t][i, j]);
                    }
                }

                Debug.Assert(!double.IsNaN(lnsum));

                // Normalize if different from zero
                if (lnsum != double.NegativeInfinity)
                {
                    for (var i = 0; i < states; i++)
                    {
                        for (var j = 0; j < states; j++)
                        {
                            logKsi[t][i, j] = logKsi[t][i, j] - lnsum;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Updates the emission probability matrix.
        /// </summary>
        /// <remarks>
        ///     Implementations of this method should use the observations
        ///     in the training data and the Gamma probability matrix to
        ///     update the probability distributions of symbol emissions.
        /// </remarks>
        protected override void UpdateEmissions()
        {
            var B = Model.Emissions;

            // For each state i in the model
            for (var i = 0; i < B.Length; i++)
            {
                var lnsum = double.NegativeInfinity;

                // For each observation sequence k
                for (int k = 0, w = 0; k < vectorObservations.Length; k++)
                {
                    var T = vectorObservations[k].Length;

                    // For each observation t in k
                    for (var t = 0; t < T; t++, w++)
                    {
                        weights[w] = LogGamma[k][t, i];
                        lnsum = Special.LogSum(lnsum, weights[w]);
                    }
                }

                Debug.Assert(!double.IsNaN(lnsum));

                if (lnsum != double.NegativeInfinity)
                {
                    for (var w = 0; w < weights.Length; w++)
                    {
                        weights[w] = weights[w] - lnsum;
                    }

                    // Convert to probabilities
                    for (var w = 0; w < weights.Length; w++)
                    {
                        var p = System.Math.Exp(weights[w]);
                        weights[w] = double.IsNaN(p) || double.IsInfinity(p) ? 0.0 : p;
                    }

                    // Estimate the distribution for state i
                    B[i].Fit(samples, weights, FittingOptions);
                }
            }
        }

        /// <summary>
        ///     Computes the forward and backward probabilities matrices
        ///     for a given observation referenced by its index in the
        ///     input training data.
        /// </summary>
        /// <param name="index">The index of the observation in the input training data.</param>
        /// <param name="lnFwd">Returns the computed forward probabilities matrix.</param>
        /// <param name="lnBwd">Returns the computed backward probabilities matrix.</param>
        protected override void ComputeForwardBackward(int index, double[,] lnFwd, double[,] lnBwd)
        {
            var states = Model.States;
            var T = vectorObservations[index].Length;

            Debug.Assert(lnBwd.GetLength(0) >= T);
            Debug.Assert(lnBwd.GetLength(1) == states);
            Debug.Assert(lnFwd.GetLength(0) >= T);
            Debug.Assert(lnFwd.GetLength(1) == states);

            ForwardBackwardAlgorithm.LogForward(Model, vectorObservations[index], lnFwd);
            ForwardBackwardAlgorithm.LogBackward(Model, vectorObservations[index], lnBwd);
        }
    }
}