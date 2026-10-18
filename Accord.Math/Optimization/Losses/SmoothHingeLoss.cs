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
    ///     Smooth Hinge loss.
    /// </summary>
    [Serializable]
    public struct SmoothHingeLoss : ILoss<double[]>,
        IDifferentiableLoss<bool, double, double>,
        IDifferentiableLoss<double, double, double>
    {
        private HingeLoss hinge;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmoothHingeLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public SmoothHingeLoss(double[][] expected)
        {
            hinge = new HingeLoss(expected);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmoothHingeLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public SmoothHingeLoss(double[] expected)
        {
            hinge = new HingeLoss(expected);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmoothHingeLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public SmoothHingeLoss(int[] expected)
        {
            hinge = new HingeLoss(expected);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmoothHingeLoss" /> class.
        /// </summary>
        /// <param name="expected">The expected outputs (ground truth).</param>
        public SmoothHingeLoss(bool[] expected)
        {
            hinge = new HingeLoss(expected);
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
            for (var i = 0; i < hinge.Expected.Length; i++)
            {
                error += Loss(hinge.Expected[i][0], actual[i]);
            }

            return error;
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
                if (actual > 1)
                {
                    return 0;
                }

                if (actual < 0)
                {
                    return 0.5 - actual;
                }

                var d = 1 - actual;
                return 0.5 * d * d;
            }
            else
            {
                if (-actual > 1)
                {
                    return 0;
                }

                if (-actual < 0)
                {
                    return 0.5 + actual;
                }

                var d = 1 + actual;
                return 0.5 * d * d;
            }
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
                if (actual > 1)
                {
                    return 0;
                }

                if (actual < 0)
                {
                    return actual;
                }

                return actual * (1 - actual);
            }

            if (-actual > 1)
            {
                return 0;
            }

            if (-actual < 0)
            {
                return actual;
            }

            return actual * (1 + actual);
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
            // TODO: Use multiplication instead of conditionals
            if (expected > 0)
            {
                if (actual > 1)
                {
                    return 0;
                }

                if (actual < 0)
                {
                    return 0.5 - actual;
                }

                var d = 1 - actual;
                return 0.5 * d * d;
            }
            else
            {
                if (-actual > 1)
                {
                    return 0;
                }

                if (-actual < 0)
                {
                    return 0.5 + actual;
                }

                var d = 1 + actual;
                return 0.5 * d * d;
            }
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
            // TODO: Use multiplication instead of conditionals
            if (expected > 0)
            {
                if (actual > 1)
                {
                    return 0;
                }

                if (actual < 0)
                {
                    return actual;
                }

                return actual * (1 - actual);
            }

            if (-actual > 1)
            {
                return 0;
            }

            if (-actual < 0)
            {
                return actual;
            }

            return actual * (1 + actual);
        }
    }
}