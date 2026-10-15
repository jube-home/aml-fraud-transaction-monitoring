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

namespace Accord.Statistics.Models.Markov.Learning
{
    /// <summary>
    ///     Common interface for supervised learning algorithms for
    ///     <see cref="HiddenMarkovModel">
    ///         hidden Markov models
    ///     </see>
    ///     such as the
    ///     <see cref="MaximumLikelihoodLearning">
    ///         Maximum Likelihood (MLE)
    ///     </see>
    ///     learning algorithm.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In the context of <see cref="HiddenMarkovModel">hidden Markov models</see>,
    ///         supervised algorithms are algorithms which consider that both the sequence
    ///         of observations and the sequence of states are visible (or known) during
    ///         training. This is in contrast with
    ///         <see cref="IUnsupervisedLearning">
    ///             unsupervised learning algorithms
    ///         </see>
    ///         such as the
    ///         <see cref="BaumWelchLearning">
    ///             Baum-Welch
    ///         </see>
    ///         , which consider that the sequence of states is hidden.
    ///     </para>
    /// </remarks>
    /// <see cref="MaximumLikelihoodLearning" />
    /// <see cref="MaximumLikelihoodLearning{TDistribution}" />
    [Obsolete("Please use Accord.MachineLearning.ISupervisedLearning instead.")]
    public interface ISupervisedLearning
    {
        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        /// <remarks>
        ///     Supervised learning problem. Given some training observation sequences
        ///     O = {o1, o2, ..., oK} and sequence of hidden states H = {h1, h2, ..., hK}
        ///     and general structure of HMM (numbers of hidden and visible states),
        ///     determine HMM parameters M = (A, B, pi) that best fit training data.
        /// </remarks>
        double Run(Array[] observations, int[][] paths);
    }
}