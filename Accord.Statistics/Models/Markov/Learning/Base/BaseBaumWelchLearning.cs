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

#pragma warning disable 612, 618

namespace Accord.Statistics.Models.Markov.Learning
{
    /// <summary>
    ///     Base class for implementations of the Baum-Welch learning algorithm.
    ///     This class cannot be instantiated.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class uses a template method pattern so specialized classes
    ///         can be written for each kind of hidden Markov model emission density
    ///         (either discrete or continuous). The methods <see cref="UpdateEmissions" />,
    ///         <see cref="ComputeForwardBackward" /> and <see cref="ComputeKsi" /> should
    ///         be overridden by inheriting classes to specify how those probabilities
    ///         should be computed for the density being modeled.
    ///     </para>
    ///     <para>
    ///         For the actual Baum-Welch classes, please refer to <see cref="BaumWelchLearning" />
    ///         or <see cref="BaumWelchLearning{T}" />. For other kinds of algorithms, please
    ///         see <see cref="ViterbiLearning" /> and <see cref="MaximumLikelihoodLearning" />
    ///         and their generic counter-parts.
    ///     </para>
    /// </remarks>
    /// <seealso cref="BaumWelchLearning" />
    /// <seealso cref="BaumWelchLearning{T}" />
    [Obsolete("This class will be removed")]
    public abstract partial class BaseBaumWelchLearning : IConvergenceLearning
    {
        private readonly AbsoluteConvergence convergence;
        private readonly IHiddenMarkovModel model;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseBaumWelchLearning" /> class.
        /// </summary>
        protected BaseBaumWelchLearning(IHiddenMarkovModel model)
        {
            convergence = new AbsoluteConvergence();
            this.model = model;
        }

        /// <summary>
        ///     Gets the Ksi matrix of log probabilities created during
        ///     the last iteration of the Baum-Welch learning algorithm.
        /// </summary>
        public double[][][,] LogKsi { get; protected set; }

        /// <summary>
        ///     Gets the Gamma matrix of log probabilities created during
        ///     the last iteration of the Baum-Welch learning algorithm.
        /// </summary>
        public double[][,] LogGamma { get; protected set; }

        /// <summary>
        ///     Gets the sample weights in the last iteration of the
        ///     Baum-Welch learning algorithm.
        /// </summary>
        public double[] LogWeights { get; protected set; }

        /// <summary>
        ///     Gets or sets the maximum change in the average log-likelihood
        ///     after an iteration of the algorithm used to detect convergence.
        /// </summary>
        /// <remarks>
        ///     This is the likelihood convergence limit L between two iterations of the algorithm. The
        ///     algorithm will stop when the change in the likelihood for two consecutive iterations
        ///     has not changed by more than L percent of the likelihood. If left as zero, the
        ///     algorithm will ignore this parameter and iterate over a number of fixed iterations
        ///     specified by the previous parameter.
        /// </remarks>
        public double Tolerance
        {
            get => convergence.Tolerance;
            set => convergence.Tolerance = value;
        }

        /// <summary>
        ///     Please use MaxIterations instead.
        /// </summary>
        [Obsolete("Please use MaxIterations instead.")]
        public int Iterations
        {
            get => MaxIterations;
            set => MaxIterations = value;
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     performed by the learning algorithm.
        /// </summary>
        /// <remarks>
        ///     This is the maximum number of iterations to be performed by the learning algorithm. If
        ///     specified as zero, the algorithm will learn until convergence of the model average
        ///     likelihood respecting the desired limit.
        /// </remarks>
        public int MaxIterations
        {
            get => convergence.MaxIterations;
            set => convergence.MaxIterations = value;
        }

        /// <summary>
        ///     Gets or sets the number of performed iterations.
        /// </summary>
        public int CurrentIteration => convergence.CurrentIteration;

        /// <summary>
        ///     Gets or sets whether the algorithm has converged.
        /// </summary>
        /// <value><c>true</c> if this instance has converged; otherwise, <c>false</c>.</value>
        public bool HasConverged => convergence.HasConverged;

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
        protected double Run(Array[] observations)
        {
            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }

            if (observations.Length == 0)
            {
                throw new ArgumentException("Observation sequence vector must " +
                                            "contain at least one observation sequence.", "observations");
            }

            for (var i = 0; i < observations.Length; i++)
            {
                if (observations[i].Length == 0)
                {
                    throw new ArgumentException("The observation sequence at position "
                                                + i + " is empty. Observation sequences must contain at least "
                                                + " one observation.", "observations");
                }
            }

            LogWeights = new double[observations.Length];

            return run(observations);
        }

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
        /// <param name="weights">
        ///     The weight associated with each sequence.
        /// </param>
        /// <returns>
        ///     The average log-likelihood for the observations after the model has been trained.
        /// </returns>
        protected double Run(Array[] observations, double[] weights)
        {
            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }

            if (observations.Length == 0)
            {
                throw new ArgumentException(
                    "Observations vector must contain at least one observation", "observations");
            }

            LogWeights = new double[observations.Length];
            for (var i = 0; i < observations.Length; i++)
            {
                LogWeights[i] = System.Math.Log(weights[i]);
            }

            return run(observations);
        }

        private double run(Array[] observations)
        {
            // Baum-Welch algorithm.

            // The Baum–Welch algorithm is a particular case of a generalized expectation-maximization
            // (GEM) algorithm. It can compute maximum likelihood estimates and posterior mode estimates
            // for the parameters (transition and emission probabilities) of an HMM, when given only
            // emissions as training data.

            // The algorithm has two steps:
            //  - Calculating the forward probability and the backward probability for each HMM state;
            //  - On the basis of this, determining the frequency of the transition-emission pair values
            //    and dividing it by the probability of the entire string. This amounts to calculating
            //    the expected count of the particular transition-emission pair. Each time a particular
            //    transition is found, the value of the quotient of the transition divided by the probability
            //    of the entire string goes up, and this value can then be made the new value of the transition.

            return new BaumWelchRun(this, observations).Execute();
        }

        /// <summary>
        ///     Computes the forward and backward probabilities matrices
        ///     for a given observation referenced by its index in the
        ///     input training data.
        /// </summary>
        /// <param name="index">The index of the observation in the input training data.</param>
        /// <param name="lnFwd">Returns the computed forward probabilities matrix.</param>
        /// <param name="lnBwd">Returns the computed backward probabilities matrix.</param>
        protected abstract void ComputeForwardBackward(int index, double[,] lnFwd, double[,] lnBwd);

        /// <summary>
        ///     Computes the ksi matrix of probabilities for a given observation
        ///     referenced by its index in the input training data.
        /// </summary>
        /// <param name="index">The index of the observation in the input training data.</param>
        /// <param name="lnFwd">The matrix of forward probabilities for the observation.</param>
        /// <param name="lnBwd">The matrix of backward probabilities for the observation.</param>
        protected abstract void ComputeKsi(int index, double[,] lnFwd, double[,] lnBwd);

        /// <summary>
        ///     Updates the emission probability matrix.
        /// </summary>
        /// <remarks>
        ///     Implementations of this method should use the observations
        ///     in the training data and the Gamma probability matrix to
        ///     update the probability distributions of symbol emissions.
        /// </remarks>
        protected abstract void UpdateEmissions();
    }
}