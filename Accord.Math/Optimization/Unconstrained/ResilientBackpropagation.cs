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
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Diagnostics;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Resilient Backpropagation method for unconstrained optimization.
    /// </summary>
    /// <seealso cref="ConjugateGradient" />
    /// <seealso cref="BoundedBroydenFletcherGoldfarbShanno" />
    /// <seealso cref="BroydenFletcherGoldfarbShanno" />
    /// <seealso cref="TrustRegionNewtonMethod" />
    public class ResilientBackpropagation : BaseGradientOptimizationMethod, IGradientOptimizationMethod
    {
        private readonly double initialStep = 0.0125;
        private RelativeConvergence convergence;

        private double etaMinus = 0.5;
        private double etaPlus = 1.2;

        private double[] gradient;
        private double[] previousGradient;

        // update values, also known as deltas
        private double[] weightsUpdates;

        /// <summary>
        ///     Creates a new <see cref="ResilientBackpropagation" /> function optimizer.
        /// </summary>
        /// <param name="function">The function to be optimized.</param>
        public ResilientBackpropagation(NonlinearObjectiveFunction function)
            : this(function.NumberOfVariables, function.Function, function.Gradient)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="ResilientBackpropagation" /> function optimizer.
        /// </summary>
        /// <param name="numberOfVariables">The number of free parameters in the function to be optimized.</param>
        /// <param name="function">The function to be optimized.</param>
        /// <param name="gradient">The gradient of the function.</param>
        public ResilientBackpropagation(int numberOfVariables,
            Func<double[], double> function, Func<double[], double[]> gradient)
            : base(numberOfVariables, function, gradient)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="ResilientBackpropagation" /> function optimizer.
        /// </summary>
        /// <param name="numberOfVariables">The number of parameters in the function to be optimized.</param>
        public ResilientBackpropagation(int numberOfVariables)
            : base(numberOfVariables)
        {
        }

        /// <summary>
        ///     Gets or sets the maximum possible update step,
        ///     also referred as delta min. Default is 50.
        /// </summary>
        public double UpdateUpperBound { get; set; } = 50.0;

        /// <summary>
        ///     Gets or sets the minimum possible update step,
        ///     also referred as delta max. Default is 1e-6.
        /// </summary>
        public double UpdateLowerBound { get; set; } = 1e-6;

        /// <summary>
        ///     Gets the decrease parameter, also
        ///     referred as eta minus. Default is 0.5.
        /// </summary>
        public double DecreaseFactor
        {
            get => etaMinus;
            set
            {
                if (value <= 0 || value >= 1)
                {
                    throw new ArgumentOutOfRangeException("value", "Value should be between 0 and 1.");
                }

                etaMinus = value;
            }
        }

        /// <summary>
        ///     Gets the increase parameter, also
        ///     referred as eta plus. Default is 1.2.
        /// </summary>
        public double IncreaseFactor
        {
            get => etaPlus;
            set
            {
                if (value <= 1)
                {
                    throw new ArgumentOutOfRangeException("value", "Value should be higher than 1.");
                }

                etaPlus = value;
            }
        }

        /// <summary>
        ///     Gets or sets the maximum change in the average log-likelihood
        ///     after an iteration of the algorithm used to detect convergence.
        /// </summary>
        public double Tolerance
        {
            get => convergence.Tolerance;
            set => convergence.Tolerance = value;
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     performed by the learning algorithm.
        /// </summary>
        public int Iterations
        {
            get => convergence.MaxIterations;
            set => convergence.MaxIterations = value;
        }

        /// <summary>
        ///     Occurs when the current learning progress has changed.
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        ///     Called when the <see cref="IOptimizationMethod{TInput, TOutput}.NumberOfVariables" /> property has changed.
        /// </summary>
        /// <param name="numberOfVariables">The number of variables.</param>
        protected override void OnNumberOfVariablesChanged(int numberOfVariables)
        {
            base.OnNumberOfVariablesChanged(numberOfVariables);

            convergence = new RelativeConvergence();

            gradient = new double[numberOfVariables];
            previousGradient = new double[numberOfVariables];
            weightsUpdates = new double[numberOfVariables];

            // Initialize steps
            Reset(initialStep);
        }

        /// <summary>
        ///     Implements the actual optimization algorithm. This
        ///     method should try to minimize the objective function.
        /// </summary>
        protected override bool Optimize()
        {
            convergence.Clear();

            do
            {
                runEpoch();
                if (Token.IsCancellationRequested)
                {
                    break;
                }
            } while (!convergence.HasConverged);

            return true;
        }

        private void runEpoch()
        {
            // Compute the true gradient
            gradient = Gradient(Solution);

            var parameters = Solution;

            // Do the Resilient Backpropagation parameter update
            for (var k = 0; k < parameters.Length; k++)
            {
                if (double.IsInfinity(parameters[k]) || double.IsNaN(gradient[k]))
                {
                    continue;
                }

                var g = gradient[k];
                if (g > 1e100)
                {
                    g = 1e100;
                }

                if (g < -1e100)
                {
                    g = -1e100;
                }

                var S = previousGradient[k] * g;

                if (S > 0.0)
                {
                    weightsUpdates[k] = System.Math.Min(weightsUpdates[k] * etaPlus, UpdateUpperBound);
                    parameters[k] -= System.Math.Sign(g) * weightsUpdates[k];
                    previousGradient[k] = g;
                }
                else if (S < 0.0)
                {
                    weightsUpdates[k] = System.Math.Max(weightsUpdates[k] * etaMinus, UpdateLowerBound);
                    previousGradient[k] = 0.0;
                }
                else
                {
                    parameters[k] -= System.Math.Sign(g) * weightsUpdates[k];
                    previousGradient[k] = g;
                }
            }

            Debug.Assert(!parameters.HasNaN());

            var value = Function(parameters);

            convergence.NewValue = value;
        }

        /// <summary>
        ///     Raises the <see cref="E:ProgressChanged" /> event.
        /// </summary>
        /// <param name="args">The ProgressChangedEventArgs instance containing the event data.</param>
        protected void OnProgressChanged(ProgressChangedEventArgs args)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, args);
            }
        }

        /// <summary>
        ///     Resets the current update steps using the given learning rate.
        /// </summary>
        public void Reset(double rate)
        {
            convergence.Clear();

            Parallel.For(0, weightsUpdates.Length, i =>
            {
                for (var j = 0; j < weightsUpdates.Length; j++)
                {
                    weightsUpdates[i] = rate;
                }
            });
        }
    }
}