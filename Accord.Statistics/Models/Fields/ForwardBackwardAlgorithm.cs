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
using Accord.Statistics.Models.Fields.Functions;

namespace Accord.Statistics.Models.Fields
{
    /// <summary>
    ///     Forward-Backward algorithms for Conditional Random Fields.
    /// </summary>
    public static class ForwardBackwardAlgorithm
    {
        /// <summary>
        ///     Computes Forward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static void Forward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, double[] scaling, double[,] fwd)
        {
            var states = function.States;

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
                s += fwd[0, i] = System.Math.Exp(function.Compute(-1, i, observations, 0, output));
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
                s = 0;

                for (var i = 0; i < states; i++)
                {
                    var sum = 0.0;
                    for (var j = 0; j < states; j++)
                    {
                        sum += fwd[t - 1, j] * System.Math.Exp(function.Compute(j, i, observations, t, output));
                    }

                    fwd[t, i] = sum;

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
        }

        /// <summary>
        ///     Computes Forward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] Forward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output = 0)
        {
            var states = function.States;
            var fwd = new double[observations.Length, states];
            return Forward(function, observations, fwd, output);
        }

        /// <summary>
        ///     Computes Forward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] Forward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, double[,] fwd, int output = 0)
        {
            var states = function.States;
            var T = observations.Length;
            Array.Clear(fwd, 0, fwd.Length);

            // 1. Initialization
            for (var i = 0; i < states; i++)
            {
                fwd[0, i] = System.Math.Exp(function.Compute(-1, i, observations, 0, output));
            }

            // 2. Induction
            for (var t = 1; t < T; t++)
            {
                for (var i = 0; i < states; i++)
                {
                    var sum = 0.0;
                    for (var j = 0; j < states; j++)
                    {
                        sum += fwd[t - 1, j] * System.Math.Exp(function.Compute(j, i, observations, t, output));
                    }

                    fwd[t, i] = sum;
                }
            }

            return fwd;
        }

        /// <summary>
        ///     Computes Forward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] Forward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, out double logLikelihood)
        {
            var states = function.States;
            var fwd = new double[observations.Length, states];
            var coefficients = new double[observations.Length];

            Forward(function, observations, output, coefficients, fwd);

            logLikelihood = 0;
            for (var i = 0; i < coefficients.Length; i++)
            {
                logLikelihood += System.Math.Log(coefficients[i]);
            }

            return fwd;
        }

        /// <summary>
        ///     Computes Forward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] Forward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, out double[] scaling)
        {
            var states = function.States;
            var fwd = new double[observations.Length, states];
            scaling = new double[observations.Length];
            Forward(function, observations, output, scaling, fwd);

            return fwd;
        }

        /// <summary>
        ///     Computes Forward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] Forward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, out double[] scaling, out double logLikelihood)
        {
            var states = function.States;
            var fwd = new double[observations.Length, states];
            scaling = new double[observations.Length];
            Forward(function, observations, output, scaling, fwd);

            logLikelihood = 0;
            for (var i = 0; i < scaling.Length; i++)
            {
                logLikelihood += System.Math.Log(scaling[i]);
            }

            return fwd;
        }

        /// <summary>
        ///     Computes Backward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static void Backward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, double[] scaling, double[,] bwd)
        {
            var states = function.States;
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
                        sum += bwd[t + 1, j] * System.Math.Exp(function.Compute(i, j, observations, t + 1, output));
                    }

                    bwd[t, i] += sum / scaling[t];
                }
            }
        }

        /// <summary>
        ///     Computes Backward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] Backward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output = 0)
        {
            var states = function.States;
            var T = observations.Length;
            var bwd = new double[T, states];
            return Backward(function, observations, bwd, output)
                ;
        }

        /// <summary>
        ///     Computes Backward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] Backward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, double[,] bwd, int output = 0)
        {
            var states = function.States;
            var T = observations.Length;
            Array.Clear(bwd, 0, bwd.Length);

            // 1. Initialization
            for (var i = 0; i < states; i++)
            {
                bwd[T - 1, i] = 1.0;
            }

            // 2. Induction
            for (var t = T - 2; t >= 0; t--)
            {
                for (var i = 0; i < states; i++)
                {
                    double sum = 0;
                    for (var j = 0; j < states; j++)
                    {
                        sum += bwd[t + 1, j] * System.Math.Exp(function.Compute(i, j, observations, t + 1, output));
                    }

                    bwd[t, i] += sum;
                }
            }

            return bwd;
        }

        /// <summary>
        ///     Computes Backward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] Backward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, double[] scaling)
        {
            var states = function.States;

            var bwd = new double[observations.Length, states];
            Backward(function, observations, output, scaling, bwd);
            return bwd;
        }

        /// <summary>
        ///     Computes Backward probabilities for a given potential function and a set of observations(no scaling).
        /// </summary>
        public static double[,] Backward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, out double logLikelihood)
        {
            var states = function.States;

            var bwd = Backward(function, observations, output);

            double likelihood = 0;
            for (var i = 0; i < states; i++)
            {
                likelihood += bwd[0, i] * System.Math.Exp(function.Compute(-1, i, observations, 0, output));
            }

            logLikelihood = System.Math.Log(likelihood);

            return bwd;
        }

        /// <summary>
        ///     Computes Forward probabilities for a given hidden Markov model and a set of observations.
        /// </summary>
        public static void LogForward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, double[,] lnFwd)
        {
            var states = function.States;

            var T = observations.Length;

            // Ensures minimum requirements
            Debug.Assert(lnFwd.GetLength(0) >= T);
            Debug.Assert(lnFwd.GetLength(1) == states);
            Array.Clear(lnFwd, 0, lnFwd.Length);


            // 1. Initialization
            for (var i = 0; i < states; i++)
            {
                lnFwd[0, i] = function.Compute(-1, i, observations, 0, output);
            }

            // 2. Induction
            for (var t = 1; t < T; t++)
            {
                for (var i = 0; i < states; i++)
                {
                    var sum = double.NegativeInfinity;
                    for (var j = 0; j < states; j++)
                    {
                        sum = Special.LogSum(sum, lnFwd[t - 1, j] + function.Compute(j, i, observations, t, output));
                    }

                    lnFwd[t, i] = sum;
                }
            }
        }

        /// <summary>
        ///     Computes Forward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] LogForward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output)
        {
            var states = function.States;
            var lnFwd = new double[observations.Length, states];
            LogForward(function, observations, output, lnFwd);
            return lnFwd;
        }

        /// <summary>
        ///     Computes Forward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] LogForward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, out double logLikelihood)
        {
            var states = function.States;
            var lnFwd = new double[observations.Length, states];
            var T = observations.Length;

            LogForward(function, observations, output, lnFwd);

            logLikelihood = double.NegativeInfinity;
            for (var j = 0; j < states; j++)
            {
                logLikelihood = Special.LogSum(logLikelihood, lnFwd[T - 1, j]);
            }

            Debug.Assert(!double.IsNaN(logLikelihood));

            return lnFwd;
        }

        /// <summary>
        ///     Computes Backward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static void LogBackward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, double[,] lnBwd)
        {
            var states = function.States;
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
                            lnBwd[t + 1, j] + function.Compute(i, j, observations, t + 1, output));
                    }

                    lnBwd[t, i] += sum;
                }
            }
        }

        /// <summary>
        ///     Computes Backward probabilities for a given potential function and a set of observations.
        /// </summary>
        public static double[,] LogBackward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output)
        {
            var states = function.States;

            var lnBwd = new double[observations.Length, states];
            LogBackward(function, observations, output, lnBwd);
            return lnBwd;
        }

        /// <summary>
        ///     Computes Backward probabilities for a given potential function and a set of observations(no scaling).
        /// </summary>
        public static double[,] LogBackward<TObservation>(FactorPotential<TObservation> function,
            TObservation[] observations, int output, out double logLikelihood)
        {
            var states = function.States;

            var lnBwd = LogBackward(function, observations, output);

            logLikelihood = double.NegativeInfinity;
            for (var i = 0; i < states; i++)
            {
                logLikelihood = Special.LogSum(logLikelihood,
                    lnBwd[0, i] + function.Compute(-1, i, observations, 0, output));
            }

            return lnBwd;
        }
    }
}