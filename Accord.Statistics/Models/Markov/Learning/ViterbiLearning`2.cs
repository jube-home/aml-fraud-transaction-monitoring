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

using System.Threading;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Models.Markov.Learning
{
    /// <summary>
    ///     Viterbi learning algorithm.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Viterbi learning algorithm is an alternate learning algorithms
    ///         for hidden Markov models. It works by obtaining the Viterbi path
    ///         for the set of training observation sequences and then computing
    ///         the maximum likelihood estimates for the model parameters. Those
    ///         operations are repeated iteratively until model convergence.
    ///     </para>
    ///     <para>
    ///         The Viterbi learning algorithm is also known as the Segmental K-Means
    ///         algorithm.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\ViterbiLearning`1Test.cs" region="doc_learn" />
    /// </example>
    /// <seealso cref="ViterbiLearning" />
    /// <seealso cref="BaumWelchLearning{TDistribution}" />
    public class ViterbiLearning<TDistribution, TObservation> : BaseViterbiLearning<TObservation[]>,
        IUnsupervisedLearning<HiddenMarkovModel<TDistribution, TObservation>, TObservation[], int[]>
        where TDistribution : IFittableDistribution<TObservation>
    {
        private readonly MaximumLikelihoodLearning<TDistribution, TObservation> mle;

        /// <summary>
        ///     Creates a new instance of the Viterbi learning algorithm.
        /// </summary>
        public ViterbiLearning(HiddenMarkovModel<TDistribution, TObservation> model)
        {
            mle = new MaximumLikelihoodLearning<TDistribution, TObservation>(model);
        }

        /// <summary>
        ///     Gets the model being trained.
        /// </summary>
        public HiddenMarkovModel<TDistribution, TObservation> Model => mle.Model;

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
        ///     Gets or sets a cancellation token that can be used
        ///     to cancel the algorithm while it is running.
        /// </summary>
        public override CancellationToken Token
        {
            get => mle.Token;
            set => mle.Token = value;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public HiddenMarkovModel<TDistribution, TObservation> Learn(TObservation[][] x, double[] weights = null)
        {
            var model = mle.Model;
            Run(x);
            return model;
        }

        /// <summary>
        ///     Runs one single epoch (iteration) of the learning algorithm.
        /// </summary>
        /// <param name="inputs">The observation sequences.</param>
        /// <param name="outputs">A vector to be populated with the decoded Viterbi sequences.</param>
        protected override void RunEpoch(TObservation[][] inputs, int[][] outputs)
        {
            var model = mle.Model;

            // Compute the Viterbi path for all sequences
            model.Decide(inputs, outputs);

            // Compute Maximum Likelihood Estimation 
            mle.Learn(inputs, outputs);
        }

        /// <summary>
        ///     Computes the log-likelihood for the current model for the given observations.
        /// </summary>
        /// <param name="observations">The observation vectors.</param>
        /// <returns>The log-likelihood of the observations belonging to the model.</returns>
        protected override double ComputeLogLikelihood(TObservation[][] observations)
        {
            var model = mle.Model;

            var logLikelihood = double.NegativeInfinity;
            for (var i = 0; i < observations.Length; i++)
            {
                logLikelihood = Special.LogSum(logLikelihood, model.LogLikelihood(observations[i]));
            }

            return logLikelihood;
        }
    }
}