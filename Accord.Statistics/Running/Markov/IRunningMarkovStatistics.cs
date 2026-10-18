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
using Accord.Statistics.Models.Markov;

namespace Accord.Statistics.Running
{
    /// <summary>
    ///     Common interface for running Markov filters.
    /// </summary>
    public interface IRunningMarkovStatistics
    {
        /// <summary>
        ///     Gets whether the model has been initialized or not.
        /// </summary>
        bool Started { get; set; }

        /// <summary>
        ///     Gets the current vector of probabilities of being in each state.
        /// </summary>
        double[] Current { get; }

        /// <summary>
        ///     Gets the current most likely state (in the Viterbi path).
        /// </summary>
        int CurrentState { get; }

        /// <summary>
        ///     Gets the current Viterbi probability
        ///     (along the most likely path).
        /// </summary>
        double LogViterbi { get; }

        /// <summary>
        ///     Gets the current Forward probability
        ///     (along all possible paths).
        /// </summary>
        double LogForward { get; }

        /// <summary>
        ///     Clears all measures previously computed
        ///     and indicate the sequence has ended.
        /// </summary>
        void Clear();
    }

    /// <summary>
    ///     Base class for running hidden Markov filters.
    /// </summary>
    [Serializable]
    internal class BaseRunningMarkovStatistics
    {
        private int? currentState;
        private double? logForward;

        private double? logViterbi;
        private bool started;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseRunningMarkovStatistics" /> class.
        /// </summary>
        /// <param name="model">The Markov model.</param>
        public BaseRunningMarkovStatistics(IHiddenMarkovModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            Current = new double[model.States];
        }

        /// <summary>
        ///     Gets whether the model has been initialized or not.
        /// </summary>
        public bool Started
        {
            get => started;
            set => started = value;
        }

        /// <summary>
        ///     Gets the current vector of probabilities of being in each state.
        /// </summary>
        public double[] Current { get; }

        /// <summary>
        ///     Gets the current most likely state (in the Viterbi path).
        /// </summary>
        public int CurrentState
        {
            get
            {
                if (currentState == null)
                {
                    int state;
                    Current.Max(out state);
                    currentState = state;
                }

                return currentState.Value;
            }
        }

        /// <summary>
        ///     Gets the current Viterbi probability
        ///     (along the most likely path).
        /// </summary>
        public double LogViterbi
        {
            get
            {
                if (logViterbi == null)
                {
                    var state = CurrentState;
                    logViterbi = Current[state];
                }

                return logViterbi.Value;
            }
        }

        /// <summary>
        ///     Gets the current Forward probability
        ///     (along all possible paths).
        /// </summary>
        public double LogForward
        {
            get
            {
                if (logForward == null)
                {
                    var sum = double.NegativeInfinity;
                    for (var i = 0; i < Current.Length; i++)
                    {
                        sum = Special.LogSum(sum, Current[i]);
                    }

                    logForward = sum;
                }

                return logForward.Value;
            }
        }

        /// <summary>
        ///     Clears all measures previously computed
        ///     and indicate the sequence has ended.
        /// </summary>
        public void Clear()
        {
            Started = false;
            for (var i = 0; i < Current.Length; i++)
            {
                Current[i] = 0;
            }

            currentState = null;
            logViterbi = null;
            logForward = null;
        }

        /// <summary>
        ///     Clears all measures previously computed.
        /// </summary>
        public void Invalidate()
        {
            currentState = null;
            logViterbi = null;
            logForward = null;
        }
    }
}