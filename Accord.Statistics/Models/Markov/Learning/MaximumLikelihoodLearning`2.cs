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

using Accord.MachineLearning;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Models.Markov.Learning
{
    /// <summary>
    ///     Maximum Likelihood learning algorithm for discrete-density Hidden Markov Models.
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
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\MaximumLikelihoodLearning`1Test.cs"
    ///         region="doc_learn" />
    ///     <para>
    ///         The following example shows how hidden Markov models trained using Maximum Likelihood Learning
    ///         can be used in the context of fraud analysis.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\MaximumLikelihoodLearning`1Test.cs"
    ///         region="doc_learn_fraud_analysis" />
    ///     <para>
    ///         Where the transform function is defined as:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\MaximumLikelihoodLearning`1Test.cs"
    ///         region="doc_learn_fraud_transform" />
    /// </example>
    /// <seealso cref="MaximumLikelihoodLearning" />
    /// <seealso cref="ViterbiLearning{TDistribution}" />
    /// <seealso cref="HiddenMarkovModel{TDistribution}" />
    public class MaximumLikelihoodLearning<TDistribution, TObservation> :
        BaseMaximumLikelihoodLearning<HiddenMarkovModel<TDistribution, TObservation>, TDistribution, TObservation,
            IFittingOptions>,
        ISupervisedLearning<HiddenMarkovModel<TDistribution, TObservation>, TObservation[], int[]>
        where TDistribution : IFittableDistribution<TObservation>
    {
        /// <summary>
        ///     Creates a new instance of the Maximum Likelihood learning algorithm.
        /// </summary>
        public MaximumLikelihoodLearning(HiddenMarkovModel<TDistribution, TObservation> model)
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
        ///     Creates an instance of the model to be learned. Inheritors of this abstract
        ///     class must define this method so new models can be created from the training data.
        /// </summary>
        protected override HiddenMarkovModel<TDistribution, TObservation> Create(TObservation[][] x,
            int numberOfClasses)
        {
            var hmm = new HiddenMarkovModel<TDistribution, TObservation>(numberOfClasses, Emissions);

            MarkovHelperMethods.CheckObservationDimensions(x, hmm);

            return hmm;
        }
    }
}