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
using System.Threading;
using System.Threading.Tasks;
using Accord.Diagnostics;
using Accord.MachineLearning;
using Accord.Math;

#pragma warning disable 612, 618

namespace Accord.Statistics.Models.Fields.Learning
{
    /// <summary>
    ///     Stochastic Gradient Descent learning algorithm for
    ///     <see cref="HiddenConditionalRandomField{T}">
    ///         Hidden Conditional Hidden Fields
    ///     </see>
    ///     .
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\HiddenConditionalRandomFieldTest.cs"
    ///         region="doc_learn_1" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\HiddenConditionalRandomFieldTest.cs"
    ///         region="doc_learn_2" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\HiddenConditionalRandomFieldTest.cs"
    ///         region="doc_learn_3" />
    /// </example>
    /// <seealso cref="HiddenQuasiNewtonLearning{T}" />
    /// <seealso cref="HiddenResilientGradientLearning{T}" />
    public class HiddenGradientDescentLearning<T> : BaseHiddenConditionalRandomFieldLearning<T>,
        ISupervisedLearning<HiddenConditionalRandomField<T>, T[], int>, IParallel,
        IHiddenConditionalRandomFieldLearning<T>, IConvergenceLearning, IDisposable
    {
        private readonly ISingleValueConvergence convergence;

        private readonly object lockObj = new();

        private ForwardBackwardGradient<T> calculator = new();
        private double[] gradient;

        //private double decay = 0.9;
        //private double tau = 0.5;
        private double stepSize;

        /// <summary>
        ///     Initializes a new instance of the <see cref="HiddenGradientDescentLearning&lt;T&gt;" /> class.
        /// </summary>
        public HiddenGradientDescentLearning()
        {
            convergence = new RelativeConvergence();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HiddenGradientDescentLearning&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="model">The model to be trained.</param>
        public HiddenGradientDescentLearning(HiddenConditionalRandomField<T> model)
            : this()
        {
            Model = model;
            init();
        }

        /// <summary>
        ///     Gets or sets the learning rate to use as the gradient
        ///     descent step size. Default value is 1e-1.
        /// </summary>
        public double LearningRate { get; set; } = 100;

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="HiddenGradientDescentLearning&lt;T&gt;" />
        ///     should use stochastic gradient updates.
        /// </summary>
        /// <value><c>true</c> for stochastic updates; otherwise, <c>false</c>.</value>
        public bool Stochastic { get; set; } = true;

        /// <summary>
        ///     Gets or sets the amount of the parameter weights
        ///     which should be included in the objective function.
        ///     Default is 0 (do not include regularization).
        /// </summary>
        public double Regularization
        {
            get => calculator.Regularization;
            set => calculator.Regularization = value;
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
        public int MaxIterations
        {
            get => convergence.MaxIterations;
            set => convergence.MaxIterations = value;
        }

        /// <summary>
        ///     Please use MaxIterations instead.
        /// </summary>
        [Obsolete("Please use MaxIterations instead.")]
        public int Iterations
        {
            get => MaxIterations;
            set => MaxIterations = value;
        }

        /// <summary>
        ///     Gets or sets the number of performed iterations.
        /// </summary>
        public int CurrentIteration => convergence.CurrentIteration;

        /// <summary>
        ///     Gets or sets whether the algorithm has converged.
        /// </summary>
        /// <value><c>true</c> if this instance has converged; otherwise, <c>false</c>.</value>
        public bool HasConverged => convergence.HasConverged;

        /// <summary>
        ///     Runs the learning algorithm with the specified input
        ///     training observations and corresponding output labels.
        /// </summary>
        /// <param name="observations">The training observations.</param>
        /// <param name="outputs">The observation's labels.</param>
        /// <returns>The error in the last iteration.</returns>
        public double RunEpoch(T[][] observations, int[] outputs)
        {
            double error = 0;

            if (Stochastic)
            {
                // In batch mode, we will use the average of the gradients
                // at each point as a better estimate of the true gradient.
                Array.Clear(gradient, 0, gradient.Length);

                var progress = 0;

                // For each training point
                if (ParallelOptions.MaxDegreeOfParallelism == 1)
                {
                    for (var i = 0; i < observations.Length; i++)
                    {
                        iterate(observations, outputs, i, ref error, ref progress);
                    }
                }
                else
                {
                    Parallel.For(0, observations.Length, ParallelOptions, i =>
                        iterate(observations, outputs, i, ref error, ref progress));
                }

                // Compute the average gradient
                for (var i = 0; i < gradient.Length; i++)
                {
                    gradient[i] /= observations.Length;
                }
            }
            else
            {
                calculator.Inputs = observations;
                calculator.Outputs = outputs;

                // Compute the true gradient
                gradient = calculator.Gradient();

                error = calculator.LastError;
            }

            var parameters = Model.Function.Weights;
            stepSize = LearningRate / (convergence.CurrentIteration + 1);

            // Update the model using a dynamic step size
            for (var i = 0; i < parameters.Length; i++)
            {
                if (double.IsInfinity(parameters[i]))
                {
                    continue;
                }

                parameters[i] -= stepSize * gradient[i];

                Debug.Assert(!double.IsNaN(parameters[i]));
                Debug.Assert(!double.IsPositiveInfinity(parameters[i]));
            }


            return convergence.NewValue = error;
        }

        /// <summary>
        ///     Runs the learning algorithm with the specified input
        ///     training observations and corresponding output labels.
        /// </summary>
        /// <param name="observations">The training observations.</param>
        /// <param name="outputs">The observation's labels.</param>
        /// <returns>The error in the last iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(T[][] observations, int[] outputs)
        {
            return InnerRun(observations, outputs);
        }

        /// <summary>
        ///     Runs one iteration of the learning algorithm with the
        ///     specified input training observation and corresponding
        ///     output label.
        /// </summary>
        /// <param name="observations">The training observations.</param>
        /// <param name="output">The observation's labels.</param>
        /// <returns>The error in the last iteration.</returns>
        public double Run(T[] observations, int output)
        {
            calculator.Inputs = new[] { observations };
            calculator.Outputs = new[] { output };

            var gradient = calculator.Gradient();
            var parameters = Model.Function.Weights;
            var stepSize = LearningRate / convergence.CurrentIteration;

            // Update the model using a dynamic step size
            for (var i = 0; i < parameters.Length; i++)
            {
                if (double.IsInfinity(parameters[i]))
                {
                    continue;
                }

                parameters[i] -= stepSize * gradient[i];
            }

            return calculator.LastError;
        }

        /// <summary>
        ///     Gets or sets the parallelization options for this algorithm.
        /// </summary>
        /// <value>The parallel options.</value>
        public ParallelOptions ParallelOptions
        {
            get => ((IParallel)calculator).ParallelOptions;
            set => ((IParallel)calculator).ParallelOptions = value;
        }

        /// <summary>
        ///     Occurs when the current learning progress has changed.
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        private void init()
        {
            calculator.Model = Model;
            gradient = new double[Model.Function.Weights.Length];
        }

        /// <summary>
        ///     Resets the step size.
        /// </summary>
        public void Reset()
        {
            convergence.Clear();
            stepSize = 0;
        }

        private void iterate(T[][] observations, int[] outputs, int i, ref double error, ref int progress)
        {
            calculator.Inputs = new[] { observations[i] };
            calculator.Outputs = new[] { outputs[i] };

            // Compute the estimated gradient
            var estimate = calculator.Gradient();

            lock (lockObj)
            {
                // Accumulate
                for (var j = 0; j < estimate.Length; j++)
                {
                    gradient[j] += estimate[j];
                }

                error += calculator.LastError;
            }

            var current = Interlocked.Increment(ref progress);
            var percent = current / (double)observations.Length * 100.0;
            OnProgressChanged(new ProgressChangedEventArgs((int)percent, i));

            Debug.Assert(!gradient.HasNaN());
        }

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        protected override double InnerRun(T[][] observations, int[] outputs)
        {
            init();
            convergence.Clear();

            do
            {
                RunEpoch(observations, outputs);
                if (Token.IsCancellationRequested)
                {
                    break;
                }
            } while (!convergence.HasConverged);

            return convergence.NewValue;
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

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing,
        ///     releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Releases unmanaged resources and performs other cleanup operations before
        ///     the <see cref="ForwardBackwardGradient{T}" /> is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~HiddenGradientDescentLearning()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed
        ///     and unmanaged resources; <c>false</c> to release only unmanaged
        ///     resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (calculator != null)
                {
                    calculator.Dispose();
                    calculator = null;
                }
            }
        }

        #endregion
    }
}