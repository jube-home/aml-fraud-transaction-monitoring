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

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Gradient Descent (GD) for unconstrained optimization.
    /// </summary>
    /// <seealso cref="ConjugateGradient" />
    /// <seealso cref="BoundedBroydenFletcherGoldfarbShanno" />
    /// <seealso cref="BroydenFletcherGoldfarbShanno" />
    /// <seealso cref="TrustRegionNewtonMethod" />
    public class GradientDescent : BaseGradientOptimizationMethod, IGradientOptimizationMethod
    {
        private readonly RelativeConvergence convergence = new();
        private readonly int numberOfUpdatesBeforeConvergenceCheck = 1;

        private double eta = 1e-3;

        /// <summary>
        ///     Creates a new instance of the GD optimization algorithm.
        /// </summary>
        public GradientDescent()
        {
            Iterations = 0;
            Tolerance = 1e-5;
        }

        /// <summary>
        ///     Gets or sets the learning rate. Default is 1e-3.
        /// </summary>
        public double LearningRate
        {
            get => eta;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Learning rate should be higher than 0.");
                }

                eta = value;
            }
        }

        /// <summary>
        ///     Gets or sets the maximum change in the average log-likelihood
        ///     after an iteration of the algorithm used to detect convergence.
        ///     Default is 1e-5.
        /// </summary>
        public double Tolerance
        {
            get => convergence.Tolerance;
            set => convergence.Tolerance = value;
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     performed by the learning algorithm. Default is 0.
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
        ///     Implements the actual optimization algorithm. This
        ///     method should try to minimize the objective function.
        /// </summary>
        protected override bool Optimize()
        {
            convergence.Clear();

            var updates = 0;

            do
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }

                var gradient = Gradient(Solution);
                for (var i = 0; i < Solution.Length; i++)
                {
                    Solution[i] -= eta * gradient[i];
                }

                updates++;

                if (updates >= numberOfUpdatesBeforeConvergenceCheck)
                {
                    convergence.NewValue = Function(Solution);
                    updates = 0;
                }
            } while (!convergence.HasConverged);

            return true;
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
    }
}