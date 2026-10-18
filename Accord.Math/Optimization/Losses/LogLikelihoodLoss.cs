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

namespace Accord.Math.Optimization.Losses
{
    /// <summary>
    ///     Negative log-likelihood loss.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The log-likelihood loss can be used to measure the performance of unsupervised
    ///         model fitting algorithms. It simply computes the sum of all log-likelihood values
    ///         produced by the model.
    ///     </para>
    ///     <para>
    ///         If you would like to measure the performance of a supervised classification model
    ///         based on their probability predictions, please refer to the <see cref="BinaryCrossEntropyLoss" />
    ///         and <see cref="CategoryCrossEntropyLoss" /> for binary and multi-class decision problems,
    ///         respectively.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to learn an one-class SVM
    ///         and measure its performance using the log-likelihood loss class.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\OneclassSupportVectorLearningTest.cs"
    ///         region="doc_learn" />
    /// </example>
    /// <seealso cref="BinaryCrossEntropyLoss" />
    /// <seealso cref="CategoryCrossEntropyLoss" />
    [Serializable]
    public class LogLikelihoodLoss : ILoss<double[][]>, ILoss<double[]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LogLikelihoodLoss" /> class.
        /// </summary>
        public LogLikelihoodLoss()
        {
        }

        /// <summary>
        ///     Computes the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        public double Loss(double[] actual)
        {
            double error = 0;
            for (var i = 0; i < actual.Length; i++)
            {
                error += actual[i];
            }

            return error;
        }

        /// <summary>
        ///     Computes the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        public double Loss(double[][] actual)
        {
            double error = 0;
            for (var i = 0; i < actual.Length; i++)
            {
                for (var j = 0; j < actual[i].Length; j++)
                {
                    error += actual[i][j];
                }
            }

            return error;
        }
    }
}