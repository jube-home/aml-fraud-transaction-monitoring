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
using Accord.Diagnostics;

namespace Accord.Statistics.Models.Fields.Functions.Specialized
{
    /// <summary>
    ///     Discrete-density Markov Factor Potential (Clique Potential) function.
    /// </summary>
    [Serializable]
    public class MarkovDiscreteFactor : FactorPotential<int>
    {
        /// <summary>
        ///     Creates a new factor (clique) potential function.
        /// </summary>
        /// <param name="owner">The owner <see cref="IPotentialFunction{T}" />.</param>
        /// <param name="states">The number of states in this clique potential.</param>
        /// <param name="factorIndex">The index of this factor potential in the <paramref name="owner" />.</param>
        /// <param name="symbols">The number of symbols in the discrete alphabet.</param>
        /// <param name="classIndex">
        ///     The index of the first class label feature in the <paramref name="owner" />'s parameter
        ///     vector.
        /// </param>
        /// <param name="classCount">The number of class label features in this factor.</param>
        /// <param name="edgeIndex">The index of the first edge feature in the <paramref name="owner" />'s parameter vector.</param>
        /// <param name="edgeCount">The number of edge features in this factor.</param>
        /// <param name="stateIndex">The index of the first state feature in the <paramref name="owner" />'s parameter vector.</param>
        /// <param name="stateCount">The number of state features in this factor.</param>
        public MarkovDiscreteFactor(IPotentialFunction<int> owner, int states, int factorIndex, int symbols,
            int edgeIndex, int edgeCount,
            int stateIndex, int stateCount,
            int classIndex = 0, int classCount = 0)
            : base(owner, states, factorIndex, edgeIndex, edgeCount, stateIndex, stateCount, classIndex, classCount)
        {
            Symbols = symbols;
        }

        /// <summary>
        ///     Gets the number of symbols in the discrete
        ///     alphabet used by this Markov model factor.
        /// </summary>
        public int Symbols { get; private set; }

        /// <summary>
        ///     Computes the factor potential function for the given parameters.
        /// </summary>
        /// <param name="previousState">The previous state in a given sequence of states.</param>
        /// <param name="currentState">The current state in a given sequence of states.</param>
        /// <param name="observations">The observation vector.</param>
        /// <param name="index">The index of the observation in the current state of the sequence.</param>
        /// <param name="outputClass">The output class label for the sequence.</param>
        /// <returns>The value of the factor potential function evaluated for the given parameters.</returns>
        public override double Compute(int previousState, int currentState, int[] observations, int index,
            int outputClass = 0)
        {
            // PS: This code seems messy because it should be as fast as possible. Unfortunately, 
            // avoiding (virtual) function calls is the main objective for this method to exist;
            // thus, refactoring those sections would defeat the existence of this method in the
            // first place. An alternative approach might be reconsidered once the project fully
            // migrates to .NET 4.5 and we could then use the explicit in-line method attributes.


            if (outputClass != Index)
            {
                return double.NegativeInfinity;
            }

            var parameters = Owner.Weights;

            double sum = 0;

            // Output (class) probability
            if (OutputParameters.Count != 0 && previousState == -1)
            {
                var cindex = OutputParameters.Offset;
                var w = parameters[cindex];

                if (double.IsNegativeInfinity(w))
                {
                    return double.NegativeInfinity;
                }

                if (double.IsNaN(w))
                {
                    return parameters[cindex] = double.NegativeInfinity;
                }

                sum += w;
            }

            if (previousState == -1)
            {
                // Initial state probability (pi)
                var pindex = EdgeParameters.Offset + currentState;
                var p = parameters[pindex];

                if (double.IsNegativeInfinity(p))
                {
                    return double.NegativeInfinity;
                }

                if (double.IsNaN(p))
                {
                    return parameters[pindex] = double.NegativeInfinity;
                }

                sum += p;
            }
            else
            {
                // State transition probabilities (A)
                var aindex = EdgeParameters.Offset + States + previousState * States + currentState;
                var a = parameters[aindex];

                if (double.IsNegativeInfinity(a))
                {
                    return double.NegativeInfinity;
                }

                if (double.IsNaN(a))
                {
                    return parameters[aindex] = double.NegativeInfinity;
                }

                sum += a;
            }

            // State emission probability (B)
            var bindex = StateParameters.Offset + currentState * Symbols + observations[index];
            var b = parameters[bindex];

            if (double.IsNegativeInfinity(b))
            {
                return double.NegativeInfinity;
            }

            if (double.IsNaN(b))
            {
                return parameters[bindex] = double.NegativeInfinity;
            }

            sum += b;


            Debug.Assert(!double.IsNaN(sum));

            return sum;
        }
    }
}