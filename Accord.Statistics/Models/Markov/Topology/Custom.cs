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
using System.Data;
using Accord.Math;

namespace Accord.Statistics.Models.Markov.Topology
{
    /// <summary>
    ///     Custom Topology for Hidden Markov Model.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         An Hidden Markov Model Topology specifies how many states and which
    ///         initial probabilities a Markov model should have. Two common topologies
    ///         can be discussed in terms of transition state probabilities and are
    ///         available to construction through the <see cref="Ergodic" /> and
    ///         <see cref="Forward" /> classes implementing the <see cref="ITopology" />
    ///         interface.
    ///     </para>
    ///     <para>
    ///         Topology specification is important with regard to both learning and
    ///         performance: A model with too many states (and thus too many settable
    ///         parameters) will require too much training data while an model with an
    ///         insufficient number of states will prohibit the HMM from capturing subtle
    ///         statistical patterns.
    ///     </para>
    ///     <para>
    ///         This custom implementation allows for arbitrarily specification of
    ///         the state transition matrix and initial state probabilities for
    ///         <see cref="IHiddenMarkovModel">hidden Markov models</see>.
    ///     </para>
    /// </remarks>
    /// <seealso cref="HiddenMarkovModel" />
    /// <seealso cref="ITopology" />
    /// <seealso cref="Ergodic" />
    /// <seealso cref="Forward" />
    [Serializable]
    public class Custom : ITopology
    {
        private int states;

        /// <summary>
        ///     Creates a new custom topology with user-defined
        ///     transition matrix and initial state probabilities.
        /// </summary>
        /// <param name="initial">The initial probabilities for the model.</param>
        /// <param name="transitions">The transition probabilities for the model.</param>
        public Custom(double[][] transitions, double[] initial)
            : this(transitions, initial, false)
        {
        }

        /// <summary>
        ///     Creates a new custom topology with user-defined
        ///     transition matrix and initial state probabilities.
        /// </summary>
        /// <param name="initial">The initial probabilities for the model.</param>
        /// <param name="transitions">The transition probabilities for the model.</param>
        /// <param name="logarithm">
        ///     Set to true if the passed transitions are given
        ///     in log-probabilities. Default is false (given values are probabilities).
        /// </param>
        public Custom(double[][] transitions, double[] initial, bool logarithm)
            : this(transitions.ToMatrix(), initial, logarithm)
        {
        }

        /// <summary>
        ///     Creates a new custom topology with user-defined
        ///     transition matrix and initial state probabilities.
        /// </summary>
        /// <param name="initial">The initial probabilities for the model.</param>
        /// <param name="transitions">The transition probabilities for the model.</param>
        public Custom(double[,] transitions, double[] initial)
            : this(transitions, initial, false)
        {
        }

        /// <summary>
        ///     Creates a new custom topology with user-defined
        ///     transition matrix and initial state probabilities.
        /// </summary>
        /// <param name="initial">The initial probabilities for the model.</param>
        /// <param name="transitions">The transition probabilities for the model.</param>
        /// <param name="logarithm">
        ///     Set to true if the passed transitions are given
        ///     in log-probabilities. Default is false (given values are probabilities).
        /// </param>
        public Custom(double[,] transitions, double[] initial, bool logarithm)
        {
            if (transitions == null)
            {
                throw new ArgumentNullException("transitions");
            }

            if (initial == null)
            {
                throw new ArgumentNullException("initial");
            }

            if (transitions.GetLength(0) != transitions.GetLength(1))
            {
                if (transitions.GetLength(0) != transitions.GetLength(1))
                {
                    throw new ArgumentException(
                        "Transition matrix should be square.",
                        "transitions");
                }
            }

            if (initial.Length != transitions.GetLength(0))
            {
                throw new ArgumentException(
                    "Initial probabilities should have the same length as the number of states in the transition matrix.",
                    "initial");
            }

            states = transitions.GetLength(0);

            for (var i = 0; i < states; i++)
            {
                for (var j = 0; j < states; j++)
                {
                    if (double.IsNaN(transitions[i, j]))
                    {
                        throw new ArgumentOutOfRangeException("transitions");
                    }
                }
            }

            if (logarithm)
            {
                transitions = transitions.Exp();
                initial = initial.Exp();
            }

            Transitions = transitions;
            Initial = initial;

            double initialSum = 0;
            for (var i = 0; i < Initial.Length; i++)
            {
                initialSum += Initial[i];
            }

            if (!initialSum.IsEqual(1, 1e-10))
            {
                throw new ArgumentException("Initial probabilities do not sum up to one.",
                    "initial");
            }

            for (var i = 0; i < states; i++)
            {
                double stateSum = 0;
                for (var j = 0; j < states; j++)
                {
                    stateSum += transitions[i, j];
                }

                if (!stateSum.IsEqual(1, 1e-10))
                {
                    throw new ArgumentException("State probabilities for state " + i +
                                                " do not sum up to one.", "transitions");
                }
            }
        }

        /// <summary>
        ///     Gets the initial state probabilities.
        /// </summary>
        public double[] Initial { get; }

        /// <summary>
        ///     Gets the state-transitions matrix.
        /// </summary>
        public double[,] Transitions { get; }

        /// <summary>
        ///     Gets the number of states in this topology.
        /// </summary>
        public int States
        {
            get => states;
            set => throw new ReadOnlyException("Property is read-only.");
        }

        /// <summary>
        ///     Creates the state transitions matrix and the
        ///     initial state probabilities for this topology.
        /// </summary>
        public int Create(bool logarithm, out double[,] transitionMatrix, out double[] initialState)
        {
            if (logarithm)
            {
                transitionMatrix = Transitions.Log();
                initialState = Initial.Log();
            }
            else
            {
                transitionMatrix = (double[,])Transitions.Clone();
                initialState = (double[])Initial.Clone();
            }

            return states;
        }
    }
}