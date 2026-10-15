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
using System.Collections;
using System.Collections.Generic;
using Accord.Statistics.Models.Fields.Features;

namespace Accord.Statistics.Models.Fields.Functions
{
    /// <summary>
    ///     Factor Potential (Clique Potential) function.
    /// </summary>
    /// <typeparam name="T">The type of the observations being modeled.</typeparam>
    [Serializable]
    public class FactorPotential<T> : IEnumerable<IFeature<T>>
    {
        /// <summary>
        ///     Creates a new factor (clique) potential function.
        /// </summary>
        /// <param name="owner">The owner <see cref="IPotentialFunction{T}" />.</param>
        /// <param name="states">The number of states in this clique potential.</param>
        /// <param name="factorIndex">The index of this factor potential in the <paramref name="owner" />.</param>
        /// <param name="edgeIndex">The index of the first edge feature in the <paramref name="owner" />'s parameter vector.</param>
        /// <param name="edgeCount">The number of edge features in this factor.</param>
        /// <param name="stateIndex">The index of the first state feature in the <paramref name="owner" />'s parameter vector.</param>
        /// <param name="stateCount">The number of state features in this factor.</param>
        /// <param name="classIndex">The index of the first class feature in the <paramref name="owner" />'s parameter vector.</param>
        /// <param name="classCount">The number of class features in this factor.</param>
        public FactorPotential(IPotentialFunction<T> owner, int states, int factorIndex,
            int edgeIndex, int edgeCount, int stateIndex, int stateCount, int classIndex = 0, int classCount = 0)
            : this(owner, states, factorIndex)
        {
            EdgeParameters = new ArraySegment<double>(owner.Weights, edgeIndex, edgeCount);
            StateParameters = new ArraySegment<double>(owner.Weights, stateIndex, stateCount);
            OutputParameters = new ArraySegment<double>(owner.Weights, classIndex, classCount);

            FactorParameters = new ArraySegment<double>(owner.Weights,
                System.Math.Min(System.Math.Min(edgeIndex, stateIndex), classIndex),
                edgeCount + stateCount + classCount);
        }

        /// <summary>
        ///     Creates a new factor (clique) potential function.
        /// </summary>
        /// <param name="owner">The owner <see cref="IPotentialFunction{T}" />.</param>
        /// <param name="states">The number of states in this clique potential.</param>
        /// <param name="factorIndex">The index of this factor potential in the <paramref name="owner" />.</param>
        public FactorPotential(IPotentialFunction<T> owner, int states, int factorIndex)
        {
            Owner = owner;
            States = states;
            Index = factorIndex;
        }

        /// <summary>
        ///     Gets the <see cref="IPotentialFunction{T}" />
        ///     to which this factor potential belongs.
        /// </summary>
        public IPotentialFunction<T> Owner { get; private set; }

        /// <summary>
        ///     Gets the number of model states
        ///     assumed by this function.
        /// </summary>
        public int States { get; private set; }

        /// <summary>
        ///     Gets the index of this factor in the
        ///     <see cref="Owner" /> potential function.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        ///     Gets the segment of the parameter vector which contains
        ///     parameters respective to all features from this factor.
        /// </summary>
        public ArraySegment<double> FactorParameters { get; private set; }

        /// <summary>
        ///     Gets the segment of the parameter vector which contains
        ///     parameters respective to the edge features.
        /// </summary>
        public ArraySegment<double> EdgeParameters { get; private set; }

        /// <summary>
        ///     Gets the segment of the parameter vector which contains
        ///     parameters respective to the state features.
        /// </summary>
        public ArraySegment<double> StateParameters { get; private set; }

        /// <summary>
        ///     Gets the segment of the parameter vector which contains
        ///     parameters respective to the output features.
        /// </summary>
        public ArraySegment<double> OutputParameters { get; private set; }

        /// <summary>
        ///     Returns an enumerator that iterates through all features in this factor potential function.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<IFeature<T>> GetEnumerator()
        {
            var start = FactorParameters.Offset;
            var end = FactorParameters.Offset + FactorParameters.Count;

            for (var k = start; k < end; k++)
            {
                yield return Owner.Features[k];
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through all features in this factor potential function.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Computes the factor potential function for the given parameters.
        /// </summary>
        /// <param name="states">A state sequence.</param>
        /// <param name="observations">A sequence of observations.</param>
        /// <param name="output">The output class label for the sequence.</param>
        /// <returns>The value of the factor potential function evaluated for the given parameters.</returns>
        public double Compute(int[] states, T[] observations, int output = 0)
        {
            var p = Compute(-1, states[0], observations, 0, output);
            for (var t = 1; t < observations.Length; t++)
            {
                p += Compute(states[t - 1], states[t], observations, t, output);
            }

            return p;
        }

        /// <summary>
        ///     Computes the factor potential function for the given parameters.
        /// </summary>
        /// <param name="states">A state sequence.</param>
        /// <param name="observations">A sequence of observations.</param>
        /// <param name="output">The output class label for the sequence.</param>
        /// <returns>The value of the factor potential function evaluated for the given parameters.</returns>
        public virtual double[] GetFeatureVector(int[] states, T[] observations, int output = 0)
        {
            var featureVector = new double[FactorParameters.Count];

            for (int i = 0, k = FactorParameters.Offset; i < featureVector.Length; i++, k++)
            {
                featureVector[i] = Owner.Features[k].Compute(states, observations, output);
            }

            return featureVector;
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
        public virtual double Compute(int previousState, int currentState,
            T[] observations, int index, int outputClass = 0)
        {
            if (outputClass != Index)
            {
                return double.NegativeInfinity;
            }

            var start = FactorParameters.Offset;
            var end = FactorParameters.Offset + FactorParameters.Count;

            double sum = 0;

            for (var k = start; k < end; k++)
            {
                var weight = Owner.Weights[k];

                if (double.IsNaN(weight))
                {
                    Owner.Weights[k] = weight = 0;
                }

                if (weight != 0)
                {
                    var feature = Owner.Features[k];

                    var value = feature.Compute(previousState, currentState,
                        observations, index, outputClass);

                    if (value != 0)
                    {
                        sum += weight * value;
                    }
                }

                if (double.IsNaN(sum))
                {
                    return 0;
                }

                if (double.IsPositiveInfinity(sum))
                {
                    return double.MaxValue;
                }

                if (double.IsNegativeInfinity(sum))
                {
                    return double.NegativeInfinity;
                }
            }

            return sum;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public FactorPotential<T> Clone(IPotentialFunction<T> newOwner)
        {
            var clone = (FactorPotential<T>)MemberwiseClone();
            clone.Owner = newOwner;
            return clone;
        }
    }
}