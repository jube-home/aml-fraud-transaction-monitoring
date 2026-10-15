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
using Accord.Math;

namespace Accord.Statistics.Models.Markov.Learning
{
    /// <summary>
    ///     Base class for implementations of the Viterbi learning algorithm.
    ///     This class cannot be instantiated.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class uses a template method pattern so specialized classes
    ///         can be written for each kind of hidden Markov model emission density
    ///         (either discrete or continuous).
    ///     </para>
    ///     <para>
    ///         For the actual Viterbi classes, please refer to <see cref="ViterbiLearning" />
    ///         or <see cref="ViterbiLearning{T}" />. For other kinds of algorithms, please
    ///         see <see cref="BaumWelchLearning" /> and <see cref="MaximumLikelihoodLearning" />
    ///         and their generic counter-parts.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ViterbiLearning" />
    /// <seealso cref="ViterbiLearning{T}" />
    public abstract class BaseViterbiLearning<T>
    {
        private readonly RelativeConvergence convergence;

        /// <summary>
        ///     Creates a new instance of the Viterbi learning algorithm.
        /// </summary>
        protected BaseViterbiLearning()
        {
            convergence = new RelativeConvergence();
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        public virtual CancellationToken Token { get; set; }

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
            get => convergence.MaxIterations;
            set => convergence.MaxIterations = value;
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
        ///     Gets the current iteration.
        /// </summary>
        /// <value>The current iteration.</value>
        public int CurrentIteration => convergence.CurrentIteration;

        /// <summary>
        ///     Gets a value indicating whether this instance has converged.
        /// </summary>
        /// <value><c>true</c> if this instance has converged; otherwise, <c>false</c>.</value>
        public bool HasConverged => convergence.HasConverged;

        /// <summary>
        ///     Gets or sets on how many batches the learning data should be divided during learning.
        ///     Batches are used to estimate adequately the first models so they can better compute
        ///     the Viterbi paths for subsequent passes of the algorithm. Default is 1.
        /// </summary>
        public int Batches { get; set; } = 1;

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        /// <remarks>
        ///     Learning problem. Given some training observation sequences O = {o1, o2, ..., oK}
        ///     and general structure of HMM (numbers of hidden and visible states), determine
        ///     HMM parameters M = (A, B, pi) that best fit training data.
        /// </remarks>
        public double Run(params T[] observations)
        {
            convergence.Clear();

            var newLogLikelihood = double.NegativeInfinity;
            var paths = new int[observations.Length][];


            do // Until convergence or max iterations is reached
            {
                if (Token.IsCancellationRequested)
                {
                    return newLogLikelihood;
                }

                if (Batches == 1)
                {
                    RunEpoch(observations, paths);
                }
                else
                {
                    // Divide in batches
                    var groups = Classes.Random(observations.Length, Batches);

                    // For each batch
                    for (var j = 0; j < Batches; j++)
                    {
                        var batch = j;
                        var idx = groups.Find(x => x == batch);
                        var inputs = observations.Get(idx);
                        var outputs = paths.Get(idx);
                        RunEpoch(inputs, outputs);
                    }
                }

                // Compute log-likelihood
                newLogLikelihood = ComputeLogLikelihood(observations);

                // Check convergence
                convergence.NewValue = newLogLikelihood;
            } while (!convergence.HasConverged && !Token.IsCancellationRequested);

            return newLogLikelihood;
        }

        /// <summary>
        ///     Computes the log-likelihood for the current model for the given observations.
        /// </summary>
        /// <param name="observations">The observation vectors.</param>
        /// <returns>The log-likelihood of the observations belonging to the model.</returns>
        protected abstract double ComputeLogLikelihood(T[] observations);

        /// <summary>
        ///     Runs one single epoch (iteration) of the learning algorithm.
        /// </summary>
        /// <param name="inputs">The observation sequences.</param>
        /// <param name="outputs">A vector to be populated with the decoded Viterbi sequences.</param>
        protected abstract void RunEpoch(T[] inputs, int[][] outputs);
    }
}