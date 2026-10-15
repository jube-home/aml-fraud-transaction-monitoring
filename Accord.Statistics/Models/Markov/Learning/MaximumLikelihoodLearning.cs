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
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Models.Markov.Learning
{
#pragma warning disable 612, 618

    /// <summary>
    ///     Maximum Likelihood learning algorithm for
    ///     <see cref="HiddenMarkovModel">
    ///         discrete-density Hidden Markov Models
    ///     </see>
    ///     .
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The maximum likelihood estimate is a
    ///         <see cref="ISupervisedLearning">
    ///             supervised learning algorithm
    ///         </see>
    ///         . It considers both the sequence
    ///         of observations as well as the sequence of states in the Markov model
    ///         are visible and thus during training.
    ///     </para>
    ///     <para>
    ///         Often, the Maximum Likelihood Estimate can be used to give a starting
    ///         point to a unsupervised algorithm, making possible to use semi-supervised
    ///         techniques with HMMs. It is possible, for example, to use MLE to guess
    ///         initial values for an HMM given a small set of manually labeled labels,
    ///         and then further estimate this model using the
    ///         <see cref="ViterbiLearning">
    ///             Viterbi learning algorithm
    ///         </see>
    ///         .
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example comes from Prof. Yechiam Yemini slides on Hidden Markov
    ///         Models, available at http://www.cs.columbia.edu/4761/notes07/chapter4.3-HMM.pdf.
    ///         In this example, we will be specifying both the sequence of observations and
    ///         the sequence of states assigned to each observation in each sequence to learn
    ///         our Markov model.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\MaximumLikelihoodLearningTest.cs" region="doc_learn" />
    /// </example>
    /// <seealso cref="MaximumLikelihoodLearning" />
    /// <seealso cref="ViterbiLearning" />
    /// <seealso cref="HiddenMarkovModel" />
    public class MaximumLikelihoodLearning :
        BaseMaximumLikelihoodLearning<HiddenMarkovModel, GeneralDiscreteDistribution, int, GeneralDiscreteOptions>,
        ISupervisedLearning, ISupervisedLearning<HiddenMarkovModel, int[], int[]>
    {
        /// <summary>
        ///     Creates a new instance of the Maximum Likelihood learning algorithm.
        /// </summary>
        public MaximumLikelihoodLearning(HiddenMarkovModel model)
        {
            Model = model;
        }

        /// <summary>
        ///     Creates a new instance of the Maximum Likelihood learning algorithm.
        /// </summary>
        public MaximumLikelihoodLearning()
        {
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
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(int[][] observations, int[][] paths)
        {
            run(observations, paths);

            // 5. Compute log-likelihood
            var logLikelihood = double.NegativeInfinity;
            for (var i = 0; i < observations.Length; i++)
            {
                logLikelihood = Special.LogSum(logLikelihood, Model.Evaluate(observations[i]));
            }

            return logLikelihood;
        }

        private void run(int[][] observations, int[][] paths)
        {
            // Grab model information
            var model = Model;
            var states = model.States;
            var symbols = model.Symbols;

            var initial = new int[states];
            var transitions = new int[states, states];
            var emissions = new int[states, symbols];

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

            // 3. Count emissions for each state
            for (var i = 0; i < observations.Length; i++)
            {
                for (var j = 0; j < observations[i].Length; j++)
                {
                    emissions[paths[i][j], observations[i][j]]++;
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

                    for (var k = 0; k < symbols; k++)
                    {
                        emissions[i, k]++;
                    }
                }
            }

            // Form probabilities
            var initialCount = initial.Sum();
            var transitionCount = transitions.Sum(1);
            var emissionCount = emissions.Sum(1);

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

            for (var i = 0; i < emissionCount.Length; i++)
            {
                if (emissionCount[i] == 0)
                {
                    emissionCount[i] = 1;
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

            for (var i = 0; i < emissionCount.Length; i++)
            {
                for (var j = 0; j < symbols; j++)
                {
                    model.LogEmissions[i][j] = System.Math.Log(emissions[i, j] / (double)emissionCount[i]);
                }
            }

            Debug.Assert(!model.LogInitial.HasNaN());
            Debug.Assert(!model.LogTransitions.HasNaN());
            Debug.Assert(!model.Emissions.HasNaN());
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors of this abstract
        ///     class must define this method so new models can be created from the training data.
        /// </summary>
        protected override HiddenMarkovModel Create(int[][] x, int numberOfClasses)
        {
            return new HiddenMarkovModel(numberOfClasses, x.Max() + 1);
        }
    }
}