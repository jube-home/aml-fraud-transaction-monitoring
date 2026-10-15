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
using Accord.Statistics.Models.Fields.Functions;

namespace Accord.Statistics.Models.Fields.Features
{
    /// <summary>
    ///     Edge feature for Hidden Markov Model state transition probabilities.
    /// </summary>
    [Serializable]
    public sealed class TransitionFeature<T> : FeatureBase<T>, IFeature<T>
    {
        private int current;
        private int previous;

        /// <summary>
        ///     Constructs a state transition feature.
        /// </summary>
        /// <param name="owner">The potential function to which this feature belongs.</param>
        /// <param name="factorIndex">The index of the potential factor to which this feature belongs.</param>
        /// <param name="previous">The originating state.</param>
        /// <param name="current">The destination state.</param>
        public TransitionFeature(IPotentialFunction<T> owner, int factorIndex,
            int previous, int current)
            : base(owner, factorIndex)
        {
            this.previous = previous;
            this.current = current;
        }

        /// <summary>
        ///     Computes the feature for the given parameters.
        /// </summary>
        /// <param name="previousState">The previous state.</param>
        /// <param name="currentState">The current state.</param>
        /// <param name="observations">The observations.</param>
        /// <param name="observationIndex">The index of the current observation.</param>
        /// <param name="outputClass">The output class label for the sequence.</param>
        public override double Compute(int previousState, int currentState, T[] observations,
            int observationIndex, int outputClass)
        {
            if (previous == previousState && current == currentState)
            {
                return 1.0;
            }

            return 0.0;
        }

        /// <summary>
        ///     Computes the probability of occurrence of this
        ///     feature given a sequence of observations.
        /// </summary>
        /// <param name="fwd">The matrix of forward state probabilities.</param>
        /// <param name="bwd">The matrix of backward state probabilities.</param>
        /// <param name="x">The observation sequence.</param>
        /// <param name="y">The output class label for the sequence.</param>
        /// <returns>The probability of occurrence of this feature.</returns>
        public override double Marginal(double[,] fwd, double[,] bwd, T[] x, int y)
        {
            // Assume the simplifying structure that each
            // factor is responsible for single output y.
            if (y != FactorIndex)
            {
                return 0;
            }

            var owner = Owner.Factors[FactorIndex];

            double marginal = 0;
            for (var t = 0; t < x.Length - 1; t++)
            {
                marginal += fwd[t, previous] * bwd[t + 1, current]
                                             * System.Math.Exp(owner.Compute(previous, current, x, t + 1, y));
            }

            return marginal;
        }

        /// <summary>
        ///     Computes the log-probability of occurrence of this
        ///     feature given a sequence of observations.
        /// </summary>
        /// <param name="lnFwd">The matrix of forward state log-probabilities.</param>
        /// <param name="lnBwd">The matrix of backward state log-probabilities.</param>
        /// <param name="x">The observation sequence.</param>
        /// <param name="y">The output class label for the sequence.</param>
        /// <returns>The probability of occurrence of this feature.</returns>
        public override double LogMarginal(double[,] lnFwd, double[,] lnBwd, T[] x, int y)
        {
            // Assume the simplifying structure that each
            // factor is responsible for single output y.
            if (y != FactorIndex)
            {
                return double.NegativeInfinity;
            }

            var owner = Owner.Factors[FactorIndex];

            var marginal = double.NegativeInfinity;
            for (var t = 0; t < x.Length - 1; t++)
            {
                marginal = Special.LogSum(marginal, lnFwd[t, previous] + lnBwd[t + 1, current]
                                                                       + owner.Compute(previous, current, x, t + 1, y));
            }

            return marginal;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public IFeature<T> Clone(IPotentialFunction<T> newOwner)
        {
            var clone = (TransitionFeature<T>)MemberwiseClone();
            clone.Owner = newOwner;
            return clone;
        }
    }
}