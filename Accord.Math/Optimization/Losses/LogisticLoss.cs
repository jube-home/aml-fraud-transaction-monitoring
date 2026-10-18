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
using Accord.Statistics;

namespace Accord.Math.Optimization.Losses
{
    /// <summary>
    ///     Logistic loss.
    /// </summary>
    [Serializable]
    public struct LogisticLoss : ILoss<double[][]>, ILoss<double[]>,
        IDifferentiableLoss<bool, double, double>,
        IDifferentiableLoss<double, double, double>
    {
        private double[][] expected;

        /// <summary>
        ///     Gets the expected outputs (the ground truth).
        /// </summary>
        public double[][] Expected
        {
            get => expected;
            set => expected = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LogisticLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public LogisticLoss(double[] expected)
        {
            this.expected = Jagged.ColumnVector(expected);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LogisticLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public LogisticLoss(double[][] expected)
        {
            this.expected = expected;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LogisticLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public LogisticLoss(int[] expected)
        {
            if (expected.IsMinusOnePlusOne())
            {
                expected = expected.ToZeroOne();
            }

            var oneHot = Jagged.OneHot<bool>(expected);
            this.expected = oneHot.ToMinusOnePlusOne();
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
            double sum = 0;
            for (var i = 0; i < actual.Length; i++)
            {
                sum += Loss(Expected[i][0], actual[i]);
            }

            return sum / System.Math.Log(2);
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
            double sum = 0;
            for (var i = 0; i < actual.Length; i++)
            {
                for (var j = 0; j < actual[i].Length; j++)
                {
                    sum += Loss(Expected[i][j], actual[i][j]);
                }
            }

            return sum / System.Math.Log(2);
        }

        /// <summary>
        ///     Computes the derivative of the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="expected">The expected values that should have been predicted.</param>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        public double Loss(double expected, double actual)
        {
            return Special.Log1p(System.Math.Exp(-expected * actual));
        }

        /// <summary>
        ///     Computes the derivative of the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="expected">The expected values that should have been predicted.</param>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        public double Derivative(double expected, double actual)
        {
            return expected / (1 + System.Math.Exp(expected * actual));
        }

        /// <summary>
        ///     Computes the derivative of the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="expected">The expected values that should have been predicted.</param>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        public double Loss(bool expected, double actual)
        {
            if (expected)
            {
                return Special.Log1p(System.Math.Exp(-actual));
            }

            return Special.Log1p(System.Math.Exp(actual));
        }

        /// <summary>
        ///     Computes the derivative of the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="expected">The expected values that should have been predicted.</param>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        public double Derivative(bool expected, double actual)
        {
            if (expected)
            {
                return -1.0 / (1 + System.Math.Exp(actual));
            }

            return 1.0 / (1 + System.Math.Exp(-actual));
        }
    }
}