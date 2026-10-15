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
using System.Threading;
using Accord.Diagnostics;
using Accord.Math;
using Accord.Properties;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Models.Markov.Learning
{
    /// <summary>
    ///     Base class for observable Markov model learning algorithms.
    /// </summary>
    public abstract class BaseMaximumLikelihoodLearning<TModel, TDistribution, TObservation, TOptions>
        where TDistribution : IFittableDistribution<TObservation>
        where TModel : HiddenMarkovModel<TDistribution, TObservation>
        where TOptions : IFittingOptions
    {
        /// <summary>
        ///     Creates a new instance of the Maximum Likelihood learning algorithm.
        /// </summary>
        public BaseMaximumLikelihoodLearning()
        {
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        [field: NonSerialized]
        public CancellationToken Token { get; set; }

        /// <summary>
        ///     Gets the model being trained.
        /// </summary>
        public TModel Model { get; set; }

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
        ///     Gets or sets the function that initializes the emission
        ///     distributions in the hidden Markov Models.
        /// </summary>
        public Func<int, TDistribution> Emissions { get; set; }

        /// <summary>
        ///     Gets or sets the distribution fitting options
        ///     to use when estimating distribution densities
        ///     during learning.
        /// </summary>
        /// <value>The distribution fitting options.</value>
        public TOptions FittingOptions { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        public TModel Learn(TObservation[][] x, int[][] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            if (Model == null)
            {
                Model = Create(x, y.Max() + 1);
            }

            MarkovHelperMethods.CheckObservationDimensions(x, Model);

            // Grab model information
            var model = Model;
            var fittingOptions = FittingOptions;

            var states = model.NumberOfStates;

            var initial = new int[states];
            var transitions = new int[states, states];


            // 1. Count first state occurrences
            for (var i = 0; i < y.Length; i++)
            {
                initial[y[i][0]]++;
            }

            // 2. Count all state transitions
            foreach (var path in y)
            {
                for (var j = 1; j < path.Length; j++)
                {
                    transitions[path[j - 1], path[j]]++;
                }
            }

            if (UseWeights)
            {
                var totalObservations = 0;
                for (var i = 0; i < x.Length; i++)
                {
                    totalObservations += x[i].Length;
                }

                var totalWeights = new double[states][];
                for (var i = 0; i < totalWeights.Length; i++)
                {
                    totalWeights[i] = new double[totalObservations];
                }

                var all = new TObservation[totalObservations];

                for (int i = 0, c = 0; i < y.Length; i++)
                {
                    for (var t = 0; t < y[i].Length; t++, c++)
                    {
                        var state = y[i][t];
                        all[c] = x[i][t];
                        totalWeights[state][c] = 1;
                    }
                }

                for (var i = 0; i < model.NumberOfStates; i++)
                {
                    model.Emissions[i].Fit(all, totalWeights[i], fittingOptions);
                }
            }
            else
            {
                // 3. Count emissions for each state
                var clusters = new List<TObservation>[model.NumberOfStates];
                for (var i = 0; i < clusters.Length; i++)
                {
                    clusters[i] = new List<TObservation>();
                }

                // Count symbol frequencies per state
                for (var i = 0; i < y.Length; i++)
                {
                    for (var t = 0; t < y[i].Length; t++)
                    {
                        var state = y[i][t];
                        var symbol = x[i][t];

                        clusters[state].Add(symbol);
                    }
                }

                // Estimate probability distributions
                for (var i = 0; i < model.NumberOfStates; i++)
                {
                    if (clusters[i].Count > 0)
                    {
                        model.Emissions[i].Fit(clusters[i].ToArray(), fittingOptions);
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
                model.LogInitial[i] = System.Math.Log(initial[i] / (double)initialCount);
            }

            for (var i = 0; i < transitionCount.Length; i++)
            {
                for (var j = 0; j < states; j++)
                {
                    model.LogTransitions[i][j] = System.Math.Log(transitions[i, j] / (double)transitionCount[i]);
                }
            }

            Debug.Assert(!model.LogInitial.HasNaN());
            Debug.Assert(!model.LogTransitions.HasNaN());

            return Model;
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors of this abstract
        ///     class must define this method so new models can be created from the training data.
        /// </summary>
        protected abstract TModel Create(TObservation[][] x, int numberOfClasses);
    }
}