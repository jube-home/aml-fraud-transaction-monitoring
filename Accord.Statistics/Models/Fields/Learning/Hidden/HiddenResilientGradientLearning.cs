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
    ///     Resilient Gradient Learning.
    /// </summary>
    /// <typeparam name="T">The type of the observations being modeled.</typeparam>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\HiddenConditionalRandomFieldTest.cs"
    ///         region="doc_learn_1" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\HiddenConditionalRandomFieldTest.cs"
    ///         region="doc_learn_2" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\HiddenConditionalRandomFieldTest.cs"
    ///         region="doc_learn_3" />
    ///     <para>
    ///         The next example shows how to use the learning algorithms in a real-world dataset,
    ///         including training and testing in separate sets and evaluating its performance:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Fields\Learning\ResilientGradientHiddenLearningTest.cs"
    ///         region="doc_learn_pendigits" />
    /// </example>
    /// <seealso cref="HiddenQuasiNewtonLearning{T}" />
    /// <seealso cref="HiddenGradientDescentLearning{T}" />
    public class HiddenResilientGradientLearning<T> : BaseHiddenConditionalRandomFieldLearning<T>,
        ISupervisedLearning<HiddenConditionalRandomField<T>, T[], int>, IParallel,
        IHiddenConditionalRandomFieldLearning<T>, IConvergenceLearning, IDisposable
    {
        private readonly ISingleValueConvergence convergence;

        private readonly double initialStep = 0.0125;

        private readonly object lockObj = new();
        private ForwardBackwardGradient<T> calculator = new();

        private double etaMinus = 0.5;
        private double etaPlus = 1.2;

        private double[] gradient;
        private double[] previousGradient;

        // update values, also known as deltas
        private double[] weightsUpdates;

        /// <summary>
        ///     Initializes a new instance of the <see cref="HiddenResilientGradientLearning{T}" /> class.
        /// </summary>
        public HiddenResilientGradientLearning()
        {
            convergence = new RelativeConvergence(100, 0, 3);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HiddenResilientGradientLearning{T}" /> class.
        /// </summary>
        /// <param name="model">Model to teach.</param>
        public HiddenResilientGradientLearning(HiddenConditionalRandomField<T> model)
            : this()
        {
            Model = model;
            init();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="HiddenGradientDescentLearning&lt;T&gt;" />
        ///     should use stochastic gradient updates. Default is true.
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
        ///     Please use MaxIterations instead.
        /// </summary>
        [Obsolete("Please use MaxIterations instead.")]
        public int Iterations
        {
            get => MaxIterations;
            set => MaxIterations = value;
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
        ///     Gets or sets the number of performed iterations.
        /// </summary>
        public int CurrentIteration => convergence.CurrentIteration;

        /// <summary>
        ///     Gets or sets whether the algorithm has converged.
        /// </summary>
        /// <value><c>true</c> if this instance has converged; otherwise, <c>false</c>.</value>
        public bool HasConverged => convergence.HasConverged;

        /// <summary>
        ///     Runs one iteration of the learning algorithm with the
        ///     specified input training observation and corresponding
        ///     output label.
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


#if SERIAL // For each training point
                for (int i = 0; i < observations.Length; i++)
#else
                Parallel.For(0, observations.Length, ParallelOptions, i =>
#endif
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
#if !SERIAL
                );
#endif
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

            // Do the Resilient Backpropagation parameter update
            for (var k = 0; k < calculator.Parameters.Length; k++)
            {
                if (double.IsInfinity(parameters[k]))
                {
                    continue;
                }

                var S = previousGradient[k] * gradient[k];

                if (S > 0.0)
                {
                    weightsUpdates[k] = System.Math.Min(weightsUpdates[k] * etaPlus, UpdateUpperBound);
                    parameters[k] -= System.Math.Sign(gradient[k]) * weightsUpdates[k];
                    previousGradient[k] = gradient[k];
                }
                else if (S < 0.0)
                {
                    weightsUpdates[k] = System.Math.Max(weightsUpdates[k] * etaMinus, UpdateLowerBound);
                    previousGradient[k] = 0.0;
                }
                else
                {
                    parameters[k] -= System.Math.Sign(gradient[k]) * weightsUpdates[k];
                    previousGradient[k] = gradient[k];
                }
            }

            Debug.Assert(!Model.Function.Weights.HasNaN());

            return convergence.NewValue = error;
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
            return RunEpoch(new[] { observations }, new[] { output });
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
            var parameters = Model.Function.Weights.Length;
            gradient = new double[parameters];
            previousGradient = new double[parameters];
            weightsUpdates = new double[parameters];
            Reset(initialStep); // Initialize steps
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
        ///     the <see cref="HiddenResilientGradientLearning{T}" /> is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~HiddenResilientGradientLearning()
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