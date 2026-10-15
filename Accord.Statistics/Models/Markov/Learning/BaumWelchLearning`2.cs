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
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Models.Markov.Learning
{
    /// <summary>
    ///     Baum-Welch learning algorithm for
    ///     <see cref="HiddenMarkovModel{TDistribution}">
    ///         arbitrary-density (generic) Hidden Markov Models
    ///     </see>
    ///     .
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Baum-Welch algorithm is an <see cref="IUnsupervisedLearning">unsupervised algorithm</see>
    ///         used to learn a single hidden Markov model object from a set of observation sequences. It works
    ///         by using a variant of the
    ///         <see cref="Mixture{T}.Fit(double[], double[], MixtureOptions)">
    ///             Expectation-Maximization
    ///         </see>
    ///         algorithm to search a set of model parameters (i.e. the matrix
    ///         of
    ///         <see cref="IHiddenMarkovModel.Transitions">
    ///             transition probabilities <c>A</c>
    ///         </see>
    ///         , the vector of
    ///         <see cref="HiddenMarkovModel{T}.Emissions">
    ///             state probability distributions
    ///             <c>B</c>
    ///         </see>
    ///         , and the
    ///         <see cref="IHiddenMarkovModel.Probabilities">
    ///             initial probability
    ///             vector <c>π</c>
    ///         </see>
    ///         ) that would result in a model having a high likelihood of being able
    ///         to <see cref="HiddenMarkovModel{TDistribution}.Generate(int)">generate</see> a set of training
    ///         sequences given to this algorithm.
    ///     </para>
    ///     <para>
    ///         For increased accuracy, this class performs all computations using log-probabilities.
    ///     </para>
    ///     <para>
    ///         For a more thorough explanation on <see cref="HiddenMarkovModel">hidden Markov models</see>
    ///         with practical examples on gesture recognition, please see
    ///         <a href="http://www.codeproject.com/Articles/541428/Sequence-Classifiers-in-Csharp-Part-I-Hidden-Marko">
    ///             Sequence Classifiers in C#, Part I: Hidden Markov Models
    ///         </a>
    ///         [1].
    ///     </para>
    ///     <para>
    ///         [1]:
    ///         <a href="http://www.codeproject.com/Articles/541428/Sequence-Classifiers-in-Csharp-Part-I-Hidden-Marko">
    ///             http://www.codeproject.com/Articles/541428/Sequence-Classifiers-in-Csharp-Part-I-Hidden-Marko
    ///         </a>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         In the following example, we will create a Continuous Hidden Markov Model using
    ///         a univariate Normal distribution to model properly model continuous sequences.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\HiddenMarkovModel`2Test.cs" region="doc_learn2" />
    ///     <para>
    ///         In the following example, we will create a Discrete Hidden Markov Model
    ///         using a Generic Discrete Probability Distribution to reproduce the same
    ///         code example given in <seealso cref="BaumWelchLearning" /> documentation.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\HiddenMarkovModel`2Test.cs" region="doc_learn" />
    ///     <para>
    ///         The next example shows how to create a multivariate model using
    ///         a multivariate normal distribution. In this example, sequences
    ///         contain vector-valued observations, such as in the case of (x,y)
    ///         pairs.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\HiddenMarkovModel`2Test.cs"
    ///         region="doc_learn_multivariate" />
    ///     <para>
    ///         The following example shows how to create a hidden Markov model
    ///         that considers each feature to be independent of each other. This
    ///         is the same as following Bayes' assumption of independence for each
    ///         feature in the feature vector.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\HiddenMarkovModel`2Test.cs"
    ///         region="doc_learn_independent" />
    ///     <para>
    ///         Finally, the last example shows how to fit a mixture-density
    ///         hidden Markov models.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\HiddenMarkovModel`2Test.cs"
    ///         region="doc_learn_mixture" />
    ///     <para>
    ///         When using Normal distributions, it is often the case we might find problems
    ///         which are difficult to solve. Some problems may include constant variables or
    ///         other numerical difficulties preventing a the proper estimation of a Normal
    ///         distribution from the data.
    ///     </para>
    ///     <para>
    ///         A sign of those difficulties arises when the learning algorithm throws the exception
    ///         <c>"Variance is zero. Try specifying a regularization constant in the fitting options"</c>
    ///         for univariate distributions (e.g. <see cref="NormalDistribution" /> or a
    ///         <see
    ///             cref="NonPositiveDefiniteMatrixException" />
    ///         informing that the
    ///         <c>
    ///             "Covariance matrix
    ///             is not positive definite. Try specifying a regularization constant in the fitting options"
    ///         </c>
    ///         for multivariate distributions like the <see cref="MultivariateNormalDistribution" />.
    ///         In both cases, this is an indication that the variables being learned can not be suitably
    ///         modeled by Normal distributions. To avoid numerical difficulties when estimating those
    ///         probabilities, a small regularization constant can be added to the variances or to the
    ///         covariance matrices until they become greater than zero or positive definite.
    ///     </para>
    ///     <para>
    ///         To specify a regularization constant as given in the above message, we
    ///         can indicate a fitting options object for the model distribution using:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\HiddenMarkovModel`2Test.cs"
    ///         region="doc_learn_mixture_regularization" />
    ///     <para>
    ///         Typically, any small value would suffice as a regularization constant,
    ///         though smaller values may lead to longer fitting times. Too high values,
    ///         on the other hand, would lead to decreased accuracy.
    ///     </para>
    /// </example>
    /// <seealso cref="HiddenMarkovModel" />
    /// <seealso cref="HiddenMarkovModel{TDistribution, TObservation}" />
    /// <seealso cref="BaumWelchLearning" />
    /// <seealso cref="BaumWelchLearning{TDistribution, TObservation, TOptions}" />
    /// <typeparam name="TDistribution">The type of the emission distributions in the model.</typeparam>
    /// <typeparam name="TObservation">The type of the observations (i.e. int for a discrete model).</typeparam>
    public class BaumWelchLearning<TDistribution, TObservation> :
        BaseBaumWelchLearning<HiddenMarkovModel<TDistribution, TObservation>, TDistribution, TObservation,
            IFittingOptions>,
        IConvergenceLearning
        where TDistribution : IFittableDistribution<TObservation>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BaumWelchLearning{TDistribution, TObservation}" /> class.
        /// </summary>
        /// <param name="model">The model to be learned.</param>
        public BaumWelchLearning(HiddenMarkovModel<TDistribution, TObservation> model)
            : base(model)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaumWelchLearning{TDistribution, TObservation}" /> class.
        /// </summary>
        public BaumWelchLearning()
        {
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors of this abstract
        ///     class must define this method so new models can be created from the training data.
        /// </summary>
        protected override HiddenMarkovModel<TDistribution, TObservation> Create(TObservation[][] x)
        {
            if (Topology == null)
            {
                throw new InvalidOperationException(
                    "Please set the Topology property before trying to learn a new hidden Markov model.");
            }

            if (Emissions == null)
            {
                throw new InvalidOperationException(
                    "Please set the Emissions property before trying to learn a new hidden Markov model.");
            }

            return new HiddenMarkovModel<TDistribution, TObservation>(Topology, Emissions);
        }
    }
}