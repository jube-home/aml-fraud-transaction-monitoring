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
using Accord.Math;
using Accord.Statistics.Distributions;

namespace Accord.Statistics.Models.Markov
{
    /// <summary>
    ///     Forward-Backward algorithms for Hidden Markov Models.
    /// </summary>
    public static partial class ForwardBackwardAlgorithm
    {
        /// <summary>
        ///     Computes Forward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static void Forward<TDistribution, TObservation>(HiddenMarkovModel<TDistribution, TObservation> model,
            TObservation[] observations, double[] scaling, double[,] fwd)
            where TDistribution : IDistribution<TObservation>
        {
            var states = model.NumberOfStates;
            var A = model.LogTransitions.Exp();
            var B = model.Emissions;
            var pi = model.LogInitial.Exp();

            var T = observations.Length;
            double s = 0;

            // Ensures minimum requirements
            Debug.Assert(fwd.GetLength(0) >= T);
            Debug.Assert(fwd.GetLength(1) == states);
            Debug.Assert(scaling.Length >= T);
            Array.Clear(fwd, 0, fwd.Length);


            // 1. Initialization
            for (var i = 0; i < states; i++)
            {
                s += fwd[0, i] = pi[i] * B[i].ProbabilityFunction(observations[0]);
            }

            scaling[0] = s;

            if (s != 0) // Scaling
            {
                for (var i = 0; i < states; i++)
                {
                    fwd[0, i] /= s;
                }
            }


            // 2. Induction
            for (var t = 1; t < T; t++)
            {
                var obs = observations[t];
                s = 0;

                for (var i = 0; i < states; i++)
                {
                    var sum = 0.0;
                    for (var j = 0; j < states; j++)
                    {
                        sum += fwd[t - 1, j] * A[j][i];
                    }

                    fwd[t, i] = sum * B[i].ProbabilityFunction(obs);

                    s += fwd[t, i]; // scaling coefficient
                }

                scaling[t] = s;

                if (s != 0) // Scaling
                {
                    for (var i = 0; i < states; i++)
                    {
                        fwd[t, i] /= s;
                    }
                }
            }

            Debug.Assert(!fwd.HasNaN());
        }

        /// <summary>
        ///     Computes Forward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static double[,] Forward<TDistribution, TObservation>(
            HiddenMarkovModel<TDistribution, TObservation> model, TObservation[] observations, out double logLikelihood)
            where TDistribution : IDistribution<TObservation>
        {
            var fwd = new double[observations.Length, model.NumberOfStates];
            var scaling = new double[observations.Length];

            Forward(model, observations, scaling, fwd);

            logLikelihood = 0;
            for (var i = 0; i < scaling.Length; i++)
            {
                logLikelihood += System.Math.Log(scaling[i]);
            }

            return fwd;
        }

        /// <summary>
        ///     Computes Forward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static double[,] Forward<TDistribution, TObservation>(
            HiddenMarkovModel<TDistribution, TObservation> model, TObservation[] observations, out double[] scaling)
            where TDistribution : IDistribution<TObservation>
        {
            var fwd = new double[observations.Length, model.NumberOfStates];
            scaling = new double[observations.Length];
            Forward(model, observations, scaling, fwd);
            return fwd;
        }

        /// <summary>
        ///     Computes Forward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static double[,] Forward<TDistribution, TObservation>(
            HiddenMarkovModel<TDistribution, TObservation> model, TObservation[] observations, out double[] scaling,
            out double logLikelihood)
            where TDistribution : IDistribution<TObservation>
        {
            var fwd = new double[observations.Length, model.NumberOfStates];
            scaling = new double[observations.Length];
            Forward(model, observations, scaling, fwd);

            logLikelihood = 0;
            for (var i = 0; i < scaling.Length; i++)
            {
                logLikelihood += System.Math.Log(scaling[i]);
            }

            return fwd;
        }

        /// <summary>
        ///     Computes Backward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static void Backward<TDistribution, TObservation>(HiddenMarkovModel<TDistribution, TObservation> model,
            TObservation[] observations, double[] scaling, double[,] bwd)
            where TDistribution : IDistribution<TObservation>
        {
            var states = model.NumberOfStates;
            var A = model.LogTransitions.Exp();
            var B = model.Emissions;

            var T = observations.Length;

            // Ensures minimum requirements
            Debug.Assert(bwd.GetLength(0) >= T);
            Debug.Assert(bwd.GetLength(1) == states);
            Array.Clear(bwd, 0, bwd.Length);

            // For backward variables, we use the same scale factors
            //   for each time t as were used for forward variables.

            // 1. Initialization
            for (var i = 0; i < states; i++)
            {
                bwd[T - 1, i] = 1.0 / scaling[T - 1];
            }

            // 2. Induction
            for (var t = T - 2; t >= 0; t--)
            {
                for (var i = 0; i < states; i++)
                {
                    double sum = 0;
                    for (var j = 0; j < states; j++)
                    {
                        sum += A[i][j] * B[j].ProbabilityFunction(observations[t + 1]) * bwd[t + 1, j];
                    }

                    bwd[t, i] = sum / scaling[t];
                }
            }

            Debug.Assert(!bwd.HasNaN());
        }

        /// <summary>
        ///     Computes Backward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static double[,] Backward<TDistribution, TObservation>(
            HiddenMarkovModel<TDistribution, TObservation> model, TObservation[] observations, double[] scaling)
            where TDistribution : IDistribution<TObservation>
        {
            var states = model.NumberOfStates;
            var T = observations.Length;
            var bwd = new double[T, states];
            Backward(model, observations, scaling, bwd);
            return bwd;
        }

        /// <summary>
        ///     Computes Forward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static void LogForward<TDistribution, TObservation>(HiddenMarkovModel<TDistribution, TObservation> model,
            TObservation[] observations, double[,] lnFwd)
            where TDistribution : IDistribution<TObservation>
        {
            var states = model.NumberOfStates;
            var logA = model.LogTransitions;
            var logB = model.Emissions;
            var logPi = model.LogInitial;

            var T = observations.Length;

            // Ensures minimum requirements
            Debug.Assert(lnFwd.GetLength(0) >= T);
            Debug.Assert(lnFwd.GetLength(1) == states);
            Array.Clear(lnFwd, 0, lnFwd.Length);


            // 1. Initialization
            for (var i = 0; i < states; i++)
            {
                lnFwd[0, i] = logPi[i] + logB[i].LogProbabilityFunction(observations[0]);
            }

            // 2. Induction
            for (var t = 1; t < T; t++)
            {
                var x = observations[t];

                for (var i = 0; i < states; i++)
                {
                    var sum = double.NegativeInfinity;
                    for (var j = 0; j < states; j++)
                    {
                        sum = Special.LogSum(sum, lnFwd[t - 1, j] + logA[j][i]);
                    }

                    lnFwd[t, i] = sum + logB[i].LogProbabilityFunction(x);
                }
            }

            Debug.Assert(!lnFwd.HasNaN());
        }

        /// <summary>
        ///     Computes Forward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static double[,] LogForward<TDistribution, TObservation>(
            HiddenMarkovModel<TDistribution, TObservation> model, TObservation[] observations, out double logLikelihood)
            where TDistribution : IDistribution<TObservation>
        {
            var T = observations.Length;
            var states = model.NumberOfStates;

            var lnFwd = new double[T, states];

            LogForward(model, observations, lnFwd);

            logLikelihood = double.NegativeInfinity;
            for (var i = 0; i < states; i++)
            {
                logLikelihood = Special.LogSum(logLikelihood, lnFwd[observations.Length - 1, i]);
            }

            return lnFwd;
        }

        /// <summary>
        ///     Computes Forward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static double[,] LogForward<TDistribution, TObservation>(
            HiddenMarkovModel<TDistribution, TObservation> model, TObservation[] observations)
            where TDistribution : IDistribution<TObservation>
        {
            var T = observations.Length;
            var states = model.NumberOfStates;

            var lnFwd = new double[T, states];

            LogForward(model, observations, lnFwd);

            return lnFwd;
        }

        /// <summary>
        ///     Computes Backward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static void LogBackward<TDistribution, TObservation>(
            HiddenMarkovModel<TDistribution, TObservation> model, TObservation[] observations, double[,] lnBwd)
            where TDistribution : IDistribution<TObservation>
        {
            var states = model.NumberOfStates;
            var logA = model.LogTransitions;
            var logB = model.Emissions;

            var T = observations.Length;

            // Ensures minimum requirements
            Debug.Assert(lnBwd.GetLength(0) >= T);
            Debug.Assert(lnBwd.GetLength(1) == states);
            Array.Clear(lnBwd, 0, lnBwd.Length);


            // 1. Initialization
            for (var i = 0; i < states; i++)
            {
                lnBwd[T - 1, i] = 0;
            }

            // 2. Induction
            for (var t = T - 2; t >= 0; t--)
            {
                for (var i = 0; i < states; i++)
                {
                    var sum = double.NegativeInfinity;
                    for (var j = 0; j < states; j++)
                    {
                        sum = Special.LogSum(sum,
                            lnBwd[t + 1, j] + logA[i][j] + logB[j].LogProbabilityFunction(observations[t + 1]));
                    }

                    lnBwd[t, i] = sum;
                }
            }

            Debug.Assert(!lnBwd.HasNaN());
        }

        /// <summary>
        ///     Computes Backward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static double[,] LogBackward<TDistribution, TObservation>(
            HiddenMarkovModel<TDistribution, TObservation> model, TObservation[] observations)
            where TDistribution : IDistribution<TObservation>
        {
            var states = model.NumberOfStates;

            var T = observations.Length;
            var lnBwd = new double[T, states];
            LogBackward(model, observations, lnBwd);
            return lnBwd;
        }

        /// <summary>
        ///     Computes Backward probabilities for a given hidden Markov model and a set of observations (no scaling).
        /// </summary>
        public static double[,] LogBackward<TDistribution, TObservation>(
            HiddenMarkovModel<TDistribution, TObservation> model, TObservation[] observations, out double logLikelihood)
            where TDistribution : IDistribution<TObservation>
        {
            var states = model.NumberOfStates;

            var T = observations.Length;
            var lnBwd = new double[T, states];
            LogBackward(model, observations, lnBwd);

            logLikelihood = double.NegativeInfinity;
            for (var i = 0; i < model.NumberOfStates; i++)
            {
                logLikelihood = Special.LogSum(logLikelihood, lnBwd[0, i] + model.LogInitial[i]
                                                                          + model.Emissions[i]
                                                                              .LogProbabilityFunction(observations[0]));
            }

            return lnBwd;
        }
    }
}