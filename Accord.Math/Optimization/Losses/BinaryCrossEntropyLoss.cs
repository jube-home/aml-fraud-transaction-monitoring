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
    ///     Binary cross-entropy loss for multi-label problems, also
    ///     known as logistic loss per output of a multi-label classifier.
    /// </summary>
    /// <seealso cref="CategoryCrossEntropyLoss" />
    [Serializable]
    public class BinaryCrossEntropyLoss : LossBase<bool[][]>,
        ILoss<int[]>, ILoss<double[]>, ILoss<double[][]>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryCrossEntropyLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public BinaryCrossEntropyLoss(int[][] expected)
        {
            Expected = Classes.Decide(expected);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryCrossEntropyLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public BinaryCrossEntropyLoss(double[][] expected)
        {
            Expected = Classes.Decide(expected);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryCrossEntropyLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public BinaryCrossEntropyLoss(double[] expected)
        {
            Expected = Jagged.ColumnVector(Classes.Decide(expected));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryCrossEntropyLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public BinaryCrossEntropyLoss(bool[] expected)
        {
            Expected = Jagged.ColumnVector(expected);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryCrossEntropyLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public BinaryCrossEntropyLoss(int[] expected)
        {
            Expected = Jagged.OneHot<bool>(expected);
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
                if (Expected[i][0])
                {
                    sum -= System.Math.Log(actual[i]);
                }
                else
                {
                    sum -= Special.Log1m(actual[i]);
                }
            }

            return sum;
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
                    if (Expected[i][j])
                    {
                        sum -= System.Math.Log(actual[i][j]);
                    }
                    else
                    {
                        sum -= Special.Log1m(actual[i][j]);
                    }
                }
            }

            return sum;
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
        public double Loss(int[] actual)
        {
            return Loss(Jagged.OneHot(actual));
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
        public override double Loss(bool[][] actual)
        {
            var sum = 0;
            for (var i = 0; i < actual.Length; i++)
            {
                for (var j = 0; j < actual[i].Length; j++)
                {
                    var e = Expected[i][j];
                    var a = actual[i][j];

                    if (e)
                    {
                        if (!a)
                        {
                            sum--;
                        }
                    }
                    else
                    {
                        if (a)
                        {
                            sum--;
                        }
                    }
                }
            }

            return sum;
        }
    }
}