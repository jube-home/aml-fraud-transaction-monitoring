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

namespace Accord.Statistics.Models.Markov.Learning
{
    /// <summary>
    ///     Baum-Welch learning algorithms for learning Hidden Markov Models.
    /// </summary>
    /// <typeparam name="TDistribution">The type of the emission distributions in the model.</typeparam>
    /// <typeparam name="TObservation">The type of the observations (i.e. int for a discrete model).</typeparam>
    /// <typeparam name="TOptions">The type of fitting options accepted by this distribution.</typeparam>
    /// <remarks>
    ///     Please see the <see cref="BaumWelchLearning{TDistribution, TObservation}" /> documentation page for
    ///     the actual documentation of this class, including examples.
    /// </remarks>
    public class BaumWelchLearning<TDistribution, TObservation, TOptions> :
        BaseBaumWelchLearningOptions<HiddenMarkovModel<TDistribution, TObservation>, TDistribution, TObservation,
            TOptions>,
        IConvergenceLearning
        where TDistribution : IFittableDistribution<TObservation, TOptions>
        where TOptions : class, IFittingOptions, new()
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BaumWelchLearning{TDistribution, TObservation, TOptions}" /> class.
        /// </summary>
        public BaumWelchLearning()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaumWelchLearning{TDistribution, TObservation, TOptions}" /> class.
        /// </summary>
        /// <param name="model">The model to be learned.</param>
        public BaumWelchLearning(HiddenMarkovModel<TDistribution, TObservation> model)
            : base(model)
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