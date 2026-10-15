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
using Accord.Statistics.Models.Fields.Features;

namespace Accord.Statistics.Models.Fields.Functions
{
    /// <summary>
    ///     Base implementation for <see cref="IPotentialFunction{T}">potential functions</see>.
    /// </summary>
    /// <typeparam name="T">The type of the observations modeled.</typeparam>
    [Serializable]
    public abstract class PotentialFunctionBase<T>
    {
        /// <summary>
        ///     Gets the factor potentials (also known as clique potentials)
        ///     functions composing this potential function.
        /// </summary>
        public FactorPotential<T>[] Factors { get; protected set; }

        /// <summary>
        ///     Gets the number of output classes assumed by this function.
        /// </summary>
        public int Outputs { get; protected set; }

        /// <summary>
        ///     Gets or sets the set of weights for each feature function.
        /// </summary>
        /// <value>The weights for each of the feature functions.</value>
        public double[] Weights { get; set; }

        /// <summary>
        ///     Gets the feature functions composing this potential function.
        /// </summary>
        public IFeature<T>[] Features { get; protected set; }

        /// <summary>
        ///     Computes the factor potential function for the given parameters.
        /// </summary>
        /// <param name="states">A state sequence.</param>
        /// <param name="observations">A sequence of observations.</param>
        /// <param name="output">The output class label for the sequence.</param>
        /// <returns>The value of the factor potential function evaluated for the given parameters.</returns>
        public virtual double[] GetFeatureVector(int[] states, T[] observations, int output = 0)
        {
            var featureVector = new double[Weights.Length];

            for (var i = 0; i < featureVector.Length; i++)
            {
                featureVector[i] = Features[i].Compute(states, observations, output);
            }

            return featureVector;
        }

        internal void Deoptimize()
        {
            for (var i = 0; i < Factors.Length; i++)
            {
                var factor = Factors[i];
                Factors[i] = new FactorPotential<T>(factor.Owner, factor.States, factor.Index,
                    factor.EdgeParameters.Offset, factor.EdgeParameters.Count,
                    factor.StateParameters.Offset, factor.StateParameters.Count,
                    factor.OutputParameters.Offset, factor.OutputParameters.Count);
            }
        }
    }
}