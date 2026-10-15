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
using Accord.Math.Random;

namespace Accord.Statistics.Models.Markov.Topology
{
    /// <summary>
    ///     Forward Topology for Hidden Markov Models.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Forward topologies are commonly used to initialize models in which
    ///         training sequences can be organized in samples, such as in the recognition
    ///         of spoken words. In spoken word recognition, several examples of a single
    ///         word can (and should) be used to train a single model, to achieve the most
    ///         general model able to generalize over a great number of word samples.
    ///     </para>
    ///     <para>
    ///         Forward models can typically have a large number of states.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Alexander Schliep, "Learning Hidden Markov Model Topology".
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Richard Hughey and Anders Krogh, "Hidden Markov models for sequence analysis:
    ///                     extension and analysis of the basic method", CABIOS 12(2):95-107, 1996. Available in:
    ///                     http://compbio.soe.ucsc.edu/html_format_papers/hughkrogh96/cabios.html
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="HiddenMarkovModel" />
    /// <seealso cref="ITopology" />
    /// <seealso cref="Ergodic" />
    /// <seealso cref="Custom" />
    /// <example>
    ///     <para>
    ///         In the following example, we will create a Forward-only
    ///         discrete-density hidden Markov model.
    ///     </para>
    ///     <code>
    ///   // Create a new Forward-only hidden Markov model with
    ///   // three forward-only states and four sequence symbols.
    ///   var model = new HiddenMarkovModel(new Forward(3), 4);
    /// 
    ///   // After creation, the state transition matrix for the model
    ///   // should be given by:
    ///   //
    ///   //       { 0.33, 0.33, 0.33 }
    ///   //       { 0.00, 0.50, 0.50 }
    ///   //       { 0.00, 0.00, 1.00 }
    ///   //       
    ///   // in which no backward transitions are allowed (have zero probability).
    ///   </code>
    /// </example>
    [Serializable]
    public class Forward : ITopology
    {
        private int deepness;
        private bool random;
        private int states;

        /// <summary>
        ///     Creates a new Forward topology for a given number of states.
        /// </summary>
        /// <param name="states">The number of states to be used in the model.</param>
        public Forward(int states)
            : this(states, states, false)
        {
        }

        /// <summary>
        ///     Creates a new Forward topology for a given number of states.
        /// </summary>
        /// <param name="states">The number of states to be used in the model.</param>
        /// <param name="deepness">
        ///     The maximum number of forward transitions allowed
        ///     for a state. Default is to use the same as the number of states (all forward
        ///     connections are allowed).
        /// </param>
        public Forward(int states, int deepness)
            : this(states, deepness, false)
        {
        }

        /// <summary>
        ///     Creates a new Forward topology for a given number of states.
        /// </summary>
        /// <param name="states">The number of states to be used in the model.</param>
        /// <param name="random">
        ///     Whether to initialize the model with random probabilities
        ///     or uniformly with <c>1 / number of states</c>. Default is false (default is
        ///     to use <c>1/states</c>).
        /// </param>
        public Forward(int states, bool random)
            : this(states, states, random)
        {
        }

        /// <summary>
        ///     Creates a new Forward topology for a given number of states.
        /// </summary>
        /// <param name="states">The number of states to be used in the model.</param>
        /// <param name="deepness">
        ///     The maximum number of forward transitions allowed
        ///     for a state. Default is to use the same as the number of states (all forward
        ///     connections are allowed).
        /// </param>
        /// <param name="random">
        ///     Whether to initialize the model with random probabilities
        ///     or uniformly with <c>1 / number of states</c>. Default is false (default is
        ///     to use <c>1/states</c>).
        /// </param>
        public Forward(int states, int deepness, bool random)
        {
            if (states <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "states", "Number of states should be higher than zero.");
            }

            if (deepness > states)
            {
                throw new ArgumentOutOfRangeException(
                    "deepness", "Deepness level should be lesser or equal to the number of states.");
            }


            this.states = states;
            this.deepness = deepness;
            this.random = random;

            Initial = new double[states];
            Initial[0] = 1.0;
        }

        /// <summary>
        ///     Gets or sets the maximum deepness level allowed
        ///     for the forward state transition chains.
        /// </summary>
        public int Deepness
        {
            get => deepness;
            set => deepness = value;
        }

        /// <summary>
        ///     Gets or sets whether the transition matrix
        ///     should be initialized with random probabilities
        ///     or not. Default is false.
        /// </summary>
        public bool Random
        {
            get => random;
            set => random = value;
        }

        /// <summary>
        ///     Gets the initial state probabilities.
        /// </summary>
        public double[] Initial { get; private set; }

        /// <summary>
        ///     Gets the number of states in this topology.
        /// </summary>
        public int States
        {
            get => states;
            set
            {
                states = value;
                Initial = Vector.Create(states, Initial);
            }
        }

        /// <summary>
        ///     Creates the state transitions matrix and the
        ///     initial state probabilities for this topology.
        /// </summary>
        public int Create(bool logarithm, out double[,] transitionMatrix, out double[] initialState)
        {
            var m = System.Math.Min(States, Deepness);
            var A = new double[States, States];

            if (random)
            {
                var rand = Generator.Random;

                // Create pi
                double sum = 0;
                for (var i = 0; i < states; i++)
                {
                    sum += Initial[i] = rand.NextDouble();
                }

                for (var i = 0; i < states; i++)
                {
                    Initial[i] /= sum;
                }

                // Create A using random uniform distribution,
                //  without allowing backward transitions
                for (var i = 0; i < states; i++)
                {
                    sum = 0.0;
                    for (var j = i; j < states && j - i < m; j++)
                    {
                        sum += A[i, j] = rand.NextDouble();
                    }

                    for (var j = i; j < states && j - i < m; j++)
                    {
                        A[i, j] /= sum;
                    }
                }
            }
            else
            {
                // Create A using equal uniform probabilities,
                //   without allowing backward transitions.

                for (var i = 0; i < states; i++)
                {
                    var d = 1.0 / System.Math.Min(m, states - i);
                    for (var j = i; j < states && j - i < m; j++)
                    {
                        A[i, j] = d;
                    }
                }
            }

            if (logarithm)
            {
                transitionMatrix = A.Log();
                initialState = Initial.Log();
            }
            else
            {
                transitionMatrix = A;
                initialState = (double[])Initial.Clone();
            }

            return States;
        }
    }
}