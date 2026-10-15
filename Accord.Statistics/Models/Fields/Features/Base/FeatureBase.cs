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
using Accord.Statistics.Models.Fields.Functions;

namespace Accord.Statistics.Models.Fields.Features
{
    /// <summary>
    ///     Base implementation for <see cref="ConditionalRandomField{T}">Conditional Random Fields</see>
    ///     <see cref="IFeature{T}">feature functions</see>.
    /// </summary>
    /// <typeparam name="T">The type of the observations being modeled.</typeparam>
    [Serializable]
    public abstract class FeatureBase<T>
    {
        /// <summary>
        ///     Creates a new feature.
        /// </summary>
        /// <param name="owner">The potential function to which this feature belongs.</param>
        /// <param name="factorIndex">The index of the potential factor to which this feature belongs.</param>
        protected FeatureBase(IPotentialFunction<T> owner, int factorIndex)
        {
            Owner = owner;
            FactorIndex = factorIndex;
        }

        /// <summary>
        ///     Gets the potential function containing this feature.
        /// </summary>
        public IPotentialFunction<T> Owner { get; protected set; }

        /// <summary>
        ///     Gets the potential factor to which this feature belongs.
        /// </summary>
        public int FactorIndex { get; private set; }

        /// <summary>
        ///     Computes the feature for the given parameters.
        /// </summary>
        /// <param name="states">The sequence of states.</param>
        /// <param name="observations">The sequence of observations.</param>
        /// <param name="output">The output class label for the sequence.</param>
        /// <returns>The result of the feature.</returns>
        public virtual double Compute(int[] states, T[] observations, int output)
        {
            var z = Compute(-1, states[0], observations, 0, output);
            for (var t = 1; t < observations.Length; t++)
            {
                z += Compute(states[t - 1], states[t], observations, t, output);
            }

            return z;
        }

        /// <summary>
        ///     Computes the feature for the given parameters.
        /// </summary>
        /// <param name="previousState">The previous state.</param>
        /// <param name="currentState">The current state.</param>
        /// <param name="observations">The observations.</param>
        /// <param name="observationIndex">The index of the current observation.</param>
        /// <param name="outputClass">The output class label for the sequence.</param>
        public abstract double Compute(int previousState, int currentState, T[] observations, int observationIndex,
            int outputClass);

        /// <summary>
        ///     Computes the probability of occurrence of this
        ///     feature given a sequence of observations.
        /// </summary>
        /// <param name="fwd">The matrix of forward state probabilities.</param>
        /// <param name="bwd">The matrix of backward state probabilities.</param>
        /// <param name="x">The observation sequence.</param>
        /// <param name="y">The output class label for the sequence.</param>
        /// <returns>The probability of occurrence of this feature.</returns>
        public abstract double Marginal(double[,] fwd, double[,] bwd, T[] x, int y);

        /// <summary>
        ///     Computes the log-probability of occurrence of this
        ///     feature given a sequence of observations.
        /// </summary>
        /// <param name="lnFwd">The matrix of forward state log-probabilities.</param>
        /// <param name="lnBwd">The matrix of backward state log-probabilities.</param>
        /// <param name="x">The observation sequence.</param>
        /// <param name="y">The output class label for the sequence.</param>
        /// <returns>The probability of occurrence of this feature.</returns>
        public abstract double LogMarginal(double[,] lnFwd, double[,] lnBwd, T[] x, int y);
    }
}