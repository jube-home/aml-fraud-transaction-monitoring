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
    ///     Ergodic (fully-connected) Topology for Hidden Markov Models.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Ergodic models are commonly used to represent models in which a single
    ///         (large) sequence of observations is used for training (such as when a
    ///         training sequence does not have well defined starting and ending points
    ///         and can potentially be infinitely long).
    ///     </para>
    ///     <para>
    ///         Models starting with an ergodic transition-state topology typically
    ///         have only a small number of states.
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
    /// <seealso cref="Forward" />
    /// <seealso cref="Custom" />
    /// <example>
    ///     <para>
    ///         In a second example, we will create an Ergodic (fully connected)
    ///         discrete-density hidden Markov model with uniform probabilities.
    ///     </para>
    ///     <code>
    ///   // Create a new Ergodic hidden Markov model with three
    ///   //   fully-connected states and four sequence symbols.
    ///   var model = new HiddenMarkovModel(new Ergodic(3), 4);
    /// 
    ///   // After creation, the state transition matrix for the model
    ///   // should be given by:
    ///   //
    ///   //       { 0.33, 0.33, 0.33 }
    ///   //       { 0.33, 0.33, 0.33 }
    ///   //       { 0.33, 0.33, 0.33 }
    ///   //       
    ///   // in which all state transitions are allowed.
    ///   </code>
    /// </example>
    [Serializable]
    public class Ergodic : ITopology
    {
        private double[] pi;
        private bool random;
        private int states;

        /// <summary>
        ///     Creates a new Ergodic topology for a given number of states.
        /// </summary>
        /// <param name="states">The number of states to be used in the model.</param>
        public Ergodic(int states)
            : this(states, false)
        {
        }

        /// <summary>
        ///     Creates a new Ergodic topology for a given number of states.
        /// </summary>
        /// <param name="states">The number of states to be used in the model.</param>
        /// <param name="random">
        ///     Whether to initialize the model with random probabilities
        ///     or uniformly with <c>1 / number of states</c>. Default is false (default is
        ///     to use <c>1/states</c>).
        /// </param>
        public Ergodic(int states, bool random)
        {
            if (states <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "states", "Number of states should be higher than zero.");
            }

            this.states = states;
            this.random = random;
            pi = new double[states];

            //for (int i = 0; i < pi.Length; i++)
            //    pi[i] = 1.0 / states;
            pi[0] = 1.0;
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
        ///     Gets the number of states in this topology.
        /// </summary>
        public int States
        {
            get => states;
            set
            {
                states = value;
                pi = Vector.Create(states, pi);
            }
        }

        /// <summary>
        ///     Creates the state transitions matrix and the
        ///     initial state probabilities for this topology.
        /// </summary>
        public int Create(bool logarithm, out double[,] transitionMatrix, out double[] initialState)
        {
            var A = new double[States, States];

            if (random)
            {
                // Create pi
                double sum = 0;
                for (var i = 0; i < states; i++)
                {
                    sum += pi[i] = Generator.Random.NextDouble();
                }

                for (var i = 0; i < states; i++)
                {
                    pi[i] /= sum;
                }

                // Create A using random uniform distribution
                for (var i = 0; i < states; i++)
                {
                    sum = 0.0;
                    for (var j = 0; j < states; j++)
                    {
                        sum += A[i, j] = Generator.Random.NextDouble();
                    }

                    for (var j = 0; j < states; j++)
                    {
                        A[i, j] /= sum;
                    }
                }
            }
            else
            {
                // Create A using equal uniform probabilities,

                for (var i = 0; i < states; i++)
                {
                    for (var j = 0; j < states; j++)
                    {
                        A[i, j] = 1.0 / states;
                    }
                }
            }

            if (logarithm)
            {
                transitionMatrix = A.Log();
                initialState = pi.Log();
            }
            else
            {
                transitionMatrix = A;
                initialState = (double[])pi.Clone();
            }

            return States;
        }
    }
}