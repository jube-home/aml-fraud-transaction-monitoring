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
    ///     State feature for second moment Gaussian emission probabilities.
    /// </summary>
    [Serializable]
    public sealed class MultivariateSecondMomentFeature : FeatureBase<double[]>, IFeature<double[]>
    {
        private int dimension;
        private int state;

        /// <summary>
        ///     Constructs a new symbol emission feature.
        /// </summary>
        /// <param name="owner">The potential function to which this feature belongs.</param>
        /// <param name="factorIndex">The index of the potential factor to which this feature belongs.</param>
        /// <param name="state">The state for the emission.</param>
        /// <param name="dimension">
        ///     The dimension of the multidimensional
        ///     observation this feature should respond to.
        /// </param>
        public MultivariateSecondMomentFeature(IPotentialFunction<double[]> owner, int factorIndex,
            int state, int dimension)
            : base(owner, factorIndex)
        {
            this.state = state;
            this.dimension = dimension;
        }

        /// <summary>
        ///     Computes the feature for the given parameters.
        /// </summary>
        /// <param name="previousState">The previous state.</param>
        /// <param name="currentState">The current state.</param>
        /// <param name="observations">The observations.</param>
        /// <param name="observationIndex">The index of the current observation.</param>
        /// <param name="outputClass">The output class label for the sequence.</param>
        public override double Compute(int previousState, int currentState, double[][] observations,
            int observationIndex, int outputClass)
        {
            if (currentState == state)
            {
                if (observationIndex >= 0 && observationIndex < observations.Length)
                {
                    var x = observations[observationIndex][dimension];
                    return x * x;
                }
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
        public override double Marginal(double[,] fwd, double[,] bwd, double[][] x, int y)
        {
            // Assume the simplifying structure that each
            // factor is responsible for single output y.
            if (y != FactorIndex)
            {
                return 0;
            }

            double marginal = 0;

            for (var t = 0; t < x.Length; t++)
            {
                marginal += fwd[t, state] * bwd[t, state] * (x[t][dimension] * x[t][dimension]);
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
        public override double LogMarginal(double[,] lnFwd, double[,] lnBwd, double[][] x, int y)
        {
            // Assume the simplifying structure that each
            // factor is responsible for single output y.
            if (y != FactorIndex)
            {
                return double.NegativeInfinity;
            }

            var marginal = double.NegativeInfinity;
            for (var t = 0; t < x.Length; t++)
            {
                marginal = Special.LogSum(marginal,
                    lnFwd[t, state] + lnBwd[t, state] + System.Math.Log(x[t][dimension] * x[t][dimension]));
            }

            return marginal;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public IFeature<double[]> Clone(IPotentialFunction<double[]> newOwner)
        {
            var clone = (MultivariateSecondMomentFeature)MemberwiseClone();
            clone.Owner = newOwner;
            return clone;
        }
    }
}