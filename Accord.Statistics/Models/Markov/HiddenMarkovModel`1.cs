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
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Markov.Topology;

namespace Accord.Statistics.Models.Markov
{
#pragma warning disable 612, 618
    /// <summary>
    ///     Obsolete. Please use <see cref="HiddenMarkovModel{TDistribution, TObservation}" /> instead.
    /// </summary>
    [Serializable]
    [Obsolete("Please use HiddenMarkovModel<TDistribution, TObservation> instead.")]
    public class HiddenMarkovModel<TDistribution> : BaseHiddenMarkovModel, IHiddenMarkovModel, ICloneable
        where TDistribution : IDistribution
    {
        // Model is defined as M = (A, B, pi)

        // The other parameters are defined in HiddenMarkovModelBase
        // private double[,] A; // Transition probabilities
        // private double[] pi; // Initial state probabilities

        private bool multivariate;

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var A = (double[,])Transitions.Clone();
            var pi = (double[])Probabilities.Clone();

            var B = new TDistribution[Emissions.Length];
            for (var i = 0; i < Emissions.Length; i++)
            {
                B[i] = (TDistribution)Emissions[i].Clone();
            }

            return new HiddenMarkovModel<TDistribution>(A, B, pi, true);
        }

        #region Constructors

        /// <summary>
        ///     Constructs a new Hidden Markov Model with arbitrary-density state probabilities.
        /// </summary>
        /// <param name="topology">
        ///     A <see cref="Topology" /> object specifying the initial values of the matrix of transition
        ///     probabilities <c>A</c> and initial state probabilities <c>pi</c> to be used by this model.
        /// </param>
        /// <param name="emissions">
        ///     The initial emission probability distribution to be used by each of the states. This
        ///     initial probability distribution will be cloned across all states.
        /// </param>
        public HiddenMarkovModel(ITopology topology, TDistribution emissions)
            : base(topology)
        {
            if (emissions == null)
            {
                throw new ArgumentNullException("emissions");
            }

            // Initialize B using the initial distribution
            Emissions = new TDistribution[States];

            for (var i = 0; i < Emissions.Length; i++)
            {
                Emissions[i] = (TDistribution)emissions.Clone();
            }


            if (emissions is IMultivariateDistribution)
            {
                multivariate = true;
                Dimension = ((IMultivariateDistribution)Emissions[0]).Dimension;
            }
        }

        /// <summary>
        ///     Constructs a new Hidden Markov Model with arbitrary-density state probabilities.
        /// </summary>
        /// <param name="topology">
        ///     A <see cref="Topology" /> object specifying the initial values of the matrix of transition
        ///     probabilities <c>A</c> and initial state probabilities <c>pi</c> to be used by this model.
        /// </param>
        /// <param name="emissions">
        ///     The initial emission probability distributions for each state.
        /// </param>
        public HiddenMarkovModel(ITopology topology, TDistribution[] emissions)
            : base(topology)
        {
            if (emissions == null)
            {
                throw new ArgumentNullException("emissions");
            }

            if (emissions.Length != States)
            {
                throw new ArgumentException(
                    "The emission matrix should have the same number of rows as the number of states in the model.",
                    "emissions");
            }

            Emissions = emissions;

            // Assume all emissions have same form
            if (Emissions[0] is IMultivariateDistribution)
            {
                multivariate = true;
                Dimension = ((IMultivariateDistribution)Emissions[0]).Dimension;
            }
        }

        /// <summary>
        ///     Constructs a new Hidden Markov Model with arbitrary-density state probabilities.
        /// </summary>
        /// <param name="transitions">The transitions matrix A for this model.</param>
        /// <param name="emissions">The emissions matrix B for this model.</param>
        /// <param name="probabilities">The initial state probabilities for this model.</param>
        /// <param name="logarithm">
        ///     Set to true if the matrices are given with logarithms of the
        ///     intended probabilities; set to false otherwise. Default is false.
        /// </param>
        public HiddenMarkovModel(double[,] transitions, TDistribution[] emissions, double[] probabilities,
            bool logarithm = false)
            : this(new Custom(transitions, probabilities, logarithm), emissions)
        {
        }

        /// <summary>
        ///     Constructs a new Hidden Markov Model with arbitrary-density state probabilities.
        /// </summary>
        /// <param name="states">The number of states for the model.</param>
        /// <param name="emissions">A initial distribution to be copied to all states in the model.</param>
        public HiddenMarkovModel(int states, TDistribution emissions)
            : this(new Ergodic(states), emissions)
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the number of dimensions in the
        ///     probability distributions for the states.
        /// </summary>
        public int Dimension { get; } = 1;

        /// <summary>
        ///     Gets the Emission matrix (B) for this model.
        /// </summary>
        public TDistribution[] Emissions { get; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Calculates the most likely sequence of hidden states
        ///     that produced the given observation sequence.
        /// </summary>
        /// <remarks>
        ///     Decoding problem. Given the HMM M = (A, B, pi) and  the observation sequence
        ///     O = {o1,o2, ..., oK}, calculate the most likely sequence of hidden states Si
        ///     that produced this observation sequence O. This can be computed efficiently
        ///     using the Viterbi algorithm.
        /// </remarks>
        /// <param name="observations">A sequence of observations.</param>
        /// <returns>The sequence of states that most likely produced the sequence.</returns>
        public int[] Decode(Array observations)
        {
            return Decode(observations, out _);
        }

        /// <summary>
        ///     Calculates the most likely sequence of hidden states
        ///     that produced the given observation sequence.
        /// </summary>
        /// <remarks>
        ///     Decoding problem. Given the HMM M = (A, B, pi) and  the observation sequence
        ///     O = {o1,o2, ..., oK}, calculate the most likely sequence of hidden states Si
        ///     that produced this observation sequence O. This can be computed efficiently
        ///     using the Viterbi algorithm.
        /// </remarks>
        /// <param name="observations">A sequence of observations.</param>
        /// <param name="logLikelihood">The log-likelihood along the most likely sequence.</param>
        /// <returns>The sequence of states that most likely produced the sequence.</returns>
        public int[] Decode(Array observations, out double logLikelihood)
        {
            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }

            if (observations.Length == 0)
            {
                logLikelihood = double.NegativeInfinity;
                return new int[0];
            }

            // Argument check
            var x = MarkovHelperMethods.checkAndConvert(observations, Dimension);

            return viterbi(x, out logLikelihood);
        }

        private int[] viterbi(double[][] x, out double logLikelihood)
        {
            // Viterbi-forward algorithm.
            var T = x.Length;
            var states = States;
            int maxState;
            double maxWeight;
            double weight;

            var logPi = Probabilities;
            var logA = Transitions;

            var s = new int[states, T];
            var lnFwd = new double[states, T];


            // Base
            for (var i = 0; i < states; i++)
            {
                lnFwd[i, 0] = logPi[i] + Emissions[i].LogProbabilityFunction(x[0]);
            }

            // Induction
            for (var t = 1; t < T; t++)
            {
                var observation = x[t];

                for (var j = 0; j < states; j++)
                {
                    maxState = 0;
                    maxWeight = lnFwd[0, t - 1] + logA[0, j];

                    for (var i = 1; i < states; i++)
                    {
                        weight = lnFwd[i, t - 1] + logA[i, j];

                        if (weight > maxWeight)
                        {
                            maxState = i;
                            maxWeight = weight;
                        }
                    }

                    lnFwd[j, t] = maxWeight + Emissions[j].LogProbabilityFunction(observation);
                    s[j, t] = maxState;
                }
            }

            // Find maximum value for time T-1
            maxState = 0;
            maxWeight = lnFwd[0, T - 1];

            for (var i = 1; i < states; i++)
            {
                if (lnFwd[i, T - 1] > maxWeight)
                {
                    maxState = i;
                    maxWeight = lnFwd[i, T - 1];
                }
            }


            // Trackback
            var path = new int[T];
            path[T - 1] = maxState;

            for (var t = T - 2; t >= 0; t--)
            {
                path[t] = s[path[t + 1], t + 1];
            }


            // Returns the sequence probability as an out parameter
            logLikelihood = maxWeight;

            // Returns the most likely (Viterbi path) for the given sequence
            return path;
        }

        /// <summary>
        ///     Calculates the probability of each hidden state for each
        ///     observation in the observation vector.
        /// </summary>
        /// <remarks>
        ///     If there are 3 states in the model, and the <paramref name="observations" />
        ///     array contains 5 elements, the resulting vector will contain 5 vectors of
        ///     size 3 each. Each vector of size 3 will contain probability values that sum
        ///     up to one. By following those probabilities in order, we may decode those
        ///     probabilities into a sequence of most likely states. However, the sequence
        ///     of obtained states may not be valid in the model.
        /// </remarks>
        /// <param name="observations">A sequence of observations.</param>
        /// <returns>
        ///     A vector of the same size as the observation vectors, containing
        ///     the probabilities for each state in the model for the current observation.
        ///     If there are 3 states in the model, and the <paramref name="observations" />
        ///     array contains 5 elements, the resulting vector will contain 5 vectors of
        ///     size 3 each. Each vector of size 3 will contain probability values that sum
        ///     up to one.
        /// </returns>
        public double[][] Posterior(Array observations)
        {
            // Reference: C. S. Foo, CS262 Winter 2007, Lecture 5, Stanford
            // http://ai.stanford.edu/~serafim/CS262_2007/notes/lecture5.pdf

            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }

            var x = MarkovHelperMethods.checkAndConvert(observations, Dimension);

            double logLikelihood;

            // Compute forward and backward probabilities
            var lnFwd = ForwardBackwardAlgorithm.LogForward(this, x, out logLikelihood);
            var lnBwd = ForwardBackwardAlgorithm.LogBackward(this, x);

            var probabilities = new double[observations.Length][];

            for (var i = 0; i < probabilities.Length; i++)
            {
                var states = probabilities[i] = new double[States];

                for (var j = 0; j < states.Length; j++)
                {
                    states[j] = System.Math.Exp(lnFwd[i, j] + lnBwd[i, j] - logLikelihood);
                }
            }

            return probabilities;
        }

        /// <summary>
        ///     Calculates the probability of each hidden state for each observation
        ///     in the observation vector, and uses those probabilities to decode the
        ///     most likely sequence of states for each observation in the sequence
        ///     using the posterior decoding method. See remarks for details.
        /// </summary>
        /// <remarks>
        ///     If there are 3 states in the model, and the <paramref name="observations" />
        ///     array contains 5 elements, the resulting vector will contain 5 vectors of
        ///     size 3 each. Each vector of size 3 will contain probability values that sum
        ///     up to one. By following those probabilities in order, we may decode those
        ///     probabilities into a sequence of most likely states. However, the sequence
        ///     of obtained states may not be valid in the model.
        /// </remarks>
        /// <param name="observations">A sequence of observations.</param>
        /// <param name="path">
        ///     The sequence of states most likely associated with each
        ///     observation, estimated using the posterior decoding method.
        /// </param>
        /// <returns>
        ///     A vector of the same size as the observation vectors, containing
        ///     the probabilities for each state in the model for the current observation.
        ///     If there are 3 states in the model, and the <paramref name="observations" />
        ///     array contains 5 elements, the resulting vector will contain 5 vectors of
        ///     size 3 each. Each vector of size 3 will contain probability values that sum
        ///     up to one.
        /// </returns>
        public double[][] Posterior(Array observations, out int[] path)
        {
            var probabilities = Posterior(observations);

            path = new int[observations.Length];
            for (var i = 0; i < path.Length; i++)
            {
                probabilities[i].Max(out path[i]);
            }

            return probabilities;
        }

        /// <summary>
        ///     Calculates the likelihood that this model has generated the given sequence.
        /// </summary>
        /// <remarks>
        ///     Evaluation problem. Given the HMM  M = (A, B, pi) and  the observation
        ///     sequence O = {o1, o2, ..., oK}, calculate the probability that model
        ///     M has generated sequence O. This can be computed efficiently using the
        ///     either the Viterbi or the Forward algorithms.
        /// </remarks>
        /// <param name="observations">
        ///     A sequence of observations.
        /// </param>
        /// <returns>
        ///     The log-likelihood that the given sequence has been generated by this model.
        /// </returns>
        public double Evaluate(Array observations)
        {
            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }

            if (observations.Length == 0)
            {
                return double.NegativeInfinity;
            }


            var x = MarkovHelperMethods.checkAndConvert(observations, Dimension);


            // Forward algorithm
            double logLikelihood;

            // Compute forward probabilities
            ForwardBackwardAlgorithm.LogForward(this, x, out logLikelihood);

            // Return the sequence probability
            return logLikelihood;
        }

        /// <summary>
        ///     Calculates the log-likelihood that this model has generated the
        ///     given observation sequence along the given state path.
        /// </summary>
        /// <param name="observations">A sequence of observations. </param>
        /// <param name="path">A sequence of states. </param>
        /// <returns>
        ///     The log-likelihood that the given sequence of observations has
        ///     been generated by this model along the given sequence of states.
        /// </returns>
        public double Evaluate(Array observations, int[] path)
        {
            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (observations.Length == 0)
            {
                return double.NegativeInfinity;
            }


            var x = MarkovHelperMethods.checkAndConvert(observations, Dimension);

            try
            {
                var logLikelihood = Probabilities[path[0]]
                                    + Emissions[path[0]].LogProbabilityFunction(x[0]);

                for (var i = 1; i < observations.Length; i++)
                {
                    var a = Transitions[path[i - 1], path[i]];
                    var b = Emissions[path[i]].LogProbabilityFunction(x[i]);
                    logLikelihood += a + b;
                }

                // Return the sequence probability
                return logLikelihood;
            }
            catch (IndexOutOfRangeException ex)
            {
                checkHiddenStates(ex, path);
                throw;
            }
        }

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// <param name="observations">
        ///     A sequence of observations. Predictions will be made regarding
        ///     the next observations that should be coming after the last observation in this sequence.
        /// </param>
        public double[] Predict(double[][] observations)
        {
            if (!multivariate)
            {
                throw new ArgumentException("Model is univariate.", "observations");
            }

            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }

            return Predict(observations, out _);
        }

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// <param name="observations">
        ///     A sequence of observations. Predictions will be made regarding
        ///     the next observation that should be coming after the last observation in this sequence.
        /// </param>
        public double Predict(double[] observations)
        {
            if (multivariate)
            {
                throw new ArgumentException("Model is multivariate.", "observations");
            }

            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }


            return Predict(observations, out _);
        }

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// <param name="observations">
        ///     A sequence of observations. Predictions will be made regarding
        ///     the next observations that should be coming after the last observation in this sequence.
        /// </param>
        /// <param name="logLikelihood">
        ///     The log-likelihood of the given sequence, plus the predicted
        ///     next observation. Exponentiate this value (use the System.Math.Exp function) to obtain
        ///     a <c>likelihood</c> value.
        /// </param>
        public double[] Predict(double[][] observations, out double logLikelihood)
        {
            if (!multivariate)
            {
                throw new ArgumentException("Model is univariate.", "observations");
            }

            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }


            // Compute the next observation (currently only one ahead is supported).
            var prediction = predict(observations, 1, out logLikelihood, out _);

            return prediction[0];
        }

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// <param name="observations">
        ///     A sequence of observations. Predictions will be made regarding
        ///     the next observations that should be coming after the last observation in this sequence.
        /// </param>
        /// <param name="logLikelihood">
        ///     The log-likelihood of the given sequence, plus the predicted
        ///     next observation. Exponentiate this value (use the System.Math.Exp function) to obtain
        ///     a <c>likelihood</c> value.
        /// </param>
        public double Predict(double[] observations, out double logLikelihood)
        {
            if (multivariate)
            {
                throw new ArgumentException("Model is multivariate.", "observations");
            }

            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }


            // Convert to multivariate observations
            var obs = MarkovHelperMethods.convertNoCheck(observations, Dimension);

            // Compute the next observation (currently only one ahead is supported).
            var prediction = predict(obs, 1, out logLikelihood, out _);

            return prediction[0][0];
        }

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// <param name="observations">
        ///     A sequence of observations. Predictions will be made regarding
        ///     the next observations that should be coming after the last observation in this sequence.
        /// </param>
        /// <param name="logLikelihood">
        ///     The log-likelihood of the given sequence, plus the predicted
        ///     next observation. Exponentiate this value (use the System.Math.Exp function) to obtain
        ///     a <c>likelihood</c> value.
        /// </param>
        /// <param name="probabilities">
        ///     The continuous probability distribution describing the next observations
        ///     that are likely to be generated. Taking the mode of this distribution might give the most likely
        ///     next value in the observed sequence.
        /// </param>
        public double[] Predict<TMultivariate>(double[][] observations,
            out double logLikelihood, out MultivariateMixture<TMultivariate> probabilities)
            where TMultivariate : DistributionBase, TDistribution, IMultivariateDistribution<double[]>
        {
            if (!multivariate)
            {
                throw new ArgumentException("Model is univariate.", "observations");
            }

            // Compute the next observation (currently only one ahead is supported)
            var prediction = predict(observations, out logLikelihood, out probabilities);

            return prediction[0];
        }

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// <param name="observations">
        ///     A sequence of observations. Predictions will be made regarding
        ///     the next observations that should be coming after the last observation in this sequence.
        /// </param>
        /// <param name="probabilities">
        ///     The continuous probability distribution describing the next observations
        ///     that are likely to be generated. Taking the mode of this distribution might give the most likely
        ///     next value in the observed sequence.
        /// </param>
        public double[] Predict<TMultivariate>(double[][] observations,
            out MultivariateMixture<TMultivariate> probabilities)
            where TMultivariate : DistributionBase, TDistribution, IMultivariateDistribution<double[]>
        {
            if (!multivariate)
            {
                throw new ArgumentException("Model is univariate.", "observations");
            }

            // Compute the next observation (currently only one ahead is supported)
            var prediction = predict(observations, out _, out probabilities);

            return prediction[0];
        }

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// <param name="observations">
        ///     A sequence of observations. Predictions will be made regarding
        ///     the next observations that should be coming after the last observation in this sequence.
        /// </param>
        /// <param name="probabilities">
        ///     The continuous probability distribution describing the next observations
        ///     that are likely to be generated. Taking the mode of this distribution might give the most likely
        ///     next value in the observed sequence.
        /// </param>
        public double Predict<TUnivariate>(double[] observations, out Mixture<TUnivariate> probabilities)
            where TUnivariate : DistributionBase, TDistribution, IUnivariateDistribution<double>
        {
            if (multivariate)
            {
                throw new ArgumentException("Model is multivariate.", "observations");
            }

            // Compute the next observation (as if it were multidimensional)
            var prediction = predict(observations, out _, out probabilities);

            // Return the first (single) dimension of the next observation.
            return prediction[0];
        }

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// <param name="observations">
        ///     A sequence of observations. Predictions will be made regarding
        ///     the next observations that should be coming after the last observation in this sequence.
        /// </param>
        /// <param name="logLikelihood">
        ///     The log-likelihood of the given sequence, plus the predicted
        ///     next observation. Exponentiate this value (use the System.Math.Exp function) to obtain
        ///     a <c>likelihood</c> value.
        /// </param>
        /// <param name="probabilities">
        ///     The continuous probability distribution describing the next observations
        ///     that are likely to be generated. Taking the mode of this distribution might give the most likely
        ///     next value in the observed sequence.
        /// </param>
        public double Predict<TUnivariate>(double[] observations,
            out double logLikelihood, out Mixture<TUnivariate> probabilities)
            where TUnivariate : DistributionBase, TDistribution, IUnivariateDistribution<double>
        {
            if (multivariate)
            {
                throw new ArgumentException("Model is multivariate.", "observations");
            }

            // Compute the next observation (as if it were multidimensional)
            var prediction = predict(observations, out logLikelihood, out probabilities);

            // Return the first (single) dimension of the next observation.
            return prediction[0];
        }

        /// <summary>
        ///     Predicts the next observations occurring after a given observation sequence.
        /// </summary>
        /// <param name="observations">
        ///     A sequence of observations. Predictions will be made regarding
        ///     the next observations that should be coming after the last observation in this sequence.
        /// </param>
        /// <param name="next">The number of observations to be predicted. Default is 1.</param>
        /// <param name="logLikelihood">
        ///     The log-likelihood of the given sequence, plus the predicted
        ///     next observation. Exponentiate this value (use the System.Math.Exp function) to obtain
        ///     a <c>likelihood</c> value.
        /// </param>
        public double[][] Predict(double[][] observations, int next, out double logLikelihood)
        {
            if (!multivariate)
            {
                throw new ArgumentException("Model is univariate.", "observations");
            }

            // Compute the next observations
            var prediction = predict(observations, next, out logLikelihood, out _);

            return prediction;
        }

        /// <summary>
        ///     Predicts the next observations occurring after a given observation sequence.
        /// </summary>
        /// <param name="observations">
        ///     A sequence of observations. Predictions will be made regarding
        ///     the next observations that should be coming after the last observation in this sequence.
        /// </param>
        /// <param name="next">The number of observations to be predicted. Default is 1.</param>
        /// <param name="logLikelihood">
        ///     The log-likelihood of the given sequence, plus the predicted
        ///     next observations. Exponentiate this value (use the System.Math.Exp function) to obtain
        ///     a <c>likelihood</c> value.
        /// </param>
        public double[] Predict(double[] observations, int next, out double logLikelihood)
        {
            if (multivariate)
            {
                throw new ArgumentException("Model is multivariate.", "observations");
            }

            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }


            // Convert to multivariate observations
            var obs = MarkovHelperMethods.convertNoCheck(observations, Dimension);

            // Compute the next observations
            var prediction = predict(obs, next, out logLikelihood, out _);

            // Return the first (single) dimension of the next observations.
            return prediction.Concatenate();
        }

        /// <summary>
        ///     Generates a random vector of observations from the model.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <returns>A random vector of observations drawn from the model.</returns>
        public Array Generate(int samples)
        {
            return Generate(samples, out var _, out var _);
        }

        /// <summary>
        ///     Generates a random vector of observations from the model.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="logLikelihood">The log-likelihood of the generated observation sequence.</param>
        /// <param name="path">The Viterbi path of the generated observation sequence.</param>
        /// <returns>A random vector of observations drawn from the model.</returns>
        public Array Generate(int samples, out int[] path, out double logLikelihood)
        {
            var transitions = Probabilities;

            logLikelihood = 0; // log(1)
            path = new int[samples];

            var multivariate = Emissions as ISampleableDistribution<double[]>[];
            if (multivariate != null)
            {
                var observations = new double[samples][];

                // For each observation to be generated
                for (var t = 0; t < observations.Length; t++)
                {
                    // Navigate randomly on one of the state transitions
                    var state = GeneralDiscreteDistribution.Random(transitions.Exp());

                    // Generate a sample for the state
                    var symbol = multivariate[state].Generate();

                    // Store the sample
                    observations[t] = symbol;
                    path[t] = state;

                    // Compute log-likelihood up to this point
                    logLikelihood += transitions[state] +
                                     Emissions[state].LogProbabilityFunction(symbol);

                    // Continue sampling
                    transitions = Transitions.GetRow(state);
                }

                return observations;
            }

            var univariate = Emissions as ISampleableDistribution<double>[];
            if (univariate != null)
            {
                var observations = new double[samples];

                // For each observation to be generated
                for (var t = 0; t < observations.Length; t++)
                {
                    // Navigate randomly on one of the state transitions
                    var state = GeneralDiscreteDistribution.Random(transitions.Exp());

                    // Generate a sample for the state
                    var symbol = univariate[state].Generate();

                    // Store the sample
                    observations[t] = symbol;
                    path[t] = state;

                    // Compute log-likelihood up to this point
                    logLikelihood += transitions[state] +
                                     Emissions[state].LogProbabilityFunction(symbol);

                    // Continue sampling
                    transitions = Transitions.GetRow(state);
                }

                return observations;
            }

            throw new ArgumentException("The model's emission distributions do not support sampling.");
        }

        #endregion

        //---------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        private double[][] predict<TMultivariate>(double[][] observations,
            out double logLikelihood, out MultivariateMixture<TMultivariate> probabilities)
            where TMultivariate : DistributionBase, TDistribution, IMultivariateDistribution<double[]>
        {
            // Matrix to store the probabilities in assuming the next
            // observations (prediction) will belong to each state.
            double[][] weights;

            // Compute the next observation (currently only one ahead is supported).
            var prediction = predict(observations, 1, out logLikelihood, out weights);

            // Create the mixture distribution defining the model likelihood in
            // assuming the next observation belongs will belong to each state.
            var b = Emissions.Apply(x => (TMultivariate)x);
            probabilities = new MultivariateMixture<TMultivariate>(weights[1].Exp(), b);

            return prediction;
        }

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        private double[] predict<TUnivariate>(double[] observations,
            out double logLikelihood, out Mixture<TUnivariate> probabilities)
            where TUnivariate : DistributionBase, TDistribution, IUnivariateDistribution<double>
        {
            // Convert to multivariate observations
            var obs = MarkovHelperMethods.convertNoCheck(observations, Dimension);

            // Matrix to store the probabilities in assuming the next
            // observations (prediction) will belong to each state.
            double[][] weights;

            // Compute the next observation (currently only one ahead is supported).
            var prediction = predict(obs, 1, out logLikelihood, out weights);

            // Create the mixture distribution defining the model likelihood in
            // assuming the next observation belongs will belong to each state.
            var b = Emissions.Apply(x => (TUnivariate)x);
            probabilities = new Mixture<TUnivariate>(weights[1].Exp(), b);

            return prediction[0];
        }

        /// <summary>
        ///     Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        private double[][] predict(double[][] observations, int next,
            out double logLikelihood, out double[][] lnFuture)
        {
            var states = States;
            var T = next;

            var lnA = Transitions;

            var prediction = new double[next][];
            var expectation = new double[states][];

            // Compute expectations for each state
            for (var i = 0; i < states; i++)
            {
                expectation[i] = getMode(Emissions[i]);
            }


            // Compute forward probabilities for the given observation sequence.
            var lnFw0 = ForwardBackwardAlgorithm.LogForward(this, observations, out logLikelihood);

            // Create a matrix to store the future probabilities for the prediction
            // sequence and copy the latest forward probabilities on its first row.
            var lnFwd = new double[T + 1][];
            for (var i = 0; i < lnFwd.Length; i++)
            {
                lnFwd[i] = new double[States];
            }


            // 1. Initialization
            for (var i = 0; i < States; i++)
            {
                lnFwd[0][i] = lnFw0[observations.Length - 1, i];
            }


            // 2. Induction
            for (var t = 0; t < T; t++)
            {
                var weights = lnFwd[t + 1];

                for (var i = 0; i < weights.Length; i++)
                {
                    var sum = double.NegativeInfinity;
                    for (var j = 0; j < states; j++)
                    {
                        sum = Special.LogSum(sum, lnFwd[t][j] + lnA[j, i]);
                    }

                    weights[i] = sum + Emissions[i].LogProbabilityFunction(expectation[i]);
                }

                var sumWeight = double.NegativeInfinity;
                for (var i = 0; i < weights.Length; i++)
                {
                    sumWeight = Special.LogSum(sumWeight, weights[i]);
                }

                for (var i = 0; i < weights.Length; i++)
                {
                    weights[i] -= sumWeight;
                }

                // Select most probable value
                var maxWeight = weights[0];
                prediction[t] = expectation[0];
                for (var i = 1; i < states; i++)
                {
                    if (weights[i] > maxWeight)
                    {
                        maxWeight = weights[i];
                        prediction[t] = expectation[i];
                    }
                }

                // Recompute log-likelihood
                logLikelihood = maxWeight;
            }

            // Returns the future-forward probabilities
            lnFuture = lnFwd;

            return prediction;
        }

        private static double[] getMode(TDistribution dist)
        {
            var uni = dist as IUnivariateDistribution;
            if (uni != null)
            {
                return new[] { uni.Mode };
            }

            var multi = dist as IMultivariateDistribution;
            return multi.Mode;
        }

        private void checkHiddenStates(IndexOutOfRangeException ex, int[] path)
        {
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] < 0 || path[i] >= States)
                {
                    throw new ArgumentException("The hidden states vector must "
                                                + "only contain values higher than or equal to 0 and less than " +
                                                States
                                                + ". The value at the position " + i + " is " + path[i] + ".", "path",
                        ex);
                }
            }
        }

        #endregion
    }
}