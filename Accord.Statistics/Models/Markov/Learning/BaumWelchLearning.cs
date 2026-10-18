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
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Models.Markov.Learning
{
#pragma warning disable 612, 618
    /// <summary>
    ///     Baum-Welch learning algorithm for
    ///     <see cref="HiddenMarkovModel">
    ///         discrete-density Hidden Markov Models
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
    ///         of <see cref="IHiddenMarkovModel.Transitions">transition probabilities A</see>, the matrix
    ///         of <see cref="HiddenMarkovModel.Emissions">emission probabilities B</see>, and the
    ///         <see cref="IHiddenMarkovModel.Probabilities">initial probability vector π</see>) that
    ///         would result in a model having a high likelihood of being able
    ///         to <see cref="HiddenMarkovModel{TDistribution, TObservation}.Generate(int)">generate</see> a set of training
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
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\HiddenMarkovModelTest.cs" region="doc_learn" />
    /// </example>
    /// <seealso cref="HiddenMarkovModel" />
    /// <seealso cref="HiddenMarkovModel{TDistribution}" />
    /// <seealso cref="BaumWelchLearning{TDistribution}" />
    public class BaumWelchLearning : BaseBaumWelchLearningOptions<HiddenMarkovModel, GeneralDiscreteDistribution, int,
        GeneralDiscreteOptions>
    {
        /// <summary>
        ///     Creates a new instance of the Baum-Welch learning algorithm.
        /// </summary>
        public BaumWelchLearning()
        {
        }

        /// <summary>
        ///     Creates a new instance of the Baum-Welch learning algorithm.
        /// </summary>
        public BaumWelchLearning(HiddenMarkovModel model)
            : base(model)
        {
            NumberOfSymbols = model.NumberOfSymbols;
        }

        /// <summary>
        ///     Gets or sets the number of symbols that should be used whenever
        ///     this learning algorithm needs to create a new model. This property
        ///     must be set before learning.
        /// </summary>
        /// <value>The number of symbols.</value>
        public int NumberOfSymbols { get; set; }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(int[] observations)
        {
            Learn(new[] { observations });
            return LogLikelihood;
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(int[][] observations)
        {
            Learn(observations);
            return LogLikelihood;
        }

        /// <summary>
        ///     Creates a Baum-Welch with default configurations for
        ///     hidden Markov models with normal mixture densities.
        /// </summary>
        public static BaumWelchLearning<Mixture<NormalDistribution>> FromMixtureModel(
            HiddenMarkovModel<Mixture<NormalDistribution>> model, NormalOptions options)
        {
            var mixOptions = new MixtureOptions
            {
                Iterations = 1,
                InnerOptions = options
                //ParallelOptions = ParallelOptions, // TODO:
            };

            return new BaumWelchLearning<Mixture<NormalDistribution>>(model)
            {
                FittingOptions = mixOptions
            };
        }

        /// <summary>
        ///     Creates a Baum-Welch with default configurations for
        ///     hidden Markov models with normal mixture densities.
        /// </summary>
        public static BaumWelchLearning<MultivariateMixture<MultivariateNormalDistribution>> FromMixtureModel(
            HiddenMarkovModel<MultivariateMixture<MultivariateNormalDistribution>> model, NormalOptions options)
        {
            var mixOptions = new MixtureOptions
            {
                Iterations = 1,
                InnerOptions = options
            };

            return new BaumWelchLearning<MultivariateMixture<MultivariateNormalDistribution>>(model)
            {
                FittingOptions = mixOptions
            };
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors of this abstract
        ///     class must define this method so new models can be created from the training data.
        /// </summary>
        protected override HiddenMarkovModel Create(int[][] x)
        {
            if (Topology == null)
            {
                throw new InvalidOperationException(
                    "Please set the Topology property before trying to learn a new hidden Markov model.");
            }

            NumberOfSymbols = x.Max() + 1;

            return new HiddenMarkovModel(Topology, NumberOfSymbols);
        }

        // TODO: Uncomment the following lines
        ///// <summary>
        /////   Creates a Baum-Welch with default configurations for
        /////   hidden Markov models with normal mixture densities.
        ///// </summary>
        ///// 
        //public static BaumWelchLearning<Mixture<NormalDistribution>> FromMixtureModel(
        //    HiddenMarkovModel<Mixture<NormalDistribution, double>> model, NormalOptions options)
        //{
        //    MixtureOptions mixOptions = new MixtureOptions()
        //    {
        //        Iterations = 1,
        //        InnerOptions = options,
        //        //ParallelOptions = ParallelOptions, // TODO:
        //    };

        //    return new BaumWelchLearning<Mixture<NormalDistribution>>(model)
        //    {
        //        FittingOptions = mixOptions
        //    };
        //}

        ///// <summary>
        /////   Creates a Baum-Welch with default configurations for
        /////   hidden Markov models with normal mixture densities.
        ///// </summary>
        ///// 
        //public static BaumWelchLearning<MultivariateMixture<MultivariateNormalDistribution, double[]>> FromMixtureModel(
        //    HiddenMarkovModel<MultivariateMixture<MultivariateNormalDistribution>> model, NormalOptions options)
        //{
        //    MixtureOptions mixOptions = new MixtureOptions()
        //    {
        //        Iterations = 1,
        //        InnerOptions = options
        //    };

        //    return new BaumWelchLearning<MultivariateMixture<MultivariateNormalDistribution>>(model)
        //    {
        //        FittingOptions = mixOptions
        //    };
        //}
    }
}