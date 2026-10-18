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
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Models.Fields.Functions;

namespace Accord.Statistics.Models.Fields
{
    /// <summary>
    ///     Linear-Chain Conditional Random Field (CRF).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A conditional random field (CRF) is a type of discriminative undirected
    ///         probabilistic graphical model. It is most often used for labeling or parsing
    ///         of sequential data, such as natural language text or biological sequences
    ///         and computer vision.
    ///     </para>
    ///     <para>This implementation is currently experimental.</para>
    /// </remarks>
    [Serializable]
    public class ConditionalRandomField<T> : TaggerBase<T>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConditionalRandomField{T}" /> class.
        /// </summary>
        /// <param name="states">The number of states for the model.</param>
        /// <param name="function">The potential function to be used by the model.</param>
        public ConditionalRandomField(int states, IPotentialFunction<T> function)
        {
            States = states;
            Function = function;
        }

        /// <summary>
        ///     Gets the number of states in this
        ///     linear-chain Conditional Random Field.
        /// </summary>
        public int States { get; private set; }

        /// <summary>
        ///     Gets the potential function encompassing
        ///     all feature functions for this model.
        /// </summary>
        public IPotentialFunction<T> Function { get; private set; }

        /// <summary>
        ///     Computes the partition function, as known as Z(x),
        ///     for the specified observations.
        /// </summary>
        public double Partition(T[] observations)
        {
            return System.Math.Exp(LogPartition(observations));
        }

        /// <summary>
        ///     Computes the Log of the partition function.
        /// </summary>
        public double LogPartition(T[] observations)
        {
            double logLikelihood;
            ForwardBackwardAlgorithm.LogForward(Function.Factors[0], observations, 0, out logLikelihood);
            return logLikelihood;
        }

        /// <summary>
        ///     Computes the log-likelihood of the model for the given observations.
        ///     This method is equivalent to the HiddenMarkovModel.LogLikelihood(TObservation[], int[])
        ///     method.
        /// </summary>
        public double LogLikelihood(T[] observations, int[] labels)
        {
            var p = Function.Factors[0].Compute(-1, labels[0], observations, 0);
            for (var t = 1; t < observations.Length; t++)
            {
                p += Function.Factors[0].Compute(labels[t - 1], labels[t], observations, t);
            }

            var z = LogPartition(observations);

            if (p == z)
            {
                return 0;
            }

            if (double.IsInfinity(p))
            {
                return 0;
            }

            if (double.IsInfinity(z))
            {
                return 0;
            }

            Debug.Assert(!double.IsNaN(p));
            Debug.Assert(!double.IsInfinity(p));

            Debug.Assert(!double.IsNaN(z));
            Debug.Assert(!double.IsInfinity(z));

            return p - z;
        }

        /// <summary>
        ///     Computes the most likely state labels for the given observations,
        ///     returning the overall sequence probability for this model.
        /// </summary>
        public int[] Compute(T[] observations, out double logLikelihood)
        {
            return viterbi(Function.Factors[0], observations, out logLikelihood);
        }

        private int[] viterbi(FactorPotential<T> factor, T[] observations, out double logLikelihood)
        {
            // Viterbi-forward algorithm.
            var states = factor.States;
            int maxState;
            double maxWeight;
            double weight;

            var s = new int[states, observations.Length];
            var lnFwd = new double[states, observations.Length];


            // Base
            for (var i = 0; i < states; i++)
            {
                lnFwd[i, 0] = Function.Factors[0].Compute(-1, i, observations, 0);
            }

            // Induction
            for (var t = 1; t < observations.Length; t++)
            {
                for (var j = 0; j < states; j++)
                {
                    maxState = 0;
                    maxWeight = lnFwd[0, t - 1] + Function.Factors[0].Compute(0, j, observations, t);

                    for (var i = 1; i < states; i++)
                    {
                        weight = lnFwd[i, t - 1] + Function.Factors[0].Compute(i, j, observations, t);

                        if (weight > maxWeight)
                        {
                            maxState = i;
                            maxWeight = weight;
                        }
                    }

                    lnFwd[j, t] = maxWeight;
                    s[j, t] = maxState;
                }
            }


            // Find minimum value for time T-1
            maxState = 0;
            maxWeight = lnFwd[0, observations.Length - 1];

            for (var i = 1; i < states; i++)
            {
                if (lnFwd[i, observations.Length - 1] > maxWeight)
                {
                    maxState = i;
                    maxWeight = lnFwd[i, observations.Length - 1];
                }
            }


            // Trackback
            var path = new int[observations.Length];
            path[path.Length - 1] = maxState;

            for (var t = path.Length - 2; t >= 0; t--)
            {
                path[t] = s[path[t + 1], t + 1];
            }


            // Returns the sequence probability as an out parameter
            logLikelihood = maxWeight;

            // Returns the most likely (Viterbi path) for the given sequence
            return path;
        }

        /// <summary>
        ///     Computes the most likely state labels for the given observations,
        ///     returning the overall sequence log-likelihood for this model.
        /// </summary>
        public double LogLikelihood(T[][] observations, int[][] labels)
        {
            double logLikelihood = 0;
            for (var i = 0; i < observations.Length; i++)
            {
                logLikelihood += LogLikelihood(observations[i], labels[i]);
            }

            Debug.Assert(!double.IsNaN(logLikelihood));
            Debug.Assert(!double.IsInfinity(logLikelihood));

            return logLikelihood;
        }

        #region ICloneable Members

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return new ConditionalRandomField<T>(States, (IPotentialFunction<T>)Function.Clone());
        }

        #endregion

        /// <summary>
        ///     Computes class-label decisions for the given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vectors that should be classified as
        ///     any of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <param name="result">The location where to store the class-labels.</param>
        /// <returns>
        ///     A set of class-labels that best describe the <paramref name="input" />
        ///     vectors according to this classifier.
        /// </returns>
        public override int[][] Decide(T[][] input, int[][] result)
        {
            for (var i = 0; i < input.Length; i++)
            {
                Compute(input[i], out _).CopyTo(result[i]);
            }

            return result;
        }
    }
}