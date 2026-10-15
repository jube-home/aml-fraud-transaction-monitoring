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
using Accord.Statistics.Models.Fields.Features;

namespace Accord.Statistics.Models.Fields.Functions.Specialized
{
    /// <summary>
    ///     Factor Potential function for a Markov model whose states are independent
    ///     distributions composed of discrete and Normal distributed components.
    /// </summary>
    [Serializable]
    public class MarkovIndependentFactor : FactorPotential<double[]>
    {
        private int[][] stateTable;

        /// <summary>
        ///     Creates a new factor (clique) potential function.
        /// </summary>
        /// <param name="owner">The owner <see cref="IPotentialFunction{T}" />.</param>
        /// <param name="states">The number of states in this clique potential.</param>
        /// <param name="factorIndex">The index of this factor potential in the <paramref name="owner" />.</param>
        /// <param name="stateTable">The lookup table of states where the independent distributions begin.</param>
        /// <param name="classIndex">
        ///     The index of the first class label feature in the <paramref name="owner" />'s parameter
        ///     vector.
        /// </param>
        /// <param name="classCount">The number of class label features in this factor.</param>
        /// <param name="edgeIndex">The index of the first edge feature in the <paramref name="owner" />'s parameter vector.</param>
        /// <param name="edgeCount">The number of edge features in this factor.</param>
        /// <param name="stateIndex">The index of the first state feature in the <paramref name="owner" />'s parameter vector.</param>
        /// <param name="stateCount">The number of state features in this factor.</param>
        public MarkovIndependentFactor(IPotentialFunction<double[]> owner, int states, int factorIndex,
            int[][] stateTable,
            int edgeIndex, int edgeCount,
            int stateIndex, int stateCount,
            int classIndex = 0, int classCount = 0)
            : base(owner, states, factorIndex, edgeIndex, edgeCount, stateIndex, stateCount, classIndex, classCount)
        {
            this.stateTable = stateTable;
        }

        /// <summary>
        ///     Computes the factor potential function for the given parameters.
        /// </summary>
        /// <param name="previousState">The previous state in a given sequence of states.</param>
        /// <param name="currentState">The current state in a given sequence of states.</param>
        /// <param name="observations">The observation vector.</param>
        /// <param name="index">The index of the observation in the current state of the sequence.</param>
        /// <param name="outputClass">The output class label for the sequence.</param>
        /// <returns>The value of the factor potential function evaluated for the given parameters.</returns>
        public override double Compute(int previousState, int currentState, double[][] observations, int index,
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
            var features = Owner.Features;


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
                var pi = parameters[pindex];

                if (double.IsNegativeInfinity(pi))
                {
                    return double.NegativeInfinity;
                }

                if (double.IsNaN(pi))
                {
                    return parameters[pindex] = double.NegativeInfinity;
                }

                sum += pi;
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
            var lookup = stateTable[currentState];
            var observation = observations[index];

            double B = 0;

            // For each dimension of the observation
            for (var j = 0; j < observation.Length; j++)
            {
                var bindex = StateParameters.Offset + lookup[j];

                var discrete = features[bindex] as MultivariateEmissionFeature;
                if (discrete != null)
                {
                    var i = (int)observations[index][j];
                    var b = parameters[bindex + i];

                    if (double.IsNegativeInfinity(b))
                    {
                        return double.NegativeInfinity;
                    }

                    if (double.IsNaN(b))
                    {
                        return parameters[bindex + i] = double.NegativeInfinity;
                    }

                    B += b;
                    continue;
                }

                var occupancy = features[bindex] as OccupancyFeature<double[]>;
                if (occupancy != null)
                {
                    // State occupancy feature
                    var b = parameters[bindex];

                    if (double.IsNegativeInfinity(b))
                    {
                        return double.NegativeInfinity;
                    }

                    if (double.IsNaN(b))
                    {
                        return parameters[bindex] = double.NegativeInfinity;
                    }

                    var u = observation[j];

                    var m1index = ++bindex;
                    var m2index = ++bindex;

                    if (u != 0)
                    {
                        // First moment feature
                        var m1 = parameters[m1index];

                        if (double.IsNegativeInfinity(m1))
                        {
                            return double.NegativeInfinity;
                        }

                        if (double.IsNaN(m1))
                        {
                            m1 = parameters[m1index] = double.NegativeInfinity;
                        }

                        b += m1 * u;

                        // Second moment feature
                        var m2 = parameters[m2index];

                        if (double.IsNegativeInfinity(m2))
                        {
                            return double.NegativeInfinity;
                        }

                        if (double.IsNaN(m2))
                        {
                            m2 = parameters[m2index] = double.NegativeInfinity;
                        }

                        b += m2 * u * u;
                    }

                    B += b;
                    continue;
                }

                throw new InvalidOperationException("The feature vector contains an unsupported"
                                                    + " feature type. The MarkovIndependentFactor (which was likely created by a "
                                                    + " MarkovMultivariateFunction) supports only discrete MultivariateEmissionFeature "
                                                    + " (for GeneralDiscreteDistributions) and Occupancy/FirstMoment/SecondMoment features "
                                                    + " (for Normal distributions).");
            }

            sum += B;

            Debug.Assert(!double.IsNaN(sum));

            return sum;
        }
    }
}