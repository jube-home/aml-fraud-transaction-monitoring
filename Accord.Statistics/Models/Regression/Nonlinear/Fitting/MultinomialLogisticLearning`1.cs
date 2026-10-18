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
using Accord.Diagnostics;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Math.Optimization;
using Accord.Properties;

namespace Accord.Statistics.Models.Regression.Fitting
{
    /// <summary>
    ///     Gradient optimization for Multinomial logistic regression fitting.
    /// </summary>
    /// <examples>
    ///     <para>
    ///         The gradient optimization class allows multinomial logistic regression models to be learnt
    ///         using any mathematical optimization algorithm that implements the
    ///         <see cref="IFunctionOptimizationMethod{TInput,TOutput}" />
    ///         interface.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\MultinomialLogisticGradientDescentTest.cs"
    ///         region="doc_learn_0" />
    ///     <para>Using Conjugate Gradient:</para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\MultinomialLogisticGradientDescentTest.cs"
    ///         region="doc_learn_cg" />
    ///     <para>Using Gradient Descent:</para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\MultinomialLogisticGradientDescentTest.cs"
    ///         region="doc_learn_gd" />
    ///     <para>Using BFGS:</para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\MultinomialLogisticGradientDescentTest.cs"
    ///         region="doc_learn_bfgs" />
    /// </examples>
    public class MultinomialLogisticLearning<TMethod> :
        ISupervisedLearning<MultinomialLogisticRegression, double[], int>,
        ISupervisedLearning<MultinomialLogisticRegression, double[], int[]>,
        ISupervisedLearning<MultinomialLogisticRegression, double[], double[]>,
        ISupervisedLearning<MultinomialLogisticRegression, double[], bool[]>
        where TMethod : IFunctionOptimizationMethod<double[], double>, new()
    {
        private int current;

        private double[] gradient;

        private double[][] inputs;
        private double[] log_y_hat;
        private TMethod method = new();

        private IntRange[] miniBatches;
        private int[] outputs;

        private MultinomialLogisticRegression regression;

        /// <summary>
        ///     Creates a new <see cref="MultinomialLogisticLearning{TMethod}" />.
        /// </summary>
        public MultinomialLogisticLearning()
        {
        }

        /// <summary>
        ///     Creates a new <see cref="MultinomialLogisticLearning{TMethod}" />.
        /// </summary>
        /// <param name="regression">The regression to estimate.</param>
        public MultinomialLogisticLearning(MultinomialLogisticRegression regression)
            : this()
        {
            init(regression);
        }

        /// <summary>
        ///     Gets or sets the optimization method used to optimize
        ///     the parameters (learn) the <see cref="MultinomialLogisticRegression" />.
        /// </summary>
        public TMethod Method
        {
            get => method;
            set => method = value;
        }

        /// <summary>
        ///     Gets or sets the number of samples to be used as the mini-batch.
        ///     If set to 0 (or a negative number) the total number of training
        ///     samples will be used as the mini-batch.
        /// </summary>
        /// <value>The size of the mini batch.</value>
        public int MiniBatchSize { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public MultinomialLogisticRegression Learn(double[][] x, bool[][] y, double[] weights = null)
        {
            return Learn(x, y.ArgMax(0), weights);
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
        public MultinomialLogisticRegression Learn(double[][] x, double[][] y, double[] weights = null)
        {
            return Learn(x, y.ArgMax(0), weights);
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
        public MultinomialLogisticRegression Learn(double[][] x, int[] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            inputs = x;
            outputs = y;

            if (regression == null)
            {
                regression = new MultinomialLogisticRegression(x.Columns(), y.Max() + 1);
            }

            if (method.NumberOfVariables != regression.NumberOfParameters)
            {
                method.NumberOfVariables = regression.NumberOfParameters;
            }

            method.Function = crossEntropy;

            var gom = method as IGradientOptimizationMethod;
            if (gom != null)
            {
                gom.Gradient = crossEntropyGradient;
            }

            var sc = method as ISupportsCancellation;
            if (sc != null)
            {
                sc.Token = Token;
            }

            if (MiniBatchSize <= 0)
            {
                MiniBatchSize = x.Length;
            }

            gradient = new double[regression.NumberOfParameters];
            log_y_hat = new double[regression.NumberOfOutputs];

            current = 0;
            miniBatches = new IntRange[(int)System.Math.Floor(x.Length / (double)MiniBatchSize)];
            for (var i = 0; i < miniBatches.Length; i++)
            {
                miniBatches[i] = new IntRange(i, System.Math.Min(i + MiniBatchSize, x.Length));
            }

            //bool success = method.Minimize();

            for (int i = 0, k = 0; i < regression.Coefficients.Length; i++)
            {
                for (var j = 0; j < regression.Coefficients[i].Length; j++, k++)
                {
                    regression.Coefficients[i][j] = method.Solution[k];
                }
            }

            return regression;
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
        public MultinomialLogisticRegression Learn(double[][] x, int[][] y, double[] weights = null)
        {
            return Learn(x, y.ArgMax(0), weights);
        }

        private void init(MultinomialLogisticRegression regression)
        {
            this.regression = regression;
        }

        private void compute(double[] w, double[] x, double[] log_responses)
        {
            log_responses[0] = 0;
            for (int j = 1, c = 0; j < log_responses.Length; j++)
            {
                var logit = w[c++]; // intercept
                for (var k = 0; k < x.Length; k++)
                {
                    logit += w[c++] * x[k];
                }

                log_responses[j] = logit;
            }

            var sum = log_responses.LogSumExp();

            // Normalize the probabilities
            for (var j = 0; j < log_responses.Length; j++)
            {
                log_responses[j] -= sum;
            }

#if DEBUG
            var exp = log_responses.Exp();
            var one = exp.Sum();
            Debug.Assert(one.IsEqual(1, 1e-5));
#endif
        }

        internal double crossEntropy(double[] w)
        {
            double sum = 0;

            // Negative error log-likelihood / cross-entropy error function
            for (var j = 0; j < inputs.Length; j++)
            {
                compute(w, inputs[j], log_y_hat);
                sum -= log_y_hat[outputs[j]];
            }

            return sum / inputs.Length;
        }

        internal double[] crossEntropyGradient(double[] w)
        {
            gradient.Clear();

            var miniBatch = miniBatches[current++];
            if (current >= miniBatches.Length)
            {
                current = 0;
            }

            for (var i = miniBatch.Min; i < miniBatch.Max; i++)
            {
                var x = inputs[i];
                var y = outputs[i];

                compute(w, x, log_y_hat);

                for (int s = 1, c = 0; s < log_y_hat.Length; s++)
                {
                    var h = System.Math.Exp(log_y_hat[s]);

                    if (s == y)
                    {
                        gradient[c++] += 1 * h - 1;
                        for (var p = 0; p < x.Length; p++)
                        {
                            gradient[c++] += x[p] * h - x[p];
                        }
                    }
                    else
                    {
                        gradient[c++] += h;
                        for (var p = 0; p < x.Length; p++)
                        {
                            gradient[c++] += x[p] * h;
                        }
                    }
                }
            }

            for (var i = 0; i < gradient.Length; i++)
            {
                gradient[i] /= miniBatch.Length;
            }

            return gradient;
        }
    }
}