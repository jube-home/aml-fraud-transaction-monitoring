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
    ///     Accuracy loss, also known as zero-one-loss. This class
    ///     provides exactly the same functionality as <see cref="ZeroOneLoss" />
    ///     but has a more intuitive name. Both classes are interchangeable.
    /// </summary>
    [Serializable]
    public class AccuracyLoss : ZeroOneLoss
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AccuracyLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public AccuracyLoss(double[][] expected)
            : base(expected)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccuracyLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public AccuracyLoss(double[] expected)
            : base(expected)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccuracyLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public AccuracyLoss(int[] expected)
            : base(expected)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZeroOneLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public AccuracyLoss(bool[] expected)
            : base(expected)
        {
        }
    }

    /// <summary>
    ///     Accuracy loss, also known as zero-one-loss.
    /// </summary>
    [Serializable]
    public class ZeroOneLoss : LossBase<int[]>, ILoss<bool[]>,
        ILoss<double[][]>, ILoss<double[]>
    {
        private bool mean = true;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZeroOneLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public ZeroOneLoss(double[][] expected)
            : this(expected.ArgMax(0))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZeroOneLoss" /> class.
        /// </summary>
        /// <param name="classes">The number of classes.</param>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public ZeroOneLoss(int classes, double[][] expected)
            : this(classes, expected.ArgMax(0))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZeroOneLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public ZeroOneLoss(double[] expected)
            : this(expected.ToZeroOne())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZeroOneLoss" /> class.
        /// </summary>
        /// <param name="classes">The number of classes.</param>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public ZeroOneLoss(int classes, double[] expected)
            : this(classes, expected.ToZeroOne())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZeroOneLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public ZeroOneLoss(int[] expected)
            : this(expected.Max() + 1, expected)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZeroOneLoss" /> class.
        /// </summary>
        /// <param name="classes">The number of classes.</param>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public ZeroOneLoss(int classes, int[] expected)
        {
            NumberOfClasses = classes;
            Expected = NumberOfClasses == 2 && expected.IsMinusOnePlusOne() ? expected.ToZeroOne() : expected;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZeroOneLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public ZeroOneLoss(bool[] expected)
            : this(2, expected.ToInt32())
        {
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the average
        ///     accuracy loss should be computed. Default is true.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the average accuracy loss should be computed; otherwise, <c>false</c>.
        /// </value>
        public bool Mean
        {
            get => mean;
            set => mean = value;
        }

        /// <summary>
        ///     Gets or sets the number of classes.
        /// </summary>
        /// <value>The number of classes.</value>
        public int NumberOfClasses { get; set; }

        /// <summary>
        ///     This flag indicates whether the expected class labels are binary.
        /// </summary>
        public bool IsBinary => NumberOfClasses == 2;

        /// <summary>
        ///     Computes the loss between the expected values (ground truth)
        ///     and the given actual values that have been predicted.
        /// </summary>
        /// <param name="actual">The actual values that have been predicted.</param>
        /// <returns>
        ///     The loss value between the expected values and
        ///     the actual predicted values.
        /// </returns>
        public double Loss(bool[] actual)
        {
            return Loss(actual.ToZeroOne());
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
            return Loss(Classes.Decide(actual));
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
            return Loss(actual.ArgMax(0));
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
        public override double Loss(int[] actual)
        {
            NumberOfClasses = System.Math.Max(NumberOfClasses, actual.Max() + 1);

            if (NumberOfClasses == 2)
            {
                actual = actual.ToZeroOne();
            }

            var error = 0;
            for (var i = 0; i < Expected.Length; i++)
            {
                if (Expected[i] != actual[i])
                {
                    error++;
                }
            }

            if (mean)
            {
                return error / (double)Expected.Length;
            }

            return error;
        }
    }
}