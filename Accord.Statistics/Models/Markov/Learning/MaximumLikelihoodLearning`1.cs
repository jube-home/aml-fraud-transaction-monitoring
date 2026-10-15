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
using System.Collections.Generic;
using Accord.Diagnostics;
using Accord.Math;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Models.Markov.Learning
{
#pragma warning disable 612, 618

    /// <summary>
    ///     Obsolete. Please use <see cref="MaximumLikelihoodLearning{TDistribution, TObservation}" /> instead.
    /// </summary>
    [Obsolete("Please use MaximumLikelihoodLearning<TDistribution, TObservation> instead.")]
    public class MaximumLikelihoodLearning<TDistribution> : ISupervisedLearning
        where TDistribution : IDistribution
    {
        private readonly int[] initial;
        private readonly int[,] transitions;

        /// <summary>
        ///     Creates a new instance of the Maximum Likelihood learning algorithm.
        /// </summary>
        public MaximumLikelihoodLearning(HiddenMarkovModel<TDistribution> model)
        {
            this.Model = model;

            var states = model.States;

            initial = new int[states];
            transitions = new int[states, states];
        }

        /// <summary>
        ///     Gets the model being trained.
        /// </summary>
        public HiddenMarkovModel<TDistribution> Model { get; }

        /// <summary>
        ///     Gets or sets whether the emission fitting algorithm should
        ///     present weighted samples or simply the clustered samples to
        ///     the
        ///     <see cref="IDistribution.Fit(Array)">
        ///         density estimation
        ///         methods
        ///     </see>
        ///     .
        /// </summary>
        public bool UseWeights { get; set; }

        /// <summary>
        ///     Gets or sets whether to use Laplace's rule
        ///     of succession to avoid zero probabilities.
        /// </summary>
        public bool UseLaplaceRule { get; set; } = true;

        /// <summary>
        ///     Gets or sets the distribution fitting options
        ///     to use when estimating distribution densities
        ///     during learning.
        /// </summary>
        /// <value>The distribution fitting options.</value>
        public IFittingOptions FittingOptions { get; set; }

        /// <summary>
        ///     Runs the Maximum Likelihood learning algorithm for hidden Markov models.
        /// </summary>
        /// <param name="observations">An array of observation sequences to be used to train the model.</param>
        /// <param name="paths">An array of state labels associated to each observation sequence.</param>
        /// <returns>
        ///     The average log-likelihood for the observations after the model has been trained.
        /// </returns>
        /// <remarks>
        ///     Supervised learning problem. Given some training observation sequences O = {o1, o2, ..., oK},
        ///     known training state paths H = {h1, h2, ..., hK} and general structure of HMM (numbers of
        ///     hidden and visible states), determine HMM parameters M = (A, B, pi) that best fit training data.
        /// </remarks>
        double ISupervisedLearning.Run(Array[] observations, int[][] paths)
        {
            return Run(observations as int[][], paths);
        }

        /// <summary>
        ///     Runs the Maximum Likelihood learning algorithm for hidden Markov models.
        /// </summary>
        /// <param name="observations">An array of observation sequences to be used to train the model.</param>
        /// <param name="paths">An array of state labels associated to each observation sequence.</param>
        /// <returns>
        ///     The average log-likelihood for the observations after the model has been trained.
        /// </returns>
        /// <remarks>
        ///     Supervised learning problem. Given some training observation sequences O = {o1, o2, ..., oK},
        ///     known training state paths H = {h1, h2, ..., hK} and general structure of HMM (numbers of
        ///     hidden and visible states), determine HMM parameters M = (A, B, pi) that best fit training data.
        /// </remarks>
        public double Run(Array[] observations, int[][] paths)
        {
            // Convert the generic representation to a vector of multivariate sequences
            var obs = observations as double[][][];

            if (obs == null)
            {
                obs = new double[observations.Length][][];
                for (var i = 0; i < observations.Length; i++)
                {
                    obs[i] = convert(observations[i], Model.Dimension);
                }
            }

            // Grab model information
            var states = Model.States;

            Array.Clear(initial, 0, initial.Length);
            Array.Clear(transitions, 0, transitions.Length);


            // 1. Count first state occurrences
            for (var i = 0; i < paths.Length; i++)
            {
                initial[paths[i][0]]++;
            }

            // 2. Count all state transitions
            foreach (var path in paths)
            {
                for (var j = 1; j < path.Length; j++)
                {
                    transitions[path[j - 1], path[j]]++;
                }
            }

            if (UseWeights)
            {
                var totalObservations = 0;
                for (var i = 0; i < obs.Length; i++)
                {
                    totalObservations += obs[i].Length;
                }

                var weights = new double[states][];
                for (var i = 0; i < weights.Length; i++)
                {
                    weights[i] = new double[totalObservations];
                }

                var all = new double[totalObservations][];

                for (int i = 0, c = 0; i < paths.Length; i++)
                {
                    for (var t = 0; t < paths[i].Length; t++, c++)
                    {
                        var state = paths[i][t];
                        all[c] = obs[i][t];
                        weights[state][c] = 1;
                    }
                }

                for (var i = 0; i < Model.States; i++)
                {
                    Model.Emissions[i].Fit(all, weights[i], FittingOptions);
                }
            }
            else
            {
                // 3. Count emissions for each state
                var clusters = new List<double[]>[Model.States];
                for (var i = 0; i < clusters.Length; i++)
                {
                    clusters[i] = new List<double[]>();
                }

                // Count symbol frequencies per state
                for (var i = 0; i < paths.Length; i++)
                {
                    for (var t = 0; t < paths[i].Length; t++)
                    {
                        var state = paths[i][t];
                        var symbol = obs[i][t];

                        clusters[state].Add(symbol);
                    }
                }

                // Estimate probability distributions
                for (var i = 0; i < Model.States; i++)
                {
                    if (clusters[i].Count > 0)
                    {
                        Model.Emissions[i].Fit(clusters[i].ToArray(), FittingOptions);
                    }
                }
            }

            // 4. Form log-probabilities, using the Laplace
            //    correction to avoid zero probabilities

            if (UseLaplaceRule)
            {
                // Use Laplace's rule of succession correction
                // http://en.wikipedia.org/wiki/Rule_of_succession

                for (var i = 0; i < initial.Length; i++)
                {
                    initial[i]++;

                    for (var j = 0; j < states; j++)
                    {
                        transitions[i, j]++;
                    }
                }
            }

            // Form probabilities
            var initialCount = initial.Sum();
            var transitionCount = transitions.Sum(1);

            if (initialCount == 0)
            {
                initialCount = 1;
            }

            for (var i = 0; i < transitionCount.Length; i++)
            {
                if (transitionCount[i] == 0)
                {
                    transitionCount[i] = 1;
                }
            }


            for (var i = 0; i < initial.Length; i++)
            {
                Model.LogInitial[i] = System.Math.Log(initial[i] / (double)initialCount);
            }

            for (var i = 0; i < transitionCount.Length; i++)
            {
                for (var j = 0; j < states; j++)
                {
                    Model.LogTransitions[i][j] = System.Math.Log(transitions[i, j] / (double)transitionCount[i]);
                }
            }

            Debug.Assert(!Model.LogInitial.HasNaN());
            Debug.Assert(!Model.Transitions.HasNaN());


            // 5. Compute log-likelihood
            var logLikelihood = double.NegativeInfinity;
            for (var i = 0; i < observations.Length; i++)
            {
                logLikelihood = Special.LogSum(logLikelihood, Model.Evaluate(observations[i]));
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