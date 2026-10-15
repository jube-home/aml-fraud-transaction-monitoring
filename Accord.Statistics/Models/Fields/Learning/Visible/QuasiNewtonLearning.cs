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
using System.Linq;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Math.Optimization;
using Accord.Properties;
using Accord.Statistics.Models.Fields.Functions;

#pragma warning disable 612, 618

namespace Accord.Statistics.Models.Fields.Learning
{
    /// <summary>
    ///     Quasi-Newton (L-BFGS) learning algorithm for
    ///     <see cref="ConditionalRandomField{T}">
    ///         Conditional Hidden Fields
    ///     </see>
    ///     .
    /// </summary>
    public class QuasiNewtonLearning<T> : ParallelLearningBase,
        ISupervisedLearning<ConditionalRandomField<T>, T[], int[]>,
        IConditionalRandomFieldLearning<T>, IConvergenceLearning
    {
        private BoundedBroydenFletcherGoldfarbShanno lbfgs;

        /// <summary>
        ///     Constructs a new L-BFGS learning algorithm.
        /// </summary>
        public QuasiNewtonLearning(ConditionalRandomField<T> model)
            : this()
        {
            Model = model;
            init();
        }

        /// <summary>
        ///     Constructs a new L-BFGS learning algorithm.
        /// </summary>
        public QuasiNewtonLearning()
        {
            Tolerance = 1e-3;
        }

        /// <summary>
        ///     Gets or sets the model being trained.
        /// </summary>
        public ConditionalRandomField<T> Model { get; set; }

        /// <summary>
        ///     Gets or sets the potential function to use if this learning
        ///     algorithm needs to create a new <see cref="ConditionalRandomField{T}" />.
        /// </summary>
        public IPotentialFunction<T> Function { get; set; }

        /// <summary>
        ///     Runs the learning algorithm with the specified input
        ///     training observations and corresponding output labels.
        /// </summary>
        /// <param name="observations">The training observations.</param>
        /// <param name="labels">The observation's labels.</param>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(T[][] observations, int[][] labels)
        {
            return run(observations, labels);
        }

        /// <summary>
        ///     Gets or sets the tolerance value used to determine
        ///     whether the algorithm has converged.
        /// </summary>
        /// <value>The tolerance.</value>
        public double Tolerance { get; set; }

        int IConvergenceLearning.Iterations
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     performed by the learning algorithm.
        /// </summary>
        /// <value>The maximum iterations.</value>
        public int MaxIterations { get; set; }

        /// <summary>
        ///     Gets the current iteration number.
        /// </summary>
        /// <value>The current iteration.</value>
        public int CurrentIteration
        {
            get
            {
                if (lbfgs == null)
                {
                    return lbfgs.Iterations;
                }

                return 0;
            }
        }

        /// <summary>
        ///     Gets or sets whether the algorithm has converged.
        /// </summary>
        /// <value><c>true</c> if this instance has converged; otherwise, <c>false</c>.</value>
        public bool HasConverged =>
            lbfgs.Status == BoundedBroydenFletcherGoldfarbShannoStatus.FunctionConvergence
            || lbfgs.Status == BoundedBroydenFletcherGoldfarbShannoStatus.GradientConvergence;

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public ConditionalRandomField<T> Learn(T[][] x, int[][] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            if (Model == null)
            {
                Model = new ConditionalRandomField<T>(y.Max() + 1, Function);
                init();
            }

            run(x, y);

            return Model;
        }

        private void init()
        {
            lbfgs = new BoundedBroydenFletcherGoldfarbShanno(Model.Function.Weights.Length);
            lbfgs.FunctionTolerance = Tolerance;
            lbfgs.MaxIterations = MaxIterations;

            for (var i = 0; i < lbfgs.UpperBounds.Length; i++)
            {
                lbfgs.UpperBounds[i] = 1e10;
                lbfgs.LowerBounds[i] = -1e100;
            }
        }

        private double run(T[][] observations, int[][] labels)
        {
            var numberOfParameters = Model.Function.Weights.Length;

            var g = new double[numberOfParameters];

            var model = Model;

            lbfgs.Function = parameters =>
            {
                model.Function.Weights = parameters;
                return -model.LogLikelihood(observations, labels);
            };

            lbfgs.Gradient = parameters =>
            {
                model.Function.Weights = parameters;
                gradient(observations, labels, g);
                return g;
            };

            lbfgs.Token = Token;

            if (lbfgs.Minimize(model.Function.Weights))
            {
                model.Function.Weights = lbfgs.Solution;
            }

            return model.LogLikelihood(observations, labels);
        }

        private void gradient(T[][] observations, int[][] labels, double[] g)
        {
            var model = Model;
            var function = model.Function;
            var states = model.States;
            var d = Model.Function.Weights.Length;
            var Tmax = observations.Max(x => x.Length);

            g.Clear();


            // Compute sequence probabilities
            Parallel.For(0, observations.Length, ParallelOptions,
                () =>
                {
                    // Create thread-local storage
                    var work = new double[states + 1, states][];
                    for (var j = 0; j < states + 1; j++)
                    {
                        for (var k = 0; k < states; k++)
                        {
                            work[j, k] = new double[Tmax];
                        }
                    }

                    return new
                    {
                        bwd = new double[Tmax, states],
                        fwd = new double[Tmax, states],
                        sum1 = new double[d],
                        sum2 = new double[d],
                        work
                    };
                },
                (i, state, local) =>
                {
                    var x = observations[i];
                    var fwd = local.fwd;
                    var bwd = local.bwd;
                    var sum1 = local.sum1;
                    var sum2 = local.sum2;
                    var work = local.work;
                    ForwardBackwardAlgorithm.Forward(function.Factors[0], x, fwd);
                    ForwardBackwardAlgorithm.Backward(function.Factors[0], x, bwd);
                    var z = partition(fwd, x);

                    for (var prev = -1; prev < states; prev++)
                    {
                        for (var next = 0; next < states; next++)
                        {
                            var Pis = work[prev + 1, next];
                            for (var t = 0; t < x.Length; t++)
                            {
                                Pis[t] = p(prev, next, x, t, fwd, bwd, function) / z;
                            }
                        }
                    }

                    // Compute the gradient w.r.t. each feature
                    //  function in the model's potential function.

                    var y = labels[i];

                    Parallel.For(0, g.Length, ParallelOptions, k =>
                    {
                        var feature = function.Features[k];

                        // Compute first term of the partial derivative
                        sum1[k] += feature.Compute(-1, y[0], x, 0);
                        for (var t = 1; t < x.Length; t++)
                        {
                            sum1[k] += feature.Compute(y[t - 1], y[t], x, t);
                        }

                        // Compute second term of the partial derivative
                        for (var prev = -1; prev < states; prev++)
                        {
                            for (var next = 0; next < states; next++)
                            {
                                var Pis = work[prev + 1, next];
                                for (var t = 0; t < Pis.Length; t++)
                                {
                                    sum2[k] += feature.Compute(prev, next, x, t) * Pis[t];
                                }
                            }
                        }
                    });

                    return local;
                },
                local =>
                {
                    lock (g)
                    {
                        for (var k = 0; k < g.Length; k++)
                        {
                            g[k] -= local.sum1[k] - local.sum2[k];
                        }
                    }
                }
            );
        }

        private static double p(int previous, int next, T[] x, int t,
            double[,] fwd, double[,] bwd, IPotentialFunction<T> function)
        {
            // This is the probability of beginning at the start of the sequence, 
            // evaluating the sequence until the instant at which the label "previous"
            // happened, computing a transition from "previous" to "next" and then 
            // continuing until the end of the sequence.

            var p = 1.0;

            // Evaluate until the instant t - 1
            if (t > 0 && previous >= 0)
            {
                p *= fwd[t - 1, previous];
            }
            else if (t == 0 && previous == -1)
            {
                p = 1.0;
            }
            else
            {
                return 0;
            }

            // Compute a transition from "previous" to "next"
            p *= System.Math.Exp(function.Factors[0].Compute(previous, next, x, t));

            // Continue until the end of the sequence
            p *= bwd[t, next];

            return p;
        }

        private double partition(double[,] fwd, T[] x)
        {
            var T = x.Length - 1;
            var states = Model.States;

            var z = 0.0;
            for (var i = 0; i < states; i++)
            {
                z += fwd[T, i];
            }

            return z;
        }
    }
}