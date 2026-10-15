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

#pragma warning disable 612, 618

namespace Accord.Statistics.Running
{
    /// <summary>
    ///     Hidden Markov Model filter.
    /// </summary>
    [Serializable]
    public class RunningMarkovStatistics : IRunning<int>, IRunningMarkovStatistics
    {
        private BaseRunningMarkovStatistics _base;
        private double[] next;

        private double[] previous;

        /// <summary>
        ///     Creates a new <see cref="RunningMarkovStatistics" />.
        /// </summary>
        /// <param name="model">The hidden Markov model to use in this filter.</param>
        public RunningMarkovStatistics(HiddenMarkovModel model)
        {
            Model = model;
            previous = new double[model.States];
            next = new double[model.States];

            _base = new BaseRunningMarkovStatistics(model);
        }

        /// <summary>
        ///     Gets the <see cref="HiddenMarkovModel" /> used in this filter.
        /// </summary>
        public HiddenMarkovModel Model { get; private set; }

        /// <summary>
        ///     Registers the occurrence of a value.
        /// </summary>
        /// <param name="value">The value to be registered.</param>
        public void Push(int value)
        {
            if (!Started)
            {
                for (var i = 0; i < Current.Length; i++)
                {
                    Current[i] = Model.LogInitial[i] + Model.LogEmissions[i][value];
                }

                Started = true;
            }
            else
            {
                for (var i = 0; i < Current.Length; i++)
                {
                    previous[i] = Current[i];
                }

                for (var i = 0; i < Current.Length; i++)
                {
                    var sum = double.NegativeInfinity;
                    for (var j = 0; j < previous.Length; j++)
                    {
                        sum = Special.LogSum(sum, previous[j] + Model.LogTransitions[j][i]);
                    }

                    Current[i] = sum + Model.LogEmissions[i][value];
                }
            }

            _base.Invalidate();
        }

        /// <summary>
        ///     Clears this instance.
        /// </summary>
        public void Clear()
        {
            _base.Clear();
        }

        /// <summary>
        ///     Gets whether the model has been initialized or not.
        /// </summary>
        public bool Started
        {
            get => _base.Started;
            set => _base.Started = value;
        }

        /// <summary>
        ///     Gets the current vector of probabilities of being in each state.
        /// </summary>
        public double[] Current => _base.Current;

        /// <summary>
        ///     Gets the current most likely state (in the Viterbi path).
        /// </summary>
        public int CurrentState => _base.CurrentState;

        /// <summary>
        ///     Gets the current Viterbi probability
        ///     (along the most likely path).
        /// </summary>
        public double LogViterbi => _base.LogViterbi;

        /// <summary>
        ///     Gets the current Forward probability
        ///     (along all possible paths).
        /// </summary>
        public double LogForward => _base.LogForward;

        /// <summary>
        ///     Checks the classification after the insertion
        ///     of a new value without registering this value.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        public double Peek(int value)
        {
            if (!Started)
            {
                for (var i = 0; i < Current.Length; i++)
                {
                    next[i] = Model.LogInitial[i] + Model.Emissions[i, value];
                }
            }
            else
            {
                for (var i = 0; i < Current.Length; i++)
                {
                    var sum = double.NegativeInfinity;
                    for (var j = 0; j < previous.Length; j++)
                    {
                        sum = Special.LogSum(sum, Current[j] + Model.Transitions[j, i]);
                    }

                    next[i] = sum + Model.Emissions[i, value];
                }
            }

            var logLikelihood = double.NegativeInfinity;
            for (var i = 0; i < next.Length; i++)
            {
                logLikelihood = Special.LogSum(logLikelihood, next[i]);
            }

            return logLikelihood;
        }
    }
}