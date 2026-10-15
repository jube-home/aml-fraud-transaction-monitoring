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
using System.Threading;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Properties;

namespace Accord.Statistics.Models.Regression.Fitting
{
    /// <summary>
    ///     Stochastic Gradient Descent learning for Logistic Regression fitting.
    /// </summary>
#pragma warning disable 612, 618
    public class LogisticGradientDescent :
        ISupervisedLearning<LogisticRegression, double[], int>,
        ISupervisedLearning<LogisticRegression, double[], bool>,
        ISupervisedLearning<LogisticRegression, double[], double>,
        IRegressionFitting, IConvergenceLearning
#pragma warning restore 612, 618
    {
        private readonly RelativeParameterConvergence convergence;
        private double[] deltas;

        private LogisticRegression regression;

        /// <summary>
        ///     Constructs a new Gradient Descent algorithm.
        /// </summary>
        public LogisticGradientDescent()
        {
            convergence = new RelativeParameterConvergence
            {
                MaxIterations = 0,
                Tolerance = 1e-8
            };
        }

        /// <summary>
        ///     Constructs a new Gradient Descent algorithm.
        /// </summary>
        /// <param name="regression">The regression to estimate.</param>
        public LogisticGradientDescent(LogisticRegression regression)
            : this()
        {
            init(regression);
        }

        /// <summary>
        ///     Gets the previous values for the coefficients which were
        ///     in place before the last learning iteration was performed.
        /// </summary>
        public double[] Previous { get; private set; }

        /// <summary>
        ///     Gets the current values for the coefficients.
        /// </summary>
        public double[] Solution => regression.Intercept.Concatenate(regression.Weights);

        /// <summary>
        ///     Gets the Gradient vector computed in
        ///     the last Newton-Raphson iteration.
        /// </summary>
        public double[] Gradient { get; private set; }

        /// <summary>
        ///     Gets the total number of parameters in the model.
        /// </summary>
        public int Parameters { get; private set; }

        /// <summary>
        ///     Gets or sets whether this algorithm should use
        ///     stochastic updates or not. Default is false.
        /// </summary>
        public bool Stochastic { get; set; }

        /// <summary>
        ///     Gets or sets the algorithm
        ///     learning rate. Default is 0.1.
        /// </summary>
        public double LearningRate { get; set; } = 0.1;

        /// <summary>
        ///     Please use MaxIterations instead.
        /// </summary>
        [Obsolete("Please use MaxIterations instead.")]
        public int Iterations
        {
            get => convergence.MaxIterations;
            set => convergence.MaxIterations = value;
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
        ///     Gets or sets the tolerance value used to determine
        ///     whether the algorithm has converged.
        /// </summary>
        public double Tolerance
        {
            get => convergence.Tolerance;
            set => convergence.Tolerance = value;
        }

        /// <summary>
        ///     Gets the current iteration number.
        /// </summary>
        /// <value>The current iteration.</value>
        public int CurrentIteration => convergence.CurrentIteration;

        /// <summary>
        ///     Gets or sets whether the algorithm has converged.
        /// </summary>
        /// <value><c>true</c> if this instance has converged; otherwise, <c>false</c>.</value>
        public bool HasConverged => convergence.HasConverged;

        /// <summary>
        ///     Runs one iteration of the Reweighted Least Squares algorithm.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The outputs associated with each input vector.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use the Learn(x, y) method instead.")]
        public double Run(double[][] inputs, double[] outputs)
        {
            var old = convergence.Iterations;
            convergence.Iterations = 1;
            Learn(inputs, outputs);
            convergence.Iterations = old;
            return deltas.Max();
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public LogisticRegression Learn(double[][] x, bool[] y, double[] weights = null)
        {
            return Learn(x, y.ToDouble(), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public LogisticRegression Learn(double[][] x, double[] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            if (regression == null)
            {
                init(new LogisticRegression
                {
                    NumberOfInputs = x.Columns()
                });
            }

            // Initial definitions and memory allocations
            var previous = (double[])Solution.Clone();
            convergence.Clear();

            do
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }

                if (Stochastic)
                {
                    for (var j = 0; j < x.Length; j++)
                    {
                        // 1. Compute local gradient estimate
                        var z = regression.Linear.Transform(x[j]);
                        var actual = regression.Link.Inverse(z);
                        var error = y[j] - actual;

                        Gradient[0] = error;
                        for (var i = 0; i < x[j].Length; i++)
                        {
                            Gradient[i + 1] = x[j][i] * error;
                        }

                        // 2. Update using the local estimate
                        for (var i = 0; i < regression.Weights.Length; i++)
                        {
                            regression.Weights[i] += LearningRate * Gradient[i + 1];
                        }

                        regression.Intercept += LearningRate * Gradient[0];
                    }
                }
                else
                {
                    // Compute the complete error gradient
                    Array.Clear(Gradient, 0, Gradient.Length);

                    for (var i = 0; i < x.Length; i++)
                    {
                        // 1. Compute local gradient estimate
                        var z = regression.Linear.Transform(x[i]);
                        var actual = regression.Link.Inverse(z);
                        var error = y[i] - actual;

                        Gradient[0] += error;
                        for (var j = 0; j < x[i].Length; j++)
                        {
                            Gradient[j + 1] += x[i][j] * error;
                        }
                    }

                    // Update coefficients using the gradient
                    for (var i = 0; i < regression.Weights.Length; i++)
                    {
                        regression.Weights[i] += LearningRate * Gradient[i + 1] / x.Length;
                    }

                    regression.Intercept += LearningRate * Gradient[0] / x.Length;
                }

                // Return the maximum parameter change
                this.Previous = previous;
                for (var i = 0; i < previous.Length; i++)
                {
                    deltas[i] = System.Math.Abs(regression.GetCoefficient(i) - previous[i]) /
                                System.Math.Abs(previous[i]);
                }

                convergence.NewValues = Solution;

                if (Token.IsCancellationRequested)
                {
                    return regression;
                }
            } while (!convergence.HasConverged);

            return regression;
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        [field: NonSerialized]
        public CancellationToken Token { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public LogisticRegression Learn(double[][] x, int[] y, double[] weights = null)
        {
            return Learn(x, y.ToDouble(), weights);
        }

        private void init(LogisticRegression regression)
        {
            this.regression = regression;

            Parameters = regression.NumberOfParameters;

            Gradient = new double[Parameters];
            deltas = new double[Parameters];
        }

        /// <summary>
        ///     Runs one iteration of the Reweighted Least Squares algorithm.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The outputs associated with each input vector.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use the Learn(x, y) method instead.")]
        public double Run(double[][] inputs, double[][] outputs)
        {
            if (outputs[0].Length != 1)
            {
                throw new ArgumentException("Function must have a single output.", "outputs");
            }

            var output = new double[outputs.Length];
            for (var i = 0; i < outputs.Length; i++)
            {
                output[i] = outputs[i][0];
            }

            return Run(inputs, output);
        }

        /// <summary>
        ///     Runs a single pass of the gradient descent algorithm.
        /// </summary>
        [Obsolete("Please use the Learn(x, y) method instead.")]
        public double Run(double[] input, double output)
        {
            var old = convergence.Iterations;
            convergence.Iterations = 1;
            Learn(new[] { input }, new[] { output });
            convergence.Iterations = old;
            return deltas.Max();
        }

        /// <summary>
        ///     Computes the sum-of-squared error between the
        ///     model outputs and the expected outputs.
        /// </summary>
        /// <param name="inputs">The input data set.</param>
        /// <param name="outputs">The output values.</param>
        /// <returns>The sum-of-squared errors.</returns>
        [Obsolete("Please use the LogLikelihoodLoss class instead.")]
        public double ComputeError(double[][] inputs, double[] outputs)
        {
            double sum = 0;

            for (var i = 0; i < inputs.Length; i++)
            {
#pragma warning disable 612, 618
                var actual = regression.Compute(inputs[i]);
#pragma warning restore 612, 618
                var expected = outputs[i];
                var delta = actual - expected;
                sum += delta * delta;
            }

            return sum;
        }
    }
}